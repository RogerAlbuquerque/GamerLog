using GameLog.Domain.Rules;

namespace GameLog.Tests.Unit.Domain;

public class UserRulesTests
{
    [Theory]
    [InlineData("roger_98")]
    [InlineData("abc")]
    [InlineData("A_Valid_Name_123")]
    public void Valid_usernames_are_accepted(string name) => Assert.Null(UserRules.ValidateUsername(name));

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("emoji😀")]
    [InlineData("this_username_is_far_too_long_to_use")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    public void Invalid_or_reserved_usernames_are_rejected(string name) => Assert.NotNull(UserRules.ValidateUsername(name));

    [Fact]
    public void Username_and_email_are_normalised_for_uniqueness()
    {
        Assert.Equal("roger", UserRules.NormalizeUsername("  Roger "));
        Assert.Equal("a@b.com", UserRules.NormalizeEmail(" A@B.com "));
    }

    [Theory]
    [InlineData("short", false)]
    [InlineData("exactly-10", true)]
    [InlineData("a-much-longer-passphrase-works", true)]
    public void Password_policy_enforces_minimum_length(string password, bool ok) =>
        Assert.Equal(ok, UserRules.ValidatePassword(password) is null);

    [Fact]
    public void Password_must_not_contain_the_username() =>
        Assert.NotNull(UserRules.ValidatePassword("roger-is-my-name", "roger"));

    [Fact]
    public void Password_has_an_upper_length_bound() =>
        Assert.NotNull(UserRules.ValidatePassword(new string('x', 129)));

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData("first.last+tag@sub.example.org", true)]
    [InlineData("no-at-sign.com", false)]
    [InlineData("two@@example.com", false)]
    [InlineData("@example.com", false)]
    [InlineData("user@", false)]
    [InlineData("user@nodot", false)]
    [InlineData("user@.example.com", false)]
    [InlineData("user@example..com", false)]
    [InlineData("us er@example.com", false)]
    [InlineData("", false)]
    public void Email_validation(string email, bool ok) => Assert.Equal(ok, UserRules.ValidateEmail(email) is null);
}
