using GameLog.Application.Activity;
using GameLog.Application.Collections;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Application;

public class CollectionServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly MemoryStorage storage = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly Game hades;
    private readonly Game celeste;
    private readonly Game hollow;
    private readonly User alice;
    private readonly User bob;

    public CollectionServiceTests()
    {
        using var db = testDb.Create();
        hades = Seed.Game(db, "Hades");
        celeste = Seed.Game(db, "Celeste");
        hollow = Seed.Game(db, "Hollow Knight");
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
    }

    public void Dispose() => testDb.Dispose();

    private CollectionService Svc(bool validImage = true)
    {
        var db = testDb.Create();
        return new CollectionService(db, storage, new FakeImageProcessor(validImage), new ActivityService(db, clock), clock, NullLogger<CollectionService>.Instance);
    }

    private async Task<Guid> Make(User owner, Visibility visibility = Visibility.Public, string name = "Favourites") =>
        (await Svc().CreateAsync(owner.Id, new SaveCollectionRequest(name, "desc", visibility))).Collection.Id;

    [Fact]
    public async Task Creating_a_collection_returns_it_empty_and_owned()
    {
        var detail = await Svc().CreateAsync(alice.Id, new SaveCollectionRequest("  Best of 2025 ", null, Visibility.Public));
        Assert.Equal("Best of 2025", detail.Collection.Name);
        Assert.Equal("alice", detail.Collection.Owner.Username);
        Assert.Empty(detail.Games);
    }

    [Fact]
    public async Task An_invalid_name_is_rejected() =>
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().CreateAsync(alice.Id, new SaveCollectionRequest(" ", null, Visibility.Public)));

    [Fact]
    public async Task Public_creation_is_published_to_the_activity_feed_but_private_is_not()
    {
        await Make(alice, Visibility.Private, "Secret");
        await using (var db = testDb.Create())
        {
            Assert.Equal(0, await db.Set<ActivityFeedItem>().AsNoTracking().CountAsync());
        }

        await Make(alice, Visibility.Public, "Open");
        await using var db2 = testDb.Create();
        Assert.Equal(1, await db2.Set<ActivityFeedItem>().AsNoTracking().CountAsync(a => a.Type == ActivityType.CollectionCreated));
    }

    [Fact]
    public async Task Games_are_appended_in_order_and_duplicates_are_rejected()
    {
        var id = await Make(alice);
        await Svc().AddGameAsync(alice.Id, id, hades.Id);
        var detail = await Svc().AddGameAsync(alice.Id, id, celeste.Id);

        Assert.Equal(["Hades", "Celeste"], detail.Games.Select(g => g.Title).ToArray());
        Assert.Equal([0, 1], detail.Games.Select(g => g.Position).ToArray());
        await Assert.ThrowsAsync<ConflictException>(() => Svc().AddGameAsync(alice.Id, id, hades.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().AddGameAsync(alice.Id, id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Removing_a_game_keeps_the_rest_in_order()
    {
        var id = await Make(alice);
        foreach (var g in new[] { hades, celeste, hollow })
        {
            await Svc().AddGameAsync(alice.Id, id, g.Id);
        }

        var detail = await Svc().RemoveGameAsync(alice.Id, id, celeste.Id);
        Assert.Equal(["Hades", "Hollow Knight"], detail.Games.Select(g => g.Title).ToArray());
        var appended = await Svc().AddGameAsync(alice.Id, id, celeste.Id);
        Assert.Equal("Celeste", appended.Games[^1].Title);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().RemoveGameAsync(alice.Id, id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Reordering_applies_the_requested_order()
    {
        var id = await Make(alice);
        foreach (var g in new[] { hades, celeste, hollow })
        {
            await Svc().AddGameAsync(alice.Id, id, g.Id);
        }

        var detail = await Svc().ReorderAsync(alice.Id, id, [hollow.Id, hades.Id, celeste.Id]);
        Assert.Equal(["Hollow Knight", "Hades", "Celeste"], detail.Games.Select(g => g.Title).ToArray());
        Assert.Equal([0, 1, 2], detail.Games.Select(g => g.Position).ToArray());
    }

    [Fact]
    public async Task A_reorder_that_adds_drops_or_repeats_games_is_rejected()
    {
        var id = await Make(alice);
        await Svc().AddGameAsync(alice.Id, id, hades.Id);
        await Svc().AddGameAsync(alice.Id, id, celeste.Id);

        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().ReorderAsync(alice.Id, id, [hades.Id]));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().ReorderAsync(alice.Id, id, [hades.Id, hades.Id]));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().ReorderAsync(alice.Id, id, [hades.Id, hollow.Id]));
    }

    [Fact]
    public async Task Private_collections_are_invisible_to_other_users()
    {
        var id = await Make(alice, Visibility.Private);

        Assert.Equal(id, (await Svc().GetAsync(id, alice.Id)).Collection.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(id, bob.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(id, null));
        Assert.Empty((await Svc().ListForUserAsync("alice", bob.Id, null, null)).Items);
        Assert.Single((await Svc().ListForUserAsync("alice", alice.Id, null, null)).Items);
    }

    [Fact]
    public async Task Other_users_cannot_modify_a_collection()
    {
        var open = await Make(alice, Visibility.Public);
        var secret = await Make(alice, Visibility.Private, "Secret");

        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().AddGameAsync(bob.Id, open, hades.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().UpdateAsync(bob.Id, open, new SaveCollectionRequest("Hacked", null, Visibility.Public)));
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().DeleteAsync(bob.Id, false, open));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().AddGameAsync(bob.Id, secret, hades.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().DeleteAsync(bob.Id, false, secret));
    }

    [Fact]
    public async Task Making_a_collection_private_hides_it_from_the_public_list()
    {
        var id = await Make(alice);
        await Svc().AddGameAsync(alice.Id, id, hades.Id);
        Assert.Single((await Svc().ListPublicAsync(null, null)).Items);

        await Svc().UpdateAsync(alice.Id, id, new SaveCollectionRequest("Favourites", null, Visibility.Private));
        Assert.Empty((await Svc().ListPublicAsync(null, null)).Items);
    }

    [Fact]
    public async Task Deleting_a_collection_removes_it_and_its_cover()
    {
        var id = await Make(alice);
        var cover = await Svc().SetCoverAsync(alice.Id, id, new MemoryStream([1, 2, 3]));
        Assert.Single(storage.Objects);
        Assert.StartsWith("/api/v1/media/collections/", cover.CoverUrl);

        await Svc().DeleteAsync(alice.Id, false, id);
        Assert.Empty(storage.Objects);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(id, alice.Id));
    }

    [Fact]
    public async Task Replacing_a_cover_deletes_the_previous_file()
    {
        var id = await Make(alice);
        await Svc().SetCoverAsync(alice.Id, id, new MemoryStream([1]));
        await Svc().SetCoverAsync(alice.Id, id, new MemoryStream([2]));
        Assert.Single(storage.Objects);
    }

    [Fact]
    public async Task An_invalid_cover_image_is_rejected_and_stores_nothing()
    {
        var id = await Make(alice);
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc(validImage: false).SetCoverAsync(alice.Id, id, new MemoryStream([9])));
        Assert.Empty(storage.Objects);
    }
}
