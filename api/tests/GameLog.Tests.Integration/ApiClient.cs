using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameLog.Tests.Integration;

/// <summary>A browser-like client: keeps cookies and sends the CSRF header the way the web app does.</summary>
public sealed class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public HttpClient Http { get; }

    private string? csrf;

    public ApiClient(ApiFactory factory) => Http = factory.CreateClient();

    private async Task<string> CsrfAsync()
    {
        if (csrf is null)
        {
            using var r = await Http.GetAsync("/api/v1/auth/csrf");
            csrf = (await r.Content.ReadFromJsonAsync<Csrf>(Json))!.Token;
        }

        return csrf;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, bool withCsrf = true)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            req.Content = JsonContent.Create(body, options: Json);
        }

        if (withCsrf && method != HttpMethod.Get)
        {
            req.Headers.Add("X-CSRF-TOKEN", await CsrfAsync());
        }

        var response = await Http.SendAsync(req);
        return response;
    }

    public Task<HttpResponseMessage> Get(string url) => SendAsync(HttpMethod.Get, url);

    public Task<HttpResponseMessage> Post(string url, object? body = null) => SendAsync(HttpMethod.Post, url, body);

    public Task<HttpResponseMessage> Put(string url, object? body = null) => SendAsync(HttpMethod.Put, url, body);

    public Task<HttpResponseMessage> Delete(string url) => SendAsync(HttpMethod.Delete, url);

    public async Task<T> Read<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    /// <summary>Registers a new user; the session cookie and a user-bound CSRF token are kept for later calls.</summary>
    public async Task<ApiClient> RegisterAsync(string username)
    {
        using var r = await Post("/api/v1/auth/register", new { username, email = $"{username}@example.test", password = "Correct-Horse-9", displayName = username });
        r.EnsureSuccessStatusCode();
        csrf = (await r.Content.ReadFromJsonAsync<AuthBody>(Json))!.CsrfToken;
        return this;
    }

    private sealed record Csrf(string Token);

    private sealed record AuthBody(string CsrfToken);
}
