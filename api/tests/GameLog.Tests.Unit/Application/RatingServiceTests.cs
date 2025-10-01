using GameLog.Application.Common;
using GameLog.Application.Games;
using GameLog.Application.Ratings;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Application;

public class RatingServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly Game game;
    private readonly User[] users;

    public RatingServiceTests()
    {
        using var db = testDb.Create();
        game = Seed.Game(db, "Hades");
        users = Enumerable.Range(1, 4).Select(i => Seed.User(db, $"user{i}")).ToArray();
    }

    public void Dispose() => testDb.Dispose();

    private RatingService Svc() => new(testDb.Create(), clock);

    private async Task<Game> Reload()
    {
        await using var db = testDb.Create();
        return await db.Set<Game>().AsNoTracking().SingleAsync(g => g.Id == game.Id);
    }

    [Fact]
    public async Task Rating_a_game_updates_the_community_aggregates()
    {
        var dto = await Svc().SetAsync(users[0].Id, game.Id, 9);

        Assert.Equal(4.5, dto.Stars);
        var g = await Reload();
        Assert.Equal(1, g.RatingCount);
        Assert.Equal(9, g.RatingSum);
        Assert.Equal(4.5, g.AverageRating);
    }

    [Fact]
    public async Task Rating_again_replaces_the_previous_rating_instead_of_adding_one()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 9);
        await Svc().SetAsync(users[0].Id, game.Id, 4);

        var g = await Reload();
        Assert.Equal(1, g.RatingCount);
        Assert.Equal(4, g.RatingSum);
        await using var db = testDb.Create();
        Assert.Equal(1, await db.Set<Rating>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task The_average_covers_all_users()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 10);
        await Svc().SetAsync(users[1].Id, game.Id, 7);
        await Svc().SetAsync(users[2].Id, game.Id, 1);

        var g = await Reload();
        Assert.Equal(3, g.RatingCount);
        Assert.Equal(3.0, g.AverageRating);   // (10 + 7 + 1) / 2 / 3
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task Out_of_range_ratings_are_rejected_and_not_stored(int halfStars)
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().SetAsync(users[0].Id, game.Id, halfStars));
        Assert.Equal(0, (await Reload()).RatingCount);
    }

    [Fact]
    public async Task Removing_a_rating_updates_the_aggregates()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 10);
        await Svc().SetAsync(users[1].Id, game.Id, 6);
        await Svc().RemoveAsync(users[0].Id, game.Id);

        var g = await Reload();
        Assert.Equal(1, g.RatingCount);
        Assert.Equal(6, g.RatingSum);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().RemoveAsync(users[0].Id, game.Id));
    }

    [Fact]
    public async Task Users_cannot_remove_or_read_each_others_ratings()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 10);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().RemoveAsync(users[1].Id, game.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetMineAsync(users[1].Id, game.Id));
        Assert.Equal(1, (await Reload()).RatingCount);
    }

    [Fact]
    public async Task The_summary_reports_average_count_and_distribution()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 10);
        await Svc().SetAsync(users[1].Id, game.Id, 10);
        await Svc().SetAsync(users[2].Id, game.Id, 7);
        await Svc().SetAsync(users[3].Id, game.Id, 1);

        var s = await Svc().SummaryAsync(game.Id);
        Assert.Equal(4, s.Count);
        Assert.Equal(7.0 / 4 * 0 + 28 / 2.0 / 4, s.Average);  // 28 half-stars / 2 / 4 = 3.5
        Assert.Equal(10, s.Distribution.Count);
        Assert.Equal(1, s.Distribution[0]);   // one 0.5-star rating
        Assert.Equal(1, s.Distribution[6]);   // one 3.5-star rating
        Assert.Equal(2, s.Distribution[9]);   // two 5-star ratings
        Assert.Equal(4, s.Distribution.Sum());
    }

    [Fact]
    public async Task An_unrated_game_has_no_average()
    {
        var s = await Svc().SummaryAsync(game.Id);
        Assert.Null(s.Average);
        Assert.Equal(0, s.Count);
        Assert.Equal(new int[10], s.Distribution.ToArray());
    }

    [Fact]
    public async Task The_stored_aggregate_always_matches_the_summary()
    {
        await Svc().SetAsync(users[0].Id, game.Id, 9);
        await Svc().SetAsync(users[1].Id, game.Id, 3);
        await Svc().SetAsync(users[1].Id, game.Id, 8);
        await Svc().RemoveAsync(users[0].Id, game.Id);
        await Svc().SetAsync(users[2].Id, game.Id, 5);

        var summary = await Svc().SummaryAsync(game.Id);
        var detail = await new GameService(testDb.Create()).GetBySlugAsync("hades");
        Assert.Equal(summary.Average, detail.AverageRating);
        Assert.Equal(summary.Count, detail.RatingCount);
    }

    [Fact]
    public async Task Rating_an_unknown_game_is_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().SetAsync(users[0].Id, Guid.NewGuid(), 5));
}
