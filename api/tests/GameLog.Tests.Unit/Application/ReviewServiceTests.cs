using GameLog.Application.Common;
using GameLog.Application.Ratings;
using GameLog.Application.Reviews;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Application;

public class ReviewServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly Game game;
    private readonly User alice;
    private readonly User bob;

    public ReviewServiceTests()
    {
        using var db = testDb.Create();
        game = Seed.Game(db, "Hades");
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
    }

    public void Dispose() => testDb.Dispose();

    private ReviewService Svc()
    {
        var db = testDb.Create();
        return new ReviewService(db, new RatingService(db, clock, new GameLog.Application.Activity.ActivityService(db, clock)), clock);
    }

    private static SaveReviewRequest Req(string title = "Brilliant", string body = "Every run feels fresh.", int? stars = null, bool spoilers = false) =>
        new(title, body, spoilers, stars, null, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Saving_creates_a_review_with_its_author_and_game()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req());

        Assert.Equal("Brilliant", dto.Title);
        Assert.Equal("alice", dto.Author.Username);
        Assert.Equal("hades", dto.GameSlug);
        Assert.Null(dto.RatingHalfStars);
        Assert.Equal(0, dto.LikeCount);
    }

    [Fact]
    public async Task Saving_twice_updates_the_same_review()
    {
        var first = await Svc().SaveAsync(alice.Id, game.Id, Req());
        var second = await Svc().SaveAsync(alice.Id, game.Id, Req(title: "Even better"));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Even better", second.Title);
        await using var db = testDb.Create();
        Assert.Equal(1, await db.Set<Review>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Publishing_with_stars_sets_the_game_rating()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req(stars: 9));

        Assert.Equal(9, dto.RatingHalfStars);
        await using var db = testDb.Create();
        var g = await db.Set<Game>().AsNoTracking().SingleAsync(x => x.Id == game.Id);
        Assert.Equal(1, g.RatingCount);
        Assert.Equal(9, g.RatingSum);
    }

    [Fact]
    public async Task Editing_without_stars_keeps_the_existing_rating()
    {
        await Svc().SaveAsync(alice.Id, game.Id, Req(stars: 8));
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req(title: "Edited"));
        Assert.Equal(8, dto.RatingHalfStars);
    }

    [Fact]
    public async Task An_invalid_rating_prevents_the_review_from_being_saved()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().SaveAsync(alice.Id, game.Id, Req(stars: 11)));
        await using var db = testDb.Create();
        Assert.Equal(0, await db.Set<Review>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Invalid_content_is_rejected()
    {
        var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().SaveAsync(alice.Id, game.Id, Req(title: " ", body: "x")));
        Assert.Contains("title", ex.Errors.Keys);
        Assert.Contains("body", ex.Errors.Keys);
    }

    [Fact]
    public async Task Reviewing_an_unknown_game_is_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().SaveAsync(alice.Id, Guid.NewGuid(), Req()));

    [Fact]
    public async Task The_spoiler_flag_is_stored_and_returned()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req(spoilers: true));
        Assert.True(dto.ContainsSpoilers);
    }

    [Fact]
    public async Task Only_the_author_can_delete_a_review()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req());

        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().DeleteAsync(bob.Id, false, dto.Id));
        await Svc().DeleteAsync(alice.Id, false, dto.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(dto.Id, null));
    }

    [Fact]
    public async Task Admins_can_moderate_any_review()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req());
        await Svc().DeleteAsync(bob.Id, true, dto.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(dto.Id, null));
    }

    [Fact]
    public async Task Likes_are_idempotent_and_counted()
    {
        var dto = await Svc().SaveAsync(alice.Id, game.Id, Req());

        await Svc().LikeAsync(bob.Id, dto.Id);
        var again = await Svc().LikeAsync(bob.Id, dto.Id);
        Assert.Equal(1, again.LikeCount);

        var seenByBob = await Svc().GetAsync(dto.Id, bob.Id);
        var seenByAlice = await Svc().GetAsync(dto.Id, alice.Id);
        Assert.True(seenByBob.LikedByMe);
        Assert.False(seenByAlice.LikedByMe);

        var removed = await Svc().UnlikeAsync(bob.Id, dto.Id);
        Assert.Equal(0, removed.LikeCount);
        Assert.False(removed.Liked);
    }

    [Fact]
    public async Task Liking_or_unliking_an_unknown_review_is_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().LikeAsync(bob.Id, Guid.NewGuid()));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().UnlikeAsync(bob.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Game_reviews_can_be_sorted_by_popularity()
    {
        var a = await Svc().SaveAsync(alice.Id, game.Id, Req(title: "Alice"));
        clock.Advance(TimeSpan.FromHours(1));
        var b = await Svc().SaveAsync(bob.Id, game.Id, Req(title: "Bob"));
        await Svc().LikeAsync(bob.Id, a.Id);

        var newest = await Svc().ListForGameAsync(game.Id, ReviewSort.Newest, null, null, null);
        var popular = await Svc().ListForGameAsync(game.Id, ReviewSort.Popular, null, null, null);

        Assert.Equal([b.Id, a.Id], newest.Items.Select(r => r.Id).ToArray());
        Assert.Equal([a.Id, b.Id], popular.Items.Select(r => r.Id).ToArray());
        Assert.Equal(2, newest.Total);
    }

    [Fact]
    public async Task A_users_reviews_are_listed_by_username_and_unknown_users_are_404()
    {
        await Svc().SaveAsync(alice.Id, game.Id, Req());
        var list = await Svc().ListForUserAsync("ALICE", null, null, null, null);
        Assert.Single(list.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().ListForUserAsync("nobody", null, null, null, null));
    }
}
