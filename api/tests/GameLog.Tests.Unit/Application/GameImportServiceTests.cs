using GameLog.Application.Catalog;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Infrastructure.Persistence;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Application;

public class GameImportServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => testDb.Dispose();

    private GameImportService Service(FakeProvider? provider, FakeArtwork? artwork = null) =>
        new(testDb.Create(), provider is null ? [] : [provider], artwork ?? new FakeArtwork(), clock, NullLogger<GameImportService>.Instance);

    private async Task<Game> Load(Guid id)
    {
        await using var db = testDb.Create();
        return await db.Set<Game>().Include(g => g.GameGenres).ThenInclude(x => x.Genre)
            .Include(g => g.GamePlatforms).ThenInclude(x => x.Platform).SingleAsync(g => g.Id == id);
    }

    [Fact]
    public async Task Import_creates_the_game_with_genres_platforms_and_a_slug()
    {
        var provider = new FakeProvider(Ext.Game("1", "Baldur's Gate 3", ["RPG", "Strategy"], ["PC", "PS5"]));
        var result = await Service(provider).ImportAsync("1");

        Assert.True(result.Created);
        Assert.Equal("baldurs-gate-3", result.Slug);
        var game = await Load(result.Id);
        Assert.Equal(["RPG", "Strategy"], game.GameGenres.Select(g => g.Genre.Name).Order().ToArray());
        Assert.Equal(["PC", "PS5"], game.GamePlatforms.Select(p => p.Platform.Name).Order().ToArray());
        Assert.NotNull(game.CoverKey);
        Assert.Equal("fake", game.ExternalProvider);
        Assert.Equal("1", game.ExternalId);
    }

    [Fact]
    public async Task Importing_twice_updates_instead_of_duplicating()
    {
        var provider = new FakeProvider(Ext.Game("1", "Hades"));
        var first = await Service(provider).ImportAsync("1");
        var second = await Service(provider).ImportAsync("1");

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Id, second.Id);

        await using var db = testDb.Create();
        Assert.Equal(1, await db.Set<Game>().AsNoTracking().CountAsync());
        Assert.Equal(1, await db.Set<Genre>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Different_games_with_the_same_title_get_distinct_slugs()
    {
        var provider = new FakeProvider(Ext.Game("1", "Prey"), Ext.Game("2", "Prey"));
        var a = await Service(provider).ImportAsync("1");
        var b = await Service(provider).ImportAsync("2");

        Assert.Equal("prey", a.Slug);
        Assert.Equal("prey-2", b.Slug);
    }

    [Fact]
    public async Task A_reimport_replaces_genres_that_the_provider_no_longer_lists()
    {
        var id = (await Service(new FakeProvider(Ext.Game("1", "Doom", ["Shooter", "Action"]))).ImportAsync("1")).Id;
        await Service(new FakeProvider(Ext.Game("1", "Doom", ["Shooter", "Horror"]))).ImportAsync("1");

        var game = await Load(id);
        Assert.Equal(["Horror", "Shooter"], game.GameGenres.Select(g => g.Genre.Name).Order().ToArray());
    }

    [Fact]
    public async Task A_failed_artwork_download_does_not_fail_the_import()
    {
        var provider = new FakeProvider(Ext.Game("1", "Celeste"));
        var result = await Service(provider, new FakeArtwork(succeed: false)).ImportAsync("1");

        var game = await Load(result.Id);
        Assert.Null(game.CoverKey);
        Assert.Equal("Celeste", game.Title);
    }

    [Fact]
    public async Task Artwork_is_downloaded_only_once()
    {
        var provider = new FakeProvider(Ext.Game("1", "Celeste"));
        var artwork = new FakeArtwork();
        await Service(provider, artwork).ImportAsync("1");
        var afterFirst = artwork.Calls;
        await Service(provider, artwork).ImportAsync("1");

        Assert.Equal(afterFirst, artwork.Calls);
    }

    [Fact]
    public async Task Unknown_external_ids_are_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service(new FakeProvider()).ImportAsync("999"));
    }

    [Fact]
    public async Task Without_a_provider_imports_report_it_as_unavailable()
    {
        await Assert.ThrowsAsync<ProviderUnavailableException>(() => Service(null).ImportAsync("1"));
    }

    [Fact]
    public async Task Provider_search_requires_a_meaningful_query()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Service(new FakeProvider()).SearchProviderAsync("a"));
    }
}
