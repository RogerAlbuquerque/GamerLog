using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class CommentRulesTests
{
    [Fact]
    public void The_body_is_trimmed()
    {
        var (body, error) = CommentRules.Normalize("  hello  ");
        Assert.Equal("hello", body);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n ")]
    public void Blank_comments_are_rejected(string text) => Assert.NotNull(CommentRules.Normalize(text).Error);

    [Fact]
    public void Missing_comments_are_rejected() => Assert.NotNull(CommentRules.Normalize(null).Error);

    [Fact]
    public void The_length_limit_is_enforced()
    {
        Assert.Null(CommentRules.Normalize(new string('a', CommentRules.MaxBody)).Error);
        Assert.NotNull(CommentRules.Normalize(new string('a', CommentRules.MaxBody + 1)).Error);
    }
}
