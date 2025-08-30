using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class RatingRulesTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(0, false)]
    [InlineData(11, false)]
    [InlineData(-3, false)]
    public void HalfStars_are_valid_only_between_1_and_10(int value, bool expected) =>
        Assert.Equal(expected, RatingRules.IsValid(value));

    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(3.5, 7)]
    [InlineData(5.0, 10)]
    public void Stars_convert_to_half_stars(double stars, int expected) =>
        Assert.Equal(expected, RatingRules.FromStars(stars));

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(5.5)]
    [InlineData(4.25)]
    public void Invalid_star_values_are_rejected(double stars) => Assert.Null(RatingRules.FromStars(stars));

    [Fact]
    public void Half_stars_convert_back_to_stars() => Assert.Equal(4.5, RatingRules.ToStars(9));
}
