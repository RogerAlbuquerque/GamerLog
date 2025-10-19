using System.Net;

namespace GameLog.Tests.Integration;

public class AuthFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public AuthFlowTests(ApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Health_endpoint_is_public()
    {
        var r = await new ApiClient(factory).Get("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Registering_signs_the_user_in()
    {
        var client = await new ApiClient(factory).RegisterAsync("flow_alice");
        var me = await client.Get("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Anonymous_requests_to_protected_endpoints_get_401()
    {
        var client = new ApiClient(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Get("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Get("/api/v1/feed")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Get("/api/v1/notifications")).StatusCode);
    }

    [Fact]
    public async Task Public_catalog_endpoints_work_without_signing_in()
    {
        var client = new ApiClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.Get("/api/v1/games")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.Get("/api/v1/discover")).StatusCode);
    }

    [Fact]
    public async Task Unsafe_requests_without_a_csrf_token_are_rejected()
    {
        var client = new ApiClient(factory);
        var r = await client.SendAsync(HttpMethod.Post, "/api/v1/auth/register",
            new { username = "csrf_victim", email = "csrf@example.test", password = "Correct-Horse-9" }, withCsrf: false);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task A_wrong_password_is_401_and_does_not_reveal_whether_the_user_exists()
    {
        await new ApiClient(factory).RegisterAsync("flow_bob");
        var client = new ApiClient(factory);
        var wrongPassword = await client.Post("/api/v1/auth/login", new { login = "flow_bob", password = "nope-nope-nope" });
        var unknownUser = await client.Post("/api/v1/auth/login", new { login = "nobody_here", password = "nope-nope-nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync().ContinueWith(t => Strip(t.Result)),
            await unknownUser.Content.ReadAsStringAsync().ContinueWith(t => Strip(t.Result)));
    }

    private static string Strip(string body) => System.Text.RegularExpressions.Regex.Replace(body, "\"(traceId|correlationId)\":\"[^\"]*\"", "");

    [Fact]
    public async Task Duplicate_usernames_are_a_conflict()
    {
        await new ApiClient(factory).RegisterAsync("flow_dup");
        var r = await new ApiClient(factory).Post("/api/v1/auth/register",
            new { username = "FLOW_DUP", email = "other@example.test", password = "Correct-Horse-9" });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Logging_out_ends_the_session()
    {
        var client = await new ApiClient(factory).RegisterAsync("flow_out");
        (await client.Post("/api/v1/auth/logout")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.Get("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var r = await new ApiClient(factory).Get("/health");
        Assert.True(r.Headers.Contains("X-Content-Type-Options"));
        Assert.True(r.Headers.Contains("X-Frame-Options") || r.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_origin()
    {
        var client = new ApiClient(factory);
        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/v1/games");
        allowed.Headers.Add("Origin", "https://app.example.test");
        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/v1/games");
        denied.Headers.Add("Origin", "https://evil.example.test");

        Assert.True((await client.Http.SendAsync(allowed)).Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False((await client.Http.SendAsync(denied)).Headers.Contains("Access-Control-Allow-Origin"));
    }
}
