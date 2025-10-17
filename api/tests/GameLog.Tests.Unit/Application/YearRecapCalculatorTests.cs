using GameLog.Application.Statistics;
using GameLog.Domain.Enums;

namespace GameLog.Tests.Unit.Application;

public class YearRecapCalculatorTests
{
    private static PlaythroughStat Run(int y, int m, int d, int minutes = 600) => new(new DateOnly(y, m, d), minutes);

    private static DiaryStatRow Game(string title, int? stars, string[] genres, string? platform, params PlaythroughStat[] runs) =>
        new(Guid.NewGuid(), title.ToLowerInvariant(), title, null, DiaryStatus.Completed, runs.Sum(r => r.PlaytimeMinutes), platform, genres, runs, stars, 2020);

    [Fact]
    public void Only_completions_inside_the_year_count()
    {
        var recap = YearRecapCalculator.Compute(2025,
        [
            Game("In", 8, [], "PC", Run(2025, 3, 1)),
            Game("Before", 8, [], "PC", Run(2024, 12, 31)),
            Game("After", 8, [], "PC", Run(2026, 1, 1)),
        ], 0, 0);

        Assert.Equal(1, recap.GamesCompleted);
        Assert.Equal("In", recap.FirstCompleted!.Title);
    }

    [Fact]
    public void Year_boundaries_are_inclusive()
    {
        var recap = YearRecapCalculator.Compute(2025,
        [
            Game("NewYearsDay", null, [], null, Run(2025, 1, 1)),
            Game("NewYearsEve", null, [], null, Run(2025, 12, 31)),
        ], 0, 0);

        Assert.Equal(2, recap.GamesCompleted);
        Assert.Equal(1, recap.CompletedByMonth[0]);
        Assert.Equal(1, recap.CompletedByMonth[11]);
    }

    [Fact]
    public void A_replay_in_the_same_year_is_two_completions_of_one_game()
    {
        var recap = YearRecapCalculator.Compute(2025, [Game("Hades", 10, [], "PC", Run(2025, 2, 1, 300), Run(2025, 8, 1, 200))], 0, 0);
        Assert.Equal(1, recap.GamesCompleted);
        Assert.Equal(2, recap.Completions);
        Assert.Equal(500, recap.TotalPlaytimeMinutes);
    }

    [Fact]
    public void A_replay_in_another_year_counts_in_both_years()
    {
        var rows = new[] { Game("Hades", 10, [], "PC", Run(2024, 5, 1), Run(2025, 5, 1)) };
        Assert.Equal(1, YearRecapCalculator.Compute(2024, rows, 0, 0).GamesCompleted);
        Assert.Equal(1, YearRecapCalculator.Compute(2025, rows, 0, 0).GamesCompleted);
    }

    [Fact]
    public void The_busiest_month_is_the_one_with_most_completions_and_earliest_on_a_tie()
    {
        var recap = YearRecapCalculator.Compute(2025,
        [
            Game("A", null, [], null, Run(2025, 4, 1)),
            Game("B", null, [], null, Run(2025, 4, 9)),
            Game("C", null, [], null, Run(2025, 7, 1)),
            Game("D", null, [], null, Run(2025, 7, 2)),
        ], 0, 0);
        Assert.Equal(4, recap.BusiestMonth);
        Assert.Equal(2, recap.CompletedByMonth[3]);
    }

    [Fact]
    public void A_year_without_completions_is_an_empty_recap()
    {
        var recap = YearRecapCalculator.Compute(2025, [Game("Old", 8, ["RPG"], "PC", Run(2020, 1, 1))], 4, 6);
        Assert.Equal(0, recap.GamesCompleted);
        Assert.Null(recap.BusiestMonth);
        Assert.Null(recap.AverageRating);
        Assert.Null(recap.LongestGame);
        Assert.Null(recap.FirstCompleted);
        Assert.Empty(recap.HighestRated);
        Assert.Equal(4, recap.ReviewsWritten);
        Assert.Equal(6, recap.RatingsGiven);
        Assert.Equal([2020], recap.AvailableYears.ToArray());
    }

    [Fact]
    public void Longest_shortest_and_highest_rated_are_picked_correctly()
    {
        var recap = YearRecapCalculator.Compute(2025,
        [
            Game("Marathon", 6, ["RPG"], "PC", Run(2025, 1, 10, 6000)),
            Game("Sprint", 10, ["Platformer"], "Switch", Run(2025, 2, 10, 90)),
            Game("Middle", 8, ["RPG"], "PC", Run(2025, 3, 10, 900)),
            Game("Fourth", 4, ["RPG"], "PC", Run(2025, 4, 10, 1200)),
        ], 0, 0);

        Assert.Equal("Marathon", recap.LongestGame!.Title);
        Assert.Equal("Sprint", recap.ShortestGame!.Title);
        Assert.Equal(["Sprint", "Middle", "Marathon"], recap.HighestRated.Select(g => g.Title).ToArray());
        Assert.Equal("RPG", recap.TopGenres[0].Name);
        Assert.Equal("PC", recap.TopPlatforms[0].Name);
        Assert.Equal(7.0 / 2 + 0 + (6 + 10 + 8 + 4) / 4.0 / 2 - 7.0 / 2, recap.AverageRating);
    }

    [Fact]
    public void Available_years_are_listed_newest_first()
    {
        var recap = YearRecapCalculator.Compute(2025,
        [
            Game("A", null, [], null, Run(2023, 1, 1)), Game("B", null, [], null, Run(2025, 1, 1)), Game("C", null, [], null, Run(2023, 6, 1)),
        ], 0, 0);
        Assert.Equal([2025, 2023], recap.AvailableYears.ToArray());
    }
}
