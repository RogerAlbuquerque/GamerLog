using System.Net;
using System.Text.Json;

namespace GameLog.Tests.Integration;

public class SocialFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public SocialFlowTests(ApiFactory factory) => this.factory = factory;

    private async Task<(Guid Id, string Slug)> FirstGame(ApiClient client)
    {
        using var doc = JsonDocument.Parse(await (await client.Get("/api/v1/games")).Content.ReadAsStringAsync());
        var g = doc.RootElement.GetProperty("items")[0];
        return (g.GetProperty("id").GetGuid(), g.GetProperty("slug").GetString()!);
    }

    private static object Review(string title = "Superb", bool spoilers = false, int? halfStars = 9) => new
    {
        title, body = "Tight controls and a great soundtrack.", containsSpoilers = spoilers, halfStars,
    };

    [Fact]
    public async Task Reviewing_rating_commenting_and_liking_work_end_to_end()
    {
        var alice = await new ApiClient(factory).RegisterAsync("it_alice");
        var bob = await new ApiClient(factory).RegisterAsync("it_bob");
        var (gameId, _) = await FirstGame(alice);

        var saved = await alice.Put($"/api/v1/games/{gameId}/review", Review());
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var reviewId = JsonDocument.Parse(await saved.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await bob.Put($"/api/v1/reviews/{reviewId}/like")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await bob.Post($"/api/v1/reviews/{reviewId}/comments", new { body = "Agreed!", isSpoiler = false })).StatusCode);

        var summary = JsonDocument.Parse(await (await bob.Get($"/api/v1/games/{gameId}/rating-summary")).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, summary.GetProperty("count").GetInt32());

        var notifications = JsonDocument.Parse(await (await alice.Get("/api/v1/notifications")).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, notifications.GetProperty("total").GetInt32());   // the like and the comment
    }

    [Fact]
    public async Task Users_cannot_delete_other_users_reviews_but_the_author_can()
    {
        var alice = await new ApiClient(factory).RegisterAsync("it_owner");
        var mallory = await new ApiClient(factory).RegisterAsync("it_mallory");
        var (gameId, _) = await FirstGame(alice);
        var id = JsonDocument.Parse(await (await alice.Put($"/api/v1/games/{gameId}/review", Review())).Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await mallory.Delete($"/api/v1/reviews/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.Delete($"/api/v1/reviews/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.Get($"/api/v1/reviews/{id}")).StatusCode);
    }

    [Fact]
    public async Task Writes_require_authentication()
    {
        var anon = new ApiClient(factory);
        var (gameId, _) = await FirstGame(anon);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.Put($"/api/v1/games/{gameId}/review", Review())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.Put($"/api/v1/games/{gameId}/rating", new { halfStars = 5 })).StatusCode);
    }

    [Fact]
    public async Task Invalid_input_is_a_400_problem_document()
    {
        var alice = await new ApiClient(factory).RegisterAsync("it_invalid");
        var (gameId, _) = await FirstGame(alice);
        var r = await alice.Put($"/api/v1/games/{gameId}/rating", new { halfStars = 99 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Private_collections_are_hidden_from_other_users()
    {
        var owner = await new ApiClient(factory).RegisterAsync("it_curator");
        var other = await new ApiClient(factory).RegisterAsync("it_visitor");
        var created = await owner.Post("/api/v1/collections", new { name = "Secret list", description = (string?)null, visibility = "Private" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("collection").GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await owner.Get($"/api/v1/collections/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Get($"/api/v1/collections/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await new ApiClient(factory).Get($"/api/v1/collections/{id}")).StatusCode);
    }

    [Fact]
    public async Task Following_shows_up_in_the_feed()
    {
        var alice = await new ApiClient(factory).RegisterAsync("it_follower");
        var bob = await new ApiClient(factory).RegisterAsync("it_followed");
        var (gameId, _) = await FirstGame(bob);

        Assert.Equal(HttpStatusCode.OK, (await alice.Put("/api/v1/users/it_followed/follow")).StatusCode);
        (await bob.Put($"/api/v1/games/{gameId}/review", Review())).EnsureSuccessStatusCode();

        var feed = JsonDocument.Parse(await (await alice.Get("/api/v1/feed")).Content.ReadAsStringAsync()).RootElement;
        Assert.Contains(feed.GetProperty("items").EnumerateArray(), i => i.GetProperty("type").GetString() == "ReviewPublished");
    }

    [Fact]
    public async Task Statistics_and_recap_are_public()
    {
        await new ApiClient(factory).RegisterAsync("it_stats");
        var anon = new ApiClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await anon.Get("/api/v1/users/it_stats/stats")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.Get("/api/v1/users/it_stats/recap?year=2025")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anon.Get("/api/v1/users/nobody_at_all/stats")).StatusCode);
    }
}
