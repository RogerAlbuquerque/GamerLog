using GameLog.Application.Common;
using GameLog.Application.Notifications;
using GameLog.Application.Social;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Application;

public class FollowServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 7, 12, 0, 0, TimeSpan.Zero));
    private readonly User alice;
    private readonly User bob;

    public FollowServiceTests()
    {
        using var db = testDb.Create();
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
    }

    public void Dispose() => testDb.Dispose();

    private FollowService Svc()
    {
        var db = testDb.Create();
        return new FollowService(db, new NotificationService(db, clock), clock);
    }

    private async Task<List<Notification>> Notifications()
    {
        await using var db = testDb.Create();
        return await db.Set<Notification>().AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Following_is_idempotent_and_counts_are_correct()
    {
        await Svc().FollowAsync(alice.Id, "bob");
        var state = await Svc().FollowAsync(alice.Id, "BOB");

        Assert.True(state.Following);
        Assert.Equal(1, (await Svc().StateAsync("bob", null)).Followers);
        Assert.Equal(1, (await Svc().StateAsync("alice", null)).FollowingCount);
    }

    [Fact]
    public async Task Following_notifies_the_followee_once()
    {
        await Svc().FollowAsync(alice.Id, "bob");
        await Svc().UnfollowAsync(alice.Id, "bob");
        await Svc().FollowAsync(alice.Id, "bob");

        var list = await Notifications();
        var n = Assert.Single(list);
        Assert.Equal(bob.Id, n.RecipientId);
        Assert.Equal(alice.Id, n.ActorId);
        Assert.Equal(NotificationType.NewFollower, n.Type);
    }

    [Fact]
    public async Task You_cannot_follow_yourself() =>
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().FollowAsync(alice.Id, "alice"));

    [Fact]
    public async Task Following_an_unknown_user_is_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().FollowAsync(alice.Id, "ghost"));

    [Fact]
    public async Task Unfollowing_removes_the_relationship_and_is_safe_to_repeat()
    {
        await Svc().FollowAsync(alice.Id, "bob");
        await Svc().UnfollowAsync(alice.Id, "bob");
        var state = await Svc().UnfollowAsync(alice.Id, "bob");

        Assert.False(state.Following);
        Assert.Equal(0, state.Followers);
    }

    [Fact]
    public async Task Follower_and_following_lists_show_the_viewers_relationship()
    {
        await Svc().FollowAsync(alice.Id, "bob");

        var followers = await Svc().FollowersAsync("bob", alice.Id, null, null);
        var following = await Svc().FollowingAsync("alice", alice.Id, null, null);
        Assert.Equal("alice", Assert.Single(followers.Items).Username);
        var b = Assert.Single(following.Items);
        Assert.Equal("bob", b.Username);
        Assert.True(b.IsFollowedByMe);
        Assert.False(Assert.Single((await Svc().FollowersAsync("bob", null, null, null)).Items).IsFollowedByMe);
    }
}
