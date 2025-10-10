using GameLog.Application.Comments;
using GameLog.Application.Common;
using GameLog.Application.Notifications;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Tests.Unit.Support;

namespace GameLog.Tests.Unit.Application;

public class NotificationServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 9, 12, 0, 0, TimeSpan.Zero));
    private readonly Game game;
    private readonly User alice;
    private readonly User bob;

    public NotificationServiceTests()
    {
        using var db = testDb.Create();
        game = Seed.Game(db, "Hades");
        alice = Seed.User(db, "alice");
        bob = Seed.User(db, "bob");
    }

    public void Dispose() => testDb.Dispose();

    private NotificationService Svc() => new(testDb.Create(), clock);

    private CommentService Comments()
    {
        var db = testDb.Create();
        return new CommentService(db, new NotificationService(db, clock), clock);
    }

    [Fact]
    public async Task Replying_to_a_comment_notifies_its_author()
    {
        var root = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().CreateAsync(bob.Id, CommentTarget.Game(game.Id), new("hi", false, root.Id));

        var list = await Svc().ListAsync(alice.Id, false, null, null);
        var n = Assert.Single(list.Items);
        Assert.Equal(NotificationType.CommentReply, n.Type);
        Assert.Equal("bob", n.Actor.Username);
        Assert.Equal("hades", n.GameSlug);
        Assert.False(n.IsRead);
    }

    [Fact]
    public async Task Replying_to_yourself_does_not_notify()
    {
        var root = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("me again", false, root.Id));
        Assert.Equal(0, (await Svc().UnreadCountAsync(alice.Id)).Count);
    }

    [Fact]
    public async Task Liking_a_comment_notifies_once_even_when_toggled()
    {
        var c = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().LikeAsync(bob.Id, c.Id);
        await Comments().UnlikeAsync(bob.Id, c.Id);
        await Comments().LikeAsync(bob.Id, c.Id);

        Assert.Equal(1, (await Svc().UnreadCountAsync(alice.Id)).Count);
    }

    [Fact]
    public async Task Marking_as_read_updates_the_unread_count_and_filter()
    {
        var c = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().LikeAsync(bob.Id, c.Id);
        var id = (await Svc().ListAsync(alice.Id, true, null, null)).Items.Single().Id;

        await Svc().MarkReadAsync(alice.Id, id);

        Assert.Equal(0, (await Svc().UnreadCountAsync(alice.Id)).Count);
        Assert.Empty((await Svc().ListAsync(alice.Id, true, null, null)).Items);
        Assert.Single((await Svc().ListAsync(alice.Id, false, null, null)).Items);
    }

    [Fact]
    public async Task Users_cannot_read_or_mark_each_others_notifications()
    {
        var c = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().LikeAsync(bob.Id, c.Id);
        var id = (await Svc().ListAsync(alice.Id, false, null, null)).Items.Single().Id;

        Assert.Empty((await Svc().ListAsync(bob.Id, false, null, null)).Items);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().MarkReadAsync(bob.Id, id));
        await Svc().MarkAllReadAsync(bob.Id);
        Assert.Equal(1, (await Svc().UnreadCountAsync(alice.Id)).Count);
    }

    [Fact]
    public async Task Mark_all_read_clears_everything_for_the_caller()
    {
        var c = await Comments().CreateAsync(alice.Id, CommentTarget.Game(game.Id), new("hello", false, null));
        await Comments().LikeAsync(bob.Id, c.Id);
        await Comments().CreateAsync(bob.Id, CommentTarget.Game(game.Id), new("reply", false, c.Id));

        await Svc().MarkAllReadAsync(alice.Id);
        Assert.Equal(0, (await Svc().UnreadCountAsync(alice.Id)).Count);
    }
}
