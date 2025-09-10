using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameLog.Application.Catalog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameLog.Infrastructure.Providers.Igdb;

/// <summary>
/// IGDB implementation of <see cref="IGameMetadataProvider"/>. Registered as a singleton so the
/// Twitch access token is cached across requests and refreshed shortly before it expires.
/// </summary>
public sealed class IgdbGameProvider(
    IHttpClientFactory httpFactory,
    IOptions<IgdbOptions> options,
    TimeProvider clock,
    ILogger<IgdbGameProvider> logger) : IGameMetadataProvider
{
    public const string HttpClientName = "igdb";

    private const string Fields =
        "id,name,summary,first_release_date,cover.image_id,artworks.image_id,screenshots.image_id," +
        "genres.name,platforms.name,game_modes.name,involved_companies.developer,involved_companies.publisher," +
        "involved_companies.company.name,websites.category,websites.url";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string? token;
    private DateTimeOffset tokenExpiresAt;

    private IgdbOptions Options => options.Value;

    public string Name => IgdbMapper.ProviderName;

    public async Task<IReadOnlyList<ExternalGame>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var body = $"fields {Fields}; search {Quote(query)}; where version_parent = null; limit {Math.Clamp(limit, 1, 25)};";
        return await QueryAsync(body, ct);
    }

    public async Task<ExternalGame?> GetAsync(string externalId, CancellationToken ct = default)
    {
        if (!long.TryParse(externalId, out var id) || id <= 0)
        {
            return null;
        }

        var body = $"fields {Fields}; where id = {id}; limit 1;";
        return (await QueryAsync(body, ct)).FirstOrDefault();
    }

    /// <summary>Apicalypse string literal: quotes and backslashes are escaped so user input cannot alter the query.</summary>
    public static string Quote(string value)
    {
        var cleaned = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return "\"" + cleaned.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private async Task<IReadOnlyList<ExternalGame>> QueryAsync(string body, CancellationToken ct)
    {
        EnsureConfigured();

        var response = await SendWithRetryAsync(async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Options.ApiUrl.TrimEnd('/')}/games")
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            request.Headers.Add("Client-ID", Options.ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
            return request;
        }, ct);

        List<IgdbGame>? games;
        try
        {
            games = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(Json, ct);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "IGDB returned a body that is not valid JSON");
            throw new ProviderUnavailableException("The game database returned an unexpected response.");
        }

        return (games ?? [])
            .Select(g => IgdbMapper.ToExternal(g, Options.ImageUrl))
            .Where(g => g is not null)
            .Select(g => g!)
            .ToList();
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (token is not null && clock.GetUtcNow() < tokenExpiresAt)
        {
            return token;
        }

        await tokenLock.WaitAsync(ct);
        try
        {
            if (token is not null && clock.GetUtcNow() < tokenExpiresAt)
            {
                return token;
            }

            var url = $"{Options.TokenUrl}?client_id={Uri.EscapeDataString(Options.ClientId!)}" +
                      $"&client_secret={Uri.EscapeDataString(Options.ClientSecret!)}&grant_type=client_credentials";
            var response = await SendWithRetryAsync(() => Task.FromResult(new HttpRequestMessage(HttpMethod.Post, url)), ct);
            var parsed = await response.Content.ReadFromJsonAsync<TwitchToken>(Json, ct);
            if (parsed is null || string.IsNullOrEmpty(parsed.AccessToken))
            {
                throw new ProviderUnavailableException("The game database did not issue an access token.");
            }

            token = parsed.AccessToken;
            // Refresh a minute early so a token never expires mid-request.
            tokenExpiresAt = clock.GetUtcNow().AddSeconds(Math.Max(parsed.ExpiresIn - 60, 30));
            return token;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    /// <summary>Retries transient failures (429, 5xx, network errors) with a growing delay; auth errors fail fast.</summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpRequestMessage>> create, CancellationToken ct)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        var attempts = Math.Max(Options.MaxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = await create();
                var response = await client.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                var transient = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                logger.LogWarning("IGDB request failed with {Status} (attempt {Attempt}/{Attempts})", (int)response.StatusCode, attempt, attempts);
                response.Dispose();

                if (!transient || attempt == attempts)
                {
                    if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    {
                        token = null; // force a fresh token on the next call
                        throw new ProviderUnavailableException("The game database rejected our credentials.");
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new ProviderUnavailableException("The game database rejected the request. Check the IGDB client id and secret.");
                    }

                    throw new ProviderUnavailableException("The game database is temporarily unavailable.");
                }
            }
            catch (HttpRequestException ex) when (attempt < attempts)
            {
                logger.LogWarning(ex, "IGDB request failed (attempt {Attempt}/{Attempts})", attempt, attempts);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "IGDB is unreachable");
                throw new ProviderUnavailableException("The game database is unreachable.");
            }

            await Task.Delay(Options.RetryDelay * attempt, ct);
        }
    }

    private void EnsureConfigured()
    {
        if (!Options.IsConfigured)
        {
            throw new ProviderUnavailableException("IGDB is not configured. Set Igdb__ClientId and Igdb__ClientSecret.");
        }
    }
}
