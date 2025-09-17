using System.Net;
using GameLog.Application.Abstractions;
using GameLog.Infrastructure.Storage;
using GameLog.Tests.Unit.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Infrastructure;

public class HttpArtworkStoreTests
{
    private const string Good = "https://images.igdb.com/igdb/image/upload/t_cover_big/co1.jpg";

    private static (HttpArtworkStore Store, FakeHandler Handler, MemoryStorage Storage) Create(
        HttpStatusCode status = HttpStatusCode.OK, bool validImage = true)
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("binary", status));
        var storage = new MemoryStorage();
        var store = new HttpArtworkStore(new HttpClient(handler), new FakeImageProcessor(validImage), storage, NullLogger<HttpArtworkStore>.Instance);
        return (store, handler, storage);
    }

    [Theory]
    [InlineData(Good, true)]
    [InlineData("https://IMAGES.IGDB.COM/x.jpg", true)]
    [InlineData("http://images.igdb.com/x.jpg", false)]
    [InlineData("https://images.igdb.com.evil.example/x.jpg", false)]
    [InlineData("https://evil.example/images.igdb.com/x.jpg", false)]
    [InlineData("https://user:pw@images.igdb.com/x.jpg", false)]
    [InlineData("https://images.igdb.com:8443/x.jpg", false)]
    [InlineData("https://169.254.169.254/latest/meta-data", false)]
    [InlineData("https://localhost/x.jpg", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    public void Only_https_urls_on_the_allow_list_are_accepted(string url, bool allowed) =>
        Assert.Equal(allowed, HttpArtworkStore.IsAllowed(url));

    [Fact]
    public async Task A_disallowed_url_is_never_requested()
    {
        var (store, handler, storage) = Create();
        Assert.Null(await store.StoreAsync("https://169.254.169.254/latest/meta-data", ImageKind.Cover));
        Assert.Empty(handler.Requests);
        Assert.Empty(storage.Objects);
    }

    [Fact]
    public async Task A_valid_image_is_stored_under_a_generated_key()
    {
        var (store, _, storage) = Create();
        var key = await store.StoreAsync(Good, ImageKind.Cover);

        Assert.NotNull(key);
        Assert.StartsWith("games/", key);
        Assert.Equal("image/webp", storage.Objects[key!].ContentType);
    }

    [Fact]
    public async Task Download_errors_and_invalid_images_yield_null_instead_of_throwing()
    {
        var (notFound, _, storage1) = Create(HttpStatusCode.NotFound);
        Assert.Null(await notFound.StoreAsync(Good, ImageKind.Cover));
        Assert.Empty(storage1.Objects);

        var (bad, _, storage2) = Create(validImage: false);
        Assert.Null(await bad.StoreAsync(Good, ImageKind.Cover));
        Assert.Empty(storage2.Objects);
    }

    [Fact]
    public async Task Empty_urls_are_ignored()
    {
        var (store, handler, _) = Create();
        Assert.Null(await store.StoreAsync(null, ImageKind.Cover));
        Assert.Null(await store.StoreAsync("  ", ImageKind.Cover));
        Assert.Empty(handler.Requests);
    }
}
