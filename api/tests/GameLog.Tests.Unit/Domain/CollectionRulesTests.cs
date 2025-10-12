using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class CollectionRulesTests
{
    [Fact]
    public void A_named_collection_is_valid() => Assert.Empty(CollectionRules.Validate("Best of 2025", "My favourites"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_name_is_required(string name) => Assert.Contains(CollectionRules.Validate(name, null), v => v.Field == "name");

    [Fact]
    public void Name_and_description_are_length_limited()
    {
        var errors = CollectionRules.Validate(new string('x', CollectionRules.MaxName + 1), new string('y', CollectionRules.MaxDescription + 1));
        Assert.Equal(["description", "name"], errors.Select(e => e.Field).Order().ToArray());
    }

    [Fact]
    public void A_reorder_must_be_an_exact_permutation()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        Assert.True(CollectionRules.IsPermutation([a, b, c], [c, a, b]));
        Assert.False(CollectionRules.IsPermutation([a, b, c], [a, b]));
        Assert.False(CollectionRules.IsPermutation([a, b, c], [a, a, b]));
        Assert.False(CollectionRules.IsPermutation([a, b, c], [a, b, Guid.NewGuid()]));
    }
}
