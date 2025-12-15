using GameLog.Application.Common;

namespace GameLog.Tests.Unit.Application;

public class ObjectKeysTests
{
    [Theory]
    [InlineData("avatars/3f2a9c.webp")]
    [InlineData("covers/abc123.png")]
    [InlineData("games/covers/a-b_c.1.jpg")]
    public void Well_formed_keys_are_accepted(string key) => Assert.True(ObjectKeys.IsValid(key));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("avatar.webp")]            // no folder
    [InlineData("/avatars/a.webp")]        // absolute
    [InlineData("avatars/../secret.webp")] // traversal
    [InlineData("avatars/a..b.webp")]      // double dot anywhere
    [InlineData("avatars\\a.webp")]        // backslash
    [InlineData("Avatars/A.webp")]         // upper case is never generated
    [InlineData("avatars/a b.webp")]       // whitespace
    [InlineData("avatars/.hidden")]        // segment starting with a dot
    public void Malformed_or_dangerous_keys_are_rejected(string? key) => Assert.False(ObjectKeys.IsValid(key));

    [Fact]
    public void Overlong_keys_are_rejected() => Assert.False(ObjectKeys.IsValid("a/" + new string('b', 250)));

    [Fact]
    public void Generated_keys_are_valid_and_unique()
    {
        var first = ObjectKeys.New("avatars", "webp");
        var second = ObjectKeys.New("avatars", "webp");
        Assert.True(ObjectKeys.IsValid(first));
        Assert.StartsWith("avatars/", first);
        Assert.EndsWith(".webp", first);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("a/b.webp", "image/webp")]
    [InlineData("a/b.JPG", "image/jpeg")]
    [InlineData("a/b.jpeg", "image/jpeg")]
    [InlineData("a/b.png", "image/png")]
    [InlineData("a/b.bin", "application/octet-stream")]
    public void Content_type_follows_the_extension(string key, string expected) => Assert.Equal(expected, ObjectKeys.ContentTypeFor(key));
}

public class MediaUrlsTests
{
    [Fact]
    public void Keys_become_media_paths() => Assert.Equal("/api/v1/media/avatars/a.webp", MediaUrls.For("avatars/a.webp"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_keys_have_no_url(string? key) => Assert.Null(MediaUrls.For(key));
}
