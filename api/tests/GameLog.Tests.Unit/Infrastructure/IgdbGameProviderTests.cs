using System.Net;
using GameLog.Application.Catalog;
using GameLog.Infrastructure.Providers.Igdb;
using GameLog.Tests.Unit.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameLog.Tests.Unit.Infrastructure;

public class IgdbGameProviderTests
{
    private const string TokenJson = """{"access_token":"tok-1","expires_in":3600,"token_type":"bearer"}""";

    private const string WitcherJson = """
        [{"id":1942,"name":"The Witcher 3: Wild Hunt","summary":"An open-world RPG.","first_release_date":1431993600,
          "cover":{"image_id":"co1wyy"},"screenshots":[{"image_id":"sc1"}],
          "genres":[{"name":"Role-playing (RPG)"},{"name":"Adventure"}],
          "platforms":[{"name":"PC (Microsoft Windows)"},{"name":"PlayStation 4"}],
          "game_modes":[{"name":"Single player"}],
          "involved_companies":[{"company":{"name":"CD Projekt RED"},"developer":true,"publisher":false},
                                {"company":{"name":"Bandai Namco"},"developer":false,"publisher":true}],
          "websites":[{"category":13,"url":"https://store.steampowered.com/app/292030"},{"category":1,"url":"https://thewitcher.com"}]},
         {"id":7,"summary":"no name, must be dropped"}]
        """;

    private static (IgdbGameProvider Provider, FakeHandler Handler, FakeClock Clock) Create(
        Func<RecordedRequest, HttpResponseMessage>? api = null, bool configured = true, int attempts = 3)
    {
        var handler = new FakeHandler(r => r.Url.Contains("oauth2/token")
            ? FakeHandler.Json(TokenJson)
            : (api ?? (_ => FakeHandler.Json(WitcherJson)))(r));
        var clock = new FakeClock(new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new IgdbOptions
        {
            ClientId = configured ? "client" : null,
            ClientSecret = configured ? "secret" : null,
            MaxAttempts = attempts,
            RetryDelay = TimeSpan.Zero,
        });
        return (new IgdbGameProvider(new FakeHttpFactory(handler), options, clock, NullLogger<IgdbGameProvider>.Instance), handler, clock);
    }

    [Fact]
    public async Task Search_maps_igdb_fields_and_drops_unnamed_records()
    {
        var (provider, _, _) = Create();
        var games = await provider.SearchAsync("witcher", 5);

        var game = Assert.Single(games);
        Assert.Equal("igdb", game.Provider);
        Assert.Equal("1942", game.ExternalId);
        Assert.Equal("The Witcher 3: Wild Hunt", game.Title);
        Assert.Equal(new DateOnly(2015, 5, 19), game.ReleaseDate);
        Assert.Equal("CD Projekt RED", game.Developer);
        Assert.Equal("Bandai Namco", game.Publisher);
        Assert.Equal("https://thewitcher.com", game.WebsiteUrl);
        Assert.Equal("https://images.igdb.com/igdb/image/upload/t_cover_big/co1wyy.jpg", game.CoverUrl);
        // No artwork in the payload: the first screenshot is used as background.
        Assert.Equal("https://images.igdb.com/igdb/image/upload/t_1080p/sc1.jpg", game.BackgroundUrl);
        Assert.Equal(["Role-playing (RPG)", "Adventure"], game.Genres.ToArray());
        Assert.Equal(["Single player"], game.GameModes.ToArray());
    }

    [Fact]
    public async Task Requests_carry_the_client_id_and_bearer_token()
    {
        var (provider, handler, _) = Create();
        await provider.SearchAsync("witcher", 5);

        var api = handler.Requests.Single(r => r.Url.Contains("/games"));
        Assert.Equal("client", api.Headers["Client-ID"]);
        Assert.Equal("Bearer tok-1", api.Headers["Authorization"]);
        Assert.Contains("search \"witcher\"", api.Body);
    }

    [Fact]
    public async Task The_access_token_is_cached_until_it_expires()
    {
        var (provider, handler, clock) = Create();
        await provider.SearchAsync("a", 5);
        await provider.SearchAsync("b", 5);
        Assert.Equal(1, handler.Requests.Count(r => r.Url.Contains("oauth2/token")));

        clock.Advance(TimeSpan.FromHours(2));
        await provider.SearchAsync("c", 5);
        Assert.Equal(2, handler.Requests.Count(r => r.Url.Contains("oauth2/token")));
    }

    [Fact]
    public async Task Transient_errors_are_retried()
    {
        var calls = 0;
        var (provider, handler, _) = Create(_ => ++calls < 3
            ? FakeHandler.Json("{}", HttpStatusCode.ServiceUnavailable)
            : FakeHandler.Json(WitcherJson));

        Assert.Single(await provider.SearchAsync("witcher", 5));
        Assert.Equal(3, handler.Requests.Count(r => r.Url.Contains("/games")));
    }

    [Fact]
    public async Task Rate_limiting_gives_up_after_the_maximum_attempts()
    {
        var (provider, handler, _) = Create(_ => FakeHandler.Json("{}", HttpStatusCode.TooManyRequests), attempts: 2);

        await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.SearchAsync("x", 5));
        Assert.Equal(2, handler.Requests.Count(r => r.Url.Contains("/games")));
    }

    [Fact]
    public async Task Rejected_credentials_fail_fast_without_retrying()
    {
        var (provider, handler, _) = Create(_ => FakeHandler.Json("{}", HttpStatusCode.Unauthorized));

        var error = await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.SearchAsync("x", 5));
        Assert.Contains("credentials", error.Message);
        Assert.Equal(1, handler.Requests.Count(r => r.Url.Contains("/games")));
    }

    [Fact]
    public async Task Network_failures_become_provider_unavailable()
    {
        var (provider, _, _) = Create(_ => throw new HttpRequestException("connection refused"));
        await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.SearchAsync("x", 5));
    }

    [Fact]
    public async Task An_unconfigured_provider_makes_no_requests()
    {
        var (provider, handler, _) = Create(configured: false);

        var error = await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.SearchAsync("x", 5));
        Assert.Contains("not configured", error.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Malformed_json_is_reported_as_unavailable()
    {
        var (provider, _, _) = Create(_ => FakeHandler.Json("<html>oops</html>"));
        await Assert.ThrowsAsync<ProviderUnavailableException>(() => provider.SearchAsync("x", 5));
    }

    [Fact]
    public async Task Non_numeric_ids_are_rejected_before_any_request()
    {
        var (provider, handler, _) = Create();
        Assert.Null(await provider.GetAsync("1942; delete everything"));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("back\\slash", "\"back\\\\slash\"")]
    [InlineData("new\nline", "\"newline\"")]
    public void Search_terms_are_escaped_into_a_string_literal(string input, string expected) =>
        Assert.Equal(expected, IgdbGameProvider.Quote(input));

    [Fact]
    public async Task A_query_injection_attempt_stays_inside_the_string_literal()
    {
        var (provider, handler, _) = Create();
        await provider.SearchAsync("x\"; where id > 0; search \"y", 5);

        var body = handler.Requests.Single(r => r.Url.Contains("/games")).Body;
        Assert.Contains("search \"x\\\"; where id > 0; search \\\"y\"", body);
    }
}
