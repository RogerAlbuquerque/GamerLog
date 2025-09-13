using GameLog.Application.Common;
using GameLog.Infrastructure.Storage;

namespace GameLog.Tests.Unit.Infrastructure;

public class FileSystemObjectStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "gamelog-tests-" + Guid.NewGuid().ToString("N"));
    private readonly FileSystemObjectStorage storage;

    public FileSystemObjectStorageTests() => storage = new FileSystemObjectStorage(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static MemoryStream Bytes(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Objects_round_trip_with_a_content_type_derived_from_the_key()
    {
        await storage.PutAsync("avatars/abc.webp", Bytes("hello"), "image/webp");

        var stored = await storage.GetAsync("avatars/abc.webp");
        Assert.NotNull(stored);
        await using var content = stored!.Content;
        Assert.Equal("hello", await new StreamReader(content).ReadToEndAsync());
        Assert.Equal("image/webp", stored.ContentType);
    }

    [Fact]
    public async Task Missing_objects_return_null() => Assert.Null(await storage.GetAsync("avatars/none.webp"));

    [Fact]
    public async Task Putting_an_existing_key_replaces_it()
    {
        await storage.PutAsync("covers/x.png", Bytes("one"), "image/png");
        await storage.PutAsync("covers/x.png", Bytes("two"), "image/png");

        await using var content = (await storage.GetAsync("covers/x.png"))!.Content;
        Assert.Equal("two", await new StreamReader(content).ReadToEndAsync());
    }

    [Fact]
    public async Task Delete_removes_the_object_and_is_idempotent()
    {
        await storage.PutAsync("covers/y.png", Bytes("data"), "image/png");
        await storage.DeleteAsync("covers/y.png");
        await storage.DeleteAsync("covers/y.png");

        Assert.Null(await storage.GetAsync("covers/y.png"));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("avatars/../../secret.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("avatars\\..\\x.png")]
    [InlineData("")]
    [InlineData("noslash.png")]
    [InlineData("avatars/.hidden")]
    public async Task Path_traversal_and_malformed_keys_are_rejected(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => storage.PutAsync(key, Bytes("x"), "image/png"));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetAsync(key));
    }

    [Fact]
    public void Generated_keys_are_valid_and_unique()
    {
        var a = ObjectKeys.New("avatars", "webp");
        var b = ObjectKeys.New("avatars", "webp");
        Assert.True(ObjectKeys.IsValid(a));
        Assert.NotEqual(a, b);
    }
}
