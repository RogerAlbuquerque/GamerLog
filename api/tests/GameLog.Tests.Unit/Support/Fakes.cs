using GameLog.Application.Abstractions;
using GameLog.Application.Catalog;

namespace GameLog.Tests.Unit.Support;

public sealed class FakeProvider(params ExternalGame[] games) : IGameMetadataProvider
{
    public string Name => "fake";

    public Task<IReadOnlyList<ExternalGame>> SearchAsync(string query, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ExternalGame>>(games.Where(g => g.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<ExternalGame?> GetAsync(string externalId, CancellationToken ct = default) =>
        Task.FromResult(games.FirstOrDefault(g => g.ExternalId == externalId));
}

public sealed class FakeArtwork(bool succeed = true) : IGameArtworkStore
{
    public int Calls { get; private set; }

    public Task<string?> StoreAsync(string? url, ImageKind kind, CancellationToken ct = default)
    {
        if (url is null)
        {
            return Task.FromResult<string?>(null);
        }

        Calls++;
        return Task.FromResult(succeed ? $"games/{kind}-{Calls}.webp" : null);
    }
}

public static class Ext
{
    public static ExternalGame Game(
        string id, string title, string[]? genres = null, string[]? platforms = null, string? cover = "https://img/c.jpg") =>
        new("fake", id, title, "Summary", new DateOnly(2020, 1, 1), "Dev", "Pub", cover, "https://img/b.jpg", null,
            ["Single player"], genres ?? ["Action"], platforms ?? ["PC"]);
}
