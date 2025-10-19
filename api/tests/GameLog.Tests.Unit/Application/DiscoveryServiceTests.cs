using GameLog.Application.Discovery;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Tests.Unit.Support;

namespace GameLog.Tests.Unit.Application;

public class DiscoveryServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => testDb.Dispose();

    private DiscoveryService Svc() => new(testDb.Create(), clock);

    private Game AddGame(string title, int ratingCount = 0, int ratingSum = 0, int completed = 0, DateOnly? release = null)
    {
        using var db = testDb.Create();
        var g = Seed.Game(db, title);
        g.RatingCount = ratingCount;
        g.RatingSum = ratingSum;
        g.CompletedCount = completed;
        g.ReleaseDate = release;
        db.SaveChanges();
        return g;
    }

    [Fact]
    public async Task Top_rated_prefers_many_good_ratings_over_one_perfect_rating()
    {
        AddGame("One Vote", 1, 10);
        AddGame("Crowd Favourite", 100, 880);   // 4.4 stars from 100 ratings

        var rail = (await Svc().OverviewAsync()).TopRated;
        Assert.Equal(["Crowd Favourite", "One Vote"], rail.Select(g => g.Title).ToArray());
    }

    [Fact]
    public async Task Unrated_games_are_not_top_rated()
    {
        AddGame("Nobody Rated");
        Assert.Empty((await Svc().OverviewAsync()).TopRated);
    }

    [Fact]
    public async Task New_releases_are_recent_and_already_out()
    {
        AddGame("Fresh", release: new DateOnly(2025, 9, 1));
        AddGame("Old", release: new DateOnly(2020, 1, 1));
        AddGame("Upcoming", release: new DateOnly(2026, 3, 1));
        AddGame("Undated");

        Assert.Equal(["Fresh"], (await Svc().OverviewAsync()).NewReleases.Select(g => g.Title).ToArray());
    }

    [Fact]
    public async Task Most_completed_is_ordered_by_completions()
    {
        AddGame("A", completed: 3);
        AddGame("B", completed: 9);
        AddGame("C");

        Assert.Equal(["B", "A"], (await Svc().OverviewAsync()).MostCompleted.Select(g => g.Title).ToArray());
    }

    [Fact]
    public async Task Trending_counts_recent_diary_additions_only()
    {
        var hot = AddGame("Hot");
        var stale = AddGame("Stale");
        using (var db = testDb.Create())
        {
            var u1 = Seed.User(db, "u1");
            var u2 = Seed.User(db, "u2");
            db.Set<GamingDiaryEntry>().AddRange(
                new GamingDiaryEntry { UserId = u1.Id, GameId = hot.Id, Status = DiaryStatus.Playing, CreatedAt = clock.GetUtcNow().UtcDateTime.AddDays(-2) },
                new GamingDiaryEntry { UserId = u2.Id, GameId = hot.Id, Status = DiaryStatus.Playing, CreatedAt = clock.GetUtcNow().UtcDateTime.AddDays(-1) },
                new GamingDiaryEntry { UserId = u1.Id, GameId = stale.Id, Status = DiaryStatus.Completed, CreatedAt = clock.GetUtcNow().UtcDateTime.AddDays(-60) });
            db.SaveChanges();
        }

        Assert.Equal(["Hot"], (await Svc().OverviewAsync()).Trending.Select(g => g.Title).ToArray());
    }

    [Fact]
    public async Task Recommendations_never_include_games_already_in_the_diary_and_explain_themselves()
    {
        var rpg = Seed.Genre(testDb.Create(), "RPG");
        Game owned, same, other;
        User user;
        using (var db = testDb.Create())
        {
            user = Seed.User(db, "roger");
            owned = Seed.Game(db, "Owned");
            same = Seed.Game(db, "Same Genre");
            other = Seed.Game(db, "Other Genre");
            db.Set<GameGenre>().AddRange(new GameGenre { GameId = owned.Id, GenreId = rpg.Id }, new GameGenre { GameId = same.Id, GenreId = rpg.Id });
            db.Set<GamingDiaryEntry>().Add(new GamingDiaryEntry { UserId = user.Id, GameId = owned.Id, Status = DiaryStatus.Completed, CreatedAt = Seed.Now });
            db.Set<Rating>().Add(new Rating { UserId = user.Id, GameId = owned.Id, HalfStars = 10, CreatedAt = Seed.Now, UpdatedAt = Seed.Now });
            db.SaveChanges();
        }

        var recs = await Svc().RecommendAsync(user.Id);

        Assert.DoesNotContain(recs, r => r.Game.Title == "Owned");
        Assert.Equal("Same Genre", recs[0].Game.Title);
        Assert.Contains("RPG", recs[0].Because);
        Assert.Equal("Other Genre", recs[1].Game.Title);
    }

    [Fact]
    public async Task A_user_with_an_empty_diary_still_gets_suggestions()
    {
        AddGame("Popular", 50, 400);
        User user;
        using (var db = testDb.Create())
        {
            user = Seed.User(db, "newbie");
        }

        Assert.Single(await Svc().RecommendAsync(user.Id));
    }
}
