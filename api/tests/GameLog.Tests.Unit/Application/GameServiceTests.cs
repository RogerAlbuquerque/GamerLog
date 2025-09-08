using GameLog.Application.Common;
using GameLog.Application.Games;
using GameLog.Tests.Unit.Support;

namespace GameLog.Tests.Unit.Application;

public class GameServiceTests : IDisposable
{
    private readonly TestDb testDb = new();

    public void Dispose() => testDb.Dispose();

    private GameService Service() => new(testDb.Create());

    private void Seeded()
    {
        using var db = testDb.Create();
        Seed.Game(db, "Hades", new DateOnly(2020, 9, 17), ratingSum: 18, ratingCount: 2, completed: 5,
            genres: ["Roguelike", "Action"], platforms: ["PC", "Switch"]);
        Seed.Game(db, "Hollow Knight", new DateOnly(2017, 2, 24), ratingSum: 9, ratingCount: 1, completed: 1,
            genres: ["Metroidvania", "Action"], platforms: ["PC"]);
        Seed.Game(db, "Stardew Valley", new DateOnly(2016, 2, 26), genres: ["Simulation"], platforms: ["PC", "Switch"]);
    }

    [Fact]
    public async Task Title_search_is_case_insensitive_and_partial()
    {
        Seeded();
        var result = await Service().SearchAsync(new GameSearchQuery(Q: "HOLL"));
        Assert.Equal("Hollow Knight", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task Wildcard_characters_in_the_query_are_not_patterns()
    {
        Seeded();
        var result = await Service().SearchAsync(new GameSearchQuery(Q: "h_des"));
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Filters_by_genre_platform_and_year()
    {
        Seeded();
        var action = await Service().SearchAsync(new GameSearchQuery(Genre: "action"));
        Assert.Equal(2, action.Total);

        var switchGames = await Service().SearchAsync(new GameSearchQuery(Platform: "switch"));
        Assert.Equal(2, switchGames.Total);

        var from2017 = await Service().SearchAsync(new GameSearchQuery(Year: 2017));
        Assert.Equal("Hollow Knight", Assert.Single(from2017.Items).Title);

        var combined = await Service().SearchAsync(new GameSearchQuery(Genre: "action", Platform: "switch"));
        Assert.Equal("Hades", Assert.Single(combined.Items).Title);
    }

    [Fact]
    public async Task Sorting_by_rating_uses_the_average_not_the_total()
    {
        Seeded();
        var result = await Service().SearchAsync(new GameSearchQuery(Sort: "rating"));
        // Hades: 18/2/2 = 4.5 stars, Hollow Knight: 9/2/1 = 4.5 stars -> tie broken by rating count, unrated last.
        Assert.Equal(["Hades", "Hollow Knight", "Stardew Valley"], result.Items.Select(i => i.Title).ToArray());
        Assert.Equal(4.5, result.Items[0].AverageRating);
        Assert.Null(result.Items[2].AverageRating);
    }

    [Fact]
    public async Task Sorting_by_title_and_release()
    {
        Seeded();
        var byTitle = await Service().SearchAsync(new GameSearchQuery(Sort: "title"));
        Assert.Equal("Hades", byTitle.Items[0].Title);

        var byRelease = await Service().SearchAsync(new GameSearchQuery(Sort: "release"));
        Assert.Equal("Hades", byRelease.Items[0].Title);
    }

    [Fact]
    public async Task Page_size_is_capped()
    {
        Seeded();
        var result = await Service().SearchAsync(new GameSearchQuery(PageSize: 9999));
        Assert.Equal(Paging.MaxPageSize, result.PageSize);
    }

    [Fact]
    public async Task Summaries_list_genres_and_platforms_alphabetically()
    {
        Seeded();
        var hades = (await Service().SearchAsync(new GameSearchQuery(Q: "hades"))).Items[0];
        Assert.Equal(["Action", "Roguelike"], hades.Genres.ToArray());
        Assert.Equal(["PC", "Switch"], hades.Platforms.ToArray());
    }

    [Fact]
    public async Task Detail_is_found_by_slug_and_unknown_slugs_are_404()
    {
        Seeded();
        var detail = await Service().GetBySlugAsync("hollow-knight");
        Assert.Equal(1, detail.CompletedCount);
        Assert.Equal(4.5, detail.AverageRating);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetBySlugAsync("nope"));
    }

    [Fact]
    public async Task Related_games_share_genres_and_exclude_the_game_itself()
    {
        Seeded();
        var related = await Service().RelatedAsync("hades");
        Assert.Equal("Hollow Knight", Assert.Single(related).Title);
    }
}
