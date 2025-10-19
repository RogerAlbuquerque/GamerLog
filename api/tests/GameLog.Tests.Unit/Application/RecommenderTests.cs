using GameLog.Application.Discovery;
using GameLog.Domain.Enums;

namespace GameLog.Tests.Unit.Application;

public class RecommenderTests
{
    private static TasteSignal Signal(DiaryStatus status, int? stars, params string[] genres) => new(status, stars, genres, ["PC"]);

    private static Candidate Cand(string title, string[] genres, double? avg = null, int count = 0, string[]? platforms = null) =>
        new(Guid.NewGuid(), title, genres, platforms ?? ["PC"], avg, count);

    [Fact]
    public void Loved_completed_games_are_the_strongest_signal()
    {
        var loved = Recommender.SignalStrength(Signal(DiaryStatus.Completed, 10));
        var meh = Recommender.SignalStrength(Signal(DiaryStatus.Completed, 4));
        var abandoned = Recommender.SignalStrength(Signal(DiaryStatus.Abandoned, 2));
        Assert.True(loved > meh);
        Assert.True(meh > abandoned);
        Assert.True(abandoned < 0);
    }

    [Fact]
    public void A_neutral_rating_adds_nothing_beyond_the_status()
    {
        Assert.Equal(1.0, Recommender.SignalStrength(Signal(DiaryStatus.Completed, 6)));
        Assert.Equal(1.0, Recommender.SignalStrength(Signal(DiaryStatus.Completed, null)));
    }

    [Fact]
    public void Games_in_liked_genres_rank_above_games_in_other_genres()
    {
        var signals = new[] { Signal(DiaryStatus.Completed, 10, "RPG"), Signal(DiaryStatus.Completed, 9, "RPG", "Adventure") };
        var ranked = Recommender.Rank(signals, [Cand("Sports Game", ["Sports"]), Cand("Fantasy RPG", ["RPG"])]);
        Assert.Equal(2, ranked.Count);
        Assert.Contains("RPG", ranked[0].Because);
    }

    [Fact]
    public void Disliked_genres_are_pushed_down()
    {
        var signals = new[] { Signal(DiaryStatus.Abandoned, 1, "Sports"), Signal(DiaryStatus.Completed, 9, "RPG") };
        var sports = Cand("Sports Game", ["Sports"]);
        var puzzle = Cand("Puzzle Game", ["Puzzle"]);
        var ranked = Recommender.Rank(signals, [sports, puzzle]);
        Assert.Equal(puzzle.GameId, ranked[0].GameId);
        Assert.Empty(ranked[1].Because);
    }

    [Fact]
    public void Quality_breaks_ties_between_equal_taste_matches()
    {
        var signals = new[] { Signal(DiaryStatus.Completed, 8, "RPG") };
        var good = Cand("Good", ["RPG"], 4.8, 50);
        var poor = Cand("Poor", ["RPG"], 2.0, 50);
        Assert.Equal(good.GameId, Recommender.Rank(signals, [poor, good])[0].GameId);
    }

    [Fact]
    public void A_single_enthusiastic_rating_does_not_count_as_full_quality()
    {
        var signals = Array.Empty<TasteSignal>();
        var one = Cand("OneVote", [], 5.0, 1);
        var many = Cand("ManyVotes", [], 4.0, 100);
        Assert.Equal(many.GameId, Recommender.Rank(signals, [one, many])[0].GameId);
    }

    [Fact]
    public void A_new_user_still_gets_quality_ranked_suggestions()
    {
        var ranked = Recommender.Rank([], [Cand("A", ["RPG"], 3.0, 40), Cand("B", ["RPG"], 4.5, 40)]);
        Assert.Equal("B", ranked.Select(r => r.GameId).Select(id => id == ranked[0].GameId ? "B" : "A").First());
        Assert.All(ranked, r => Assert.Empty(r.Because));
    }

    [Fact]
    public void The_platform_the_user_plays_on_adds_a_boost()
    {
        var signals = new[] { new TasteSignal(DiaryStatus.Completed, 8, ["RPG"], ["Switch"]) };
        var onSwitch = Cand("OnSwitch", ["Puzzle"], platforms: ["Switch"]);
        var onPc = Cand("OnPc", ["Puzzle"], platforms: ["PC"]);
        Assert.Equal(onSwitch.GameId, Recommender.Rank(signals, [onPc, onSwitch])[0].GameId);
    }

    [Fact]
    public void Results_are_capped_and_deterministic()
    {
        var candidates = Enumerable.Range(0, 30).Select(i => Cand($"Game {i:00}", ["RPG"])).ToList();
        var signals = new[] { Signal(DiaryStatus.Completed, 8, "RPG") };
        var first = Recommender.Rank(signals, candidates, 12);
        var second = Recommender.Rank(signals, candidates.AsEnumerable().Reverse(), 12);
        Assert.Equal(12, first.Count);
        Assert.Equal(first.Select(r => r.GameId).ToArray(), second.Select(r => r.GameId).ToArray());
    }

    [Fact]
    public void The_explanation_lists_at_most_two_liked_genres_strongest_first()
    {
        var signals = new[]
        {
            Signal(DiaryStatus.Completed, 10, "RPG"), Signal(DiaryStatus.Completed, 10, "RPG"),
            Signal(DiaryStatus.Completed, 8, "Adventure"), Signal(DiaryStatus.Completed, 7, "Indie"),
        };
        var rec = Recommender.Rank(signals, [Cand("X", ["Indie", "Adventure", "RPG"])])[0];
        Assert.Equal(["RPG", "Adventure"], rec.Because.ToArray());
    }
}
