using GameLog.Application.Common;

namespace GameLog.Tests.Unit.Application;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Baldur's Gate 3", "baldurs-gate-3")]
    [InlineData("The Legend of Zelda: Breath of the Wild", "the-legend-of-zelda-breath-of-the-wild")]
    [InlineData("  Hades   II  ", "hades-ii")]
    [InlineData("Pokémon Légendes", "pokemon-legendes")]
    [InlineData("NieR:Automata™", "nier-automata")]
    [InlineData("!!!", "game")]
    [InlineData("", "game")]
    [InlineData(null, "game")]
    public void Slugs_are_url_safe(string? title, string expected) => Assert.Equal(expected, SlugGenerator.From(title));

    [Fact]
    public void Long_titles_are_truncated_without_trailing_dash()
    {
        var slug = SlugGenerator.From(string.Join(' ', Enumerable.Repeat("word", 40)));
        Assert.True(slug.Length <= SlugGenerator.MaxLength);
        Assert.False(slug.EndsWith('-'));
    }
}
