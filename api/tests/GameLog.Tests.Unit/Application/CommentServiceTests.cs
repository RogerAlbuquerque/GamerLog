using GameLog.Application.Comments;
using GameLog.Application.Common;
using GameLog.Application.Ratings;
using GameLog.Application.Reviews;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Application;

public class CommentServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly Game game;
    private readonly User alice;
    private readonly User bob;

    public CommentServiceTests()
    {
        using var db = testDb.Create();
        game = Seed.Game(db, "Hades");
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
    }

    public void Dispose() => testDb.Dispose();

    private CommentService Svc()
    {
        var db = testDb.Create();
        return new CommentService(db, new GameLog.Application.Notifications.NotificationService(db, clock), clock);
    }

    private CommentTarget OnGame => CommentTarget.Game(game.Id);

    private static CreateCommentRequest Say(string text = "Nice!", Guid? parent = null, bool spoiler = false) => new(text, spoiler, parent);

    private async Task<Review> MakeReview()
    {
        var db = testDb.Create();
        var dto = await new ReviewService(db, new RatingService(db, clock, new GameLog.Application.Activity.ActivityService(db, clock)), new GameLog.Application.Activity.ActivityService(db, clock), new GameLog.Application.Notifications.NotificationService(db, clock), clock).SaveAsync(
            alice.Id, game.Id, new SaveReviewRequest("Title", "Long enough body.", false, null, null, null, null, null, null, null, null, null, null));
        await using var read = testDb.Create();
        return await read.Set<Review>().AsNoTracking().SingleAsync(r => r.Id == dto.Id);
    }

    [Fact]
    public async Task A_comment_is_created_on_a_game()
    {
        var dto = await Svc().CreateAsync(alice.Id, OnGame, Say("Great game"));
        Assert.Equal("Great game", dto.Body);
        Assert.Equal("alice", dto.Author!.Username);
        Assert.Null(dto.ParentId);
    }

    [Fact]
    public async Task Empty_comments_are_rejected() =>
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().CreateAsync(alice.Id, OnGame, Say("  ")));

    [Fact]
    public async Task Commenting_on_an_unknown_target_is_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().CreateAsync(alice.Id, CommentTarget.Game(Guid.NewGuid()), Say()));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().CreateAsync(alice.Id, CommentTarget.Review(Guid.NewGuid()), Say()));
    }

    [Fact]
    public async Task Replies_attach_to_the_root_even_when_replying_to_a_reply()
    {
        var root = await Svc().CreateAsync(alice.Id, OnGame, Say("root"));
        var reply = await Svc().CreateAsync(bob.Id, OnGame, Say("reply", root.Id));
        var nested = await Svc().CreateAsync(alice.Id, OnGame, Say("nested", reply.Id));

        Assert.Equal(root.Id, reply.ParentId);
        Assert.Equal(root.Id, nested.ParentId);
        var replies = await Svc().RepliesAsync(root.Id, null, null, null);
        Assert.Equal(["reply", "nested"], replies.Items.Select(r => r.Body).ToArray());
        Assert.Equal(2, (await Svc().GetAsync(root.Id, null)).ReplyCount);
    }

    [Fact]
    public async Task Replies_must_belong_to_the_same_target()
    {
        var review = await MakeReview();
        var onReview = await Svc().CreateAsync(alice.Id, CommentTarget.Review(review.Id), Say("on review"));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().CreateAsync(bob.Id, OnGame, Say("cross", onReview.Id)));
    }

    [Fact]
    public async Task Review_comment_counts_follow_creation_and_deletion()
    {
        var review = await MakeReview();
        var c = await Svc().CreateAsync(bob.Id, CommentTarget.Review(review.Id), Say());
        Assert.Equal(1, (await ReviewCount(review.Id)));
        await Svc().DeleteAsync(bob.Id, false, c.Id);
        Assert.Equal(0, (await ReviewCount(review.Id)));
    }

    private async Task<int> ReviewCount(Guid reviewId)
    {
        await using var db = testDb.Create();
        return await db.Set<Review>().AsNoTracking().Where(r => r.Id == reviewId).Select(r => r.CommentCount).SingleAsync();
    }

    [Fact]
    public async Task Only_the_author_can_edit_and_the_edit_is_marked()
    {
        var c = await Svc().CreateAsync(alice.Id, OnGame, Say("first"));
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().EditAsync(bob.Id, c.Id, new EditCommentRequest("hacked", false)));

        clock.Advance(TimeSpan.FromMinutes(5));
        var edited = await Svc().EditAsync(alice.Id, c.Id, new EditCommentRequest("second", true));
        Assert.Equal("second", edited.Body);
        Assert.True(edited.IsSpoiler);
        Assert.NotNull(edited.EditedAt);
    }

    [Fact]
    public async Task A_deleted_comment_without_replies_disappears_from_the_list()
    {
        var c = await Svc().CreateAsync(alice.Id, OnGame, Say());
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().DeleteAsync(bob.Id, false, c.Id));
        await Svc().DeleteAsync(alice.Id, false, c.Id);

        Assert.Empty((await Svc().ListAsync(OnGame, null, null, null, null)).Items);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().DeleteAsync(alice.Id, false, c.Id));
    }

    [Fact]
    public async Task A_deleted_root_with_replies_stays_as_an_anonymous_placeholder()
    {
        var root = await Svc().CreateAsync(alice.Id, OnGame, Say("secret text"));
        await Svc().CreateAsync(bob.Id, OnGame, Say("reply", root.Id));
        await Svc().DeleteAsync(alice.Id, false, root.Id);

        var list = await Svc().ListAsync(OnGame, null, null, null, null);
        var placeholder = Assert.Single(list.Items);
        Assert.True(placeholder.IsDeleted);
        Assert.Equal("", placeholder.Body);
        Assert.Null(placeholder.Author);
        Assert.Equal(1, placeholder.ReplyCount);
    }

    [Fact]
    public async Task Admins_can_delete_any_comment()
    {
        var c = await Svc().CreateAsync(alice.Id, OnGame, Say());
        await Svc().DeleteAsync(bob.Id, true, c.Id);
        Assert.Empty((await Svc().ListAsync(OnGame, null, null, null, null)).Items);
    }

    [Fact]
    public async Task Likes_are_idempotent_and_tracked_per_viewer()
    {
        var c = await Svc().CreateAsync(alice.Id, OnGame, Say());
        await Svc().LikeAsync(bob.Id, c.Id);
        var again = await Svc().LikeAsync(bob.Id, c.Id);
        Assert.Equal(1, again.LikeCount);

        Assert.True((await Svc().GetAsync(c.Id, bob.Id)).LikedByMe);
        Assert.False((await Svc().GetAsync(c.Id, alice.Id)).LikedByMe);
        Assert.Equal(0, (await Svc().UnlikeAsync(bob.Id, c.Id)).LikeCount);
    }

    [Fact]
    public async Task Comments_sort_by_newest_or_top()
    {
        var first = await Svc().CreateAsync(alice.Id, OnGame, Say("first"));
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = await Svc().CreateAsync(bob.Id, OnGame, Say("second"));
        await Svc().LikeAsync(bob.Id, first.Id);

        var newest = await Svc().ListAsync(OnGame, CommentSort.Newest, null, null, null);
        var top = await Svc().ListAsync(OnGame, CommentSort.Top, null, null, null);
        Assert.Equal([second.Id, first.Id], newest.Items.Select(c => c.Id).ToArray());
        Assert.Equal([first.Id, second.Id], top.Items.Select(c => c.Id).ToArray());
    }

    [Fact]
    public async Task Listing_is_paginated_and_replies_are_not_roots()
    {
        var root = await Svc().CreateAsync(alice.Id, OnGame, Say("root"));
        await Svc().CreateAsync(bob.Id, OnGame, Say("reply", root.Id));
        for (var i = 0; i < 3; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(1));
            await Svc().CreateAsync(bob.Id, OnGame, Say($"c{i}"));
        }

        var page = await Svc().ListAsync(OnGame, null, null, 2, 2);
        Assert.Equal(4, page.Total);
        Assert.Equal(2, page.Items.Count);
    }
}
