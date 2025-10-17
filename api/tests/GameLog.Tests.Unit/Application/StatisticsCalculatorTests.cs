using GameLog.Application.Statistics;
using GameLog.Domain.Enums;

namespace GameLog.Tests.Unit.Application;

public class StatisticsCalculatorTests
{
    internal static DiaryStatRow Row(
        string title, DiaryStatus status = DiaryStatus.Completed, int minutes = 600, int? stars = null,
        string[]? genres = null, string? platform = "PC", int? year = 2020, params PlaythroughStat[] runs) =>
        new(Guid.NewGuid(), title.ToLowerInvariant(), title, null, status, minutes, platform, genres ?? [], runs, stars, year);

    [Fact]
    public void An_empty_diary_yields_zeroes_and_no_average()
    {
        var s = StatisticsCalculator.Compute([], 0, 0);
        Assert.Equal(0, s.TotalGames);
        Assert.Null(s.AverageRating);
        Assert.Equal(new int[10], s.RatingDistribution.ToArray());
        Assert.Equal(5, s.ByStatus.Count);
        Assert.Empty(s.TopGenres);
    }

    [Fact]
    public void Games_are_counted_by_status()
    {
        var s = StatisticsCalculator.Compute(
        [
            Row("A"), Row("B"), Row("C", DiaryStatus.Playing), Row("D", DiaryStatus.WantToPlay, 0),
        ], 0, 0);

        Assert.Equal(4, s.TotalGames);
        Assert.Equal(2, s.CompletedGames);
        Assert.Equal(1, s.ByStatus[DiaryStatus.Playing]);
        Assert.Equal(1, s.ByStatus[DiaryStatus.WantToPlay]);
        Assert.Equal(0, s.ByStatus[DiaryStatus.Abandoned]);
    }

    [Fact]
    public void Playtime_is_summed()
    {
        var s = StatisticsCalculator.Compute([Row("A", minutes: 90), Row("B", minutes: 30)], 0, 0);
        Assert.Equal(120, s.TotalPlaytimeMinutes);
    }

    [Fact]
    public void The_average_rating_ignores_unrated_games_and_converts_to_stars()
    {
        var s = StatisticsCalculator.Compute([Row("A", stars: 10), Row("B", stars: 7), Row("C")], 0, 0);
        Assert.Equal(4.25, s.AverageRating);
        Assert.Equal(2, s.RatingCount);
        Assert.Equal(1, s.RatingDistribution[9]);
        Assert.Equal(1, s.RatingDistribution[6]);
    }

    [Fact]
    public void Top_genres_are_ranked_by_count_then_name()
    {
        var s = StatisticsCalculator.Compute(
        [
            Row("A", genres: ["RPG", "Action"]), Row("B", genres: ["RPG"]), Row("C", genres: ["Puzzle", "Action"]),
        ], 0, 0);

        Assert.Equal(["Action", "RPG", "Puzzle"], s.TopGenres.Select(g => g.Name).ToArray());
        Assert.Equal([2, 2, 1], s.TopGenres.Select(g => g.Count).ToArray());
    }

    [Fact]
    public void Top_lists_are_capped_at_five()
    {
        var rows = Enumerable.Range(1, 8).Select(i => Row($"G{i}", minutes: i * 10, stars: i, genres: [$"Genre{i}"])).ToList();
        var s = StatisticsCalculator.Compute(rows, 0, 0);
        Assert.Equal(5, s.TopGenres.Count);
        Assert.Equal(5, s.MostPlayed.Count);
        Assert.Equal("G8", s.MostPlayed[0].Title);
        Assert.Equal("G8", s.HighestRated[0].Title);
    }

    [Fact]
    public void Games_with_no_playtime_are_not_listed_as_most_played()
    {
        var s = StatisticsCalculator.Compute([Row("A", DiaryStatus.WantToPlay, 0)], 0, 0);
        Assert.Empty(s.MostPlayed);
    }

    [Fact]
    public void Review_and_collection_counts_are_passed_through()
    {
        var s = StatisticsCalculator.Compute([], 3, 2);
        Assert.Equal(3, s.ReviewCount);
        Assert.Equal(2, s.CollectionCount);
    }
}
