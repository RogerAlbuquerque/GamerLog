using GameLog.Domain.Enums;
using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class DiaryRulesTests
{
    private static readonly DateOnly Today = new(2025, 10, 1);

    private static string[] Fields(DiaryStatus s, DateOnly? start, DateOnly? done, int minutes) =>
        DiaryRules.Validate(s, start, done, minutes, Today).Select(v => v.Field).ToArray();

    [Fact]
    public void A_completed_game_with_consistent_dates_is_valid() =>
        Assert.Empty(Fields(DiaryStatus.Completed, new(2025, 9, 1), new(2025, 9, 20), 1800));

    [Fact]
    public void Completion_before_start_is_rejected() =>
        Assert.Contains("completedOn", Fields(DiaryStatus.Completed, new(2025, 9, 20), new(2025, 9, 1), 60));

    [Theory]
    [InlineData(DiaryStatus.Playing)]
    [InlineData(DiaryStatus.Abandoned)]
    [InlineData(DiaryStatus.OnHold)]
    [InlineData(DiaryStatus.WantToPlay)]
    public void A_completion_date_requires_the_completed_status(DiaryStatus status) =>
        Assert.Contains("completedOn", Fields(status, null, new(2025, 9, 1), 0));

    [Fact]
    public void Dates_in_the_future_are_rejected()
    {
        Assert.Contains("startedOn", Fields(DiaryStatus.Playing, Today.AddDays(1), null, 0));
        Assert.Contains("completedOn", Fields(DiaryStatus.Completed, null, Today.AddDays(1), 0));
    }

    [Fact]
    public void Today_is_allowed() =>
        Assert.Empty(Fields(DiaryStatus.Completed, Today, Today, 10));

    [Theory]
    [InlineData(-1)]
    [InlineData(DiaryRules.MaxPlaytimeMinutes + 1)]
    public void Playtime_must_be_within_bounds(int minutes) =>
        Assert.Contains("totalPlaytimeMinutes", Fields(DiaryStatus.Playing, null, null, minutes));

    [Fact]
    public void A_wishlist_game_cannot_have_progress()
    {
        Assert.Contains("status", Fields(DiaryStatus.WantToPlay, new(2025, 9, 1), null, 0));
        Assert.Contains("status", Fields(DiaryStatus.WantToPlay, null, null, 30));
        Assert.Empty(Fields(DiaryStatus.WantToPlay, null, null, 0));
    }

    [Theory]
    [InlineData(DiaryStatus.WantToPlay, DiaryStatus.Playing, true)]
    [InlineData(DiaryStatus.WantToPlay, DiaryStatus.Completed, true)]
    [InlineData(DiaryStatus.Playing, DiaryStatus.Completed, true)]
    [InlineData(DiaryStatus.Playing, DiaryStatus.OnHold, true)]
    [InlineData(DiaryStatus.OnHold, DiaryStatus.Playing, true)]
    [InlineData(DiaryStatus.Abandoned, DiaryStatus.Playing, true)]
    [InlineData(DiaryStatus.Completed, DiaryStatus.Playing, true)]
    [InlineData(DiaryStatus.Playing, DiaryStatus.WantToPlay, false)]
    [InlineData(DiaryStatus.Completed, DiaryStatus.WantToPlay, false)]
    [InlineData(DiaryStatus.Abandoned, DiaryStatus.WantToPlay, false)]
    [InlineData(DiaryStatus.WantToPlay, DiaryStatus.WantToPlay, true)]
    public void Status_transitions(DiaryStatus from, DiaryStatus to, bool allowed) =>
        Assert.Equal(allowed, DiaryRules.IsTransitionAllowed(from, to));
}
