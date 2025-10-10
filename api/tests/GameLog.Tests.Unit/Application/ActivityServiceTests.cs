using GameLog.Application.Activity;
using GameLog.Application.Common;
using GameLog.Application.Diary;
using GameLog.Application.Social;
using GameLog.Application.Notifications;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Tests.Unit.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Application;

public class ActivityServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 9, 12, 0, 0, TimeSpan.Zero));
    private readonly Game game;
    private readonly User alice;
    private readonly User bob;
    private readonly User carol;

    public ActivityServiceTests()
    {
        using var db = testDb.Create();
        game = Seed.Game(db, "Hades");
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
        carol = Seed.User(db, "carol");
    }

    public void Dispose() => testDb.Dispose();

    private ActivityService Svc() => new(testDb.Create(), clock);

    private async Task Stage(Guid actor, ActivityType type, int? value = null)
    {
        var db = testDb.Create();
        await new ActivityService(db, clock).StageAsync(actor, type, game.Id, value: value);
        await db.SaveChangesAsync();
    }

    private async Task Follow(Guid follower, string username)
    {
        var db = testDb.Create();
        await new FollowService(db, new NotificationService(db, clock), clock).FollowAsync(follower, username);
    }

    [Fact]
    public async Task The_feed_contains_followed_users_and_yourself_but_not_strangers()
    {
        await Follow(alice.Id, "bob");
        await Stage(bob.Id, ActivityType.GameCompleted);
        await Stage(alice.Id, ActivityType.GameAddedToDiary);
        await Stage(carol.Id, ActivityType.GameCompleted);

        var feed = await Svc().FeedAsync(alice.Id, null, null);
        Assert.Equal(["alice", "bob"], feed.Items.Select(i => i.Actor.Username).Order().ToArray());
    }

    [Fact]
    public async Task The_feed_is_newest_first()
    {
        await Stage(alice.Id, ActivityType.GameAddedToDiary);
        clock.Advance(TimeSpan.FromMinutes(5));
        await Stage(alice.Id, ActivityType.GameCompleted);

        var feed = await Svc().FeedAsync(alice.Id, null, null);
        Assert.Equal([ActivityType.GameCompleted, ActivityType.GameAddedToDiary], feed.Items.Select(i => i.Type).ToArray());
    }

    [Fact]
    public async Task Repeating_an_action_within_a_day_refreshes_instead_of_duplicating()
    {
        await Stage(alice.Id, ActivityType.GameRated, 6);
        clock.Advance(TimeSpan.FromHours(2));
        await Stage(alice.Id, ActivityType.GameRated, 9);

        var item = Assert.Single((await Svc().ForUserAsync("alice", null, null)).Items);
        Assert.Equal(9, item.RatingHalfStars);
    }

    [Fact]
    public async Task The_same_action_a_day_later_is_a_new_item()
    {
        await Stage(alice.Id, ActivityType.GameCompleted);
        clock.Advance(TimeSpan.FromHours(30));
        await Stage(alice.Id, ActivityType.GameCompleted);
        Assert.Equal(2, (await Svc().ForUserAsync("alice", null, null)).Items.Count);
    }

    [Fact]
    public async Task Completing_a_game_in_the_diary_creates_both_activity_items()
    {
        var db = testDb.Create();
        var diary = new DiaryService(db, clock, new ActivityService(db, clock), NullLogger<DiaryService>.Instance);
        await diary.UpsertAsync(alice.Id, game.Id, new UpsertDiaryRequest(DiaryStatus.Completed, null, null, null, 600, null, null));

        var items = (await Svc().ForUserAsync("alice", null, null)).Items;
        Assert.Contains(items, i => i.Type == ActivityType.GameAddedToDiary);
        Assert.Contains(items, i => i.Type == ActivityType.GameCompleted && i.Game!.Slug == "hades");
    }

    [Fact]
    public async Task An_unknown_users_activity_is_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().ForUserAsync("ghost", null, null));
}
