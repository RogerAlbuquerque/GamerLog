using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class ReviewRulesTests
{
    private static IReadOnlyList<RuleViolation> Check(string? title = "Great", string? body = "A really good game.", int? minutes = null) =>
        ReviewRules.Validate(title, body, null, null, null, null, null, minutes);

    [Fact]
    public void A_minimal_review_is_valid() => Assert.Empty(Check());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_title_is_required(string title) => Assert.Contains(Check(title: title), v => v.Field == "title");

    [Fact]
    public void A_missing_title_is_rejected() => Assert.Contains(Check(title: null), v => v.Field == "title");

    [Fact]
    public void A_too_long_title_is_rejected() =>
        Assert.Contains(Check(title: new string('x', ReviewRules.MaxTitle + 1)), v => v.Field == "title");

    [Theory]
    [InlineData("short")]
    [InlineData("          ")]
    public void A_too_short_body_is_rejected(string body) => Assert.Contains(Check(body: body), v => v.Field == "body");

    [Fact]
    public void A_too_long_body_is_rejected() =>
        Assert.Contains(Check(body: new string('x', ReviewRules.MaxBody + 1)), v => v.Field == "body");

    [Fact]
    public void Optional_fields_are_length_limited()
    {
        var errors = ReviewRules.Validate("t", "long enough body", new string('x', 3001), new string('x', 1001), null, null, new string('x', 501), null);
        Assert.Equal(["experience", "favoriteMoment", "pros"], errors.Select(e => e.Field).Order().ToArray());
    }

    [Fact]
    public void Negative_playtime_is_rejected() => Assert.Contains(Check(minutes: -1), v => v.Field == "playtimeMinutes");

    [Fact]
    public void Clean_turns_blank_into_null()
    {
        Assert.Null(ReviewRules.Clean("  "));
        Assert.Equal("x", ReviewRules.Clean(" x "));
    }
}
