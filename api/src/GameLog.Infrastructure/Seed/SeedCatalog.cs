using System.Text.Json;
using GameLog.Application.Catalog;

namespace GameLog.Infrastructure.Seed;

/// <summary>A small built-in catalog so the app is fully usable offline and without IGDB credentials.</summary>
public static class SeedCatalog
{
    public const string Provider = "seed";
    private const string ResourceName = "GameLog.Seed.games.json";

    private sealed record Entry(
        string Id, string Title, string? Summary, DateOnly? ReleaseDate, string? Developer, string? Publisher,
        List<string> Genres, List<string> Platforms, List<string> GameModes);

    public static IReadOnlyList<ExternalGame> Load()
    {
        using var stream = typeof(SeedCatalog).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                      ?? throw new InvalidOperationException("The seed catalog is empty.");

        return entries
            .Select(e => new ExternalGame(
                Provider, e.Id, e.Title, e.Summary, e.ReleaseDate, e.Developer, e.Publisher,
                CoverUrl: null, BackgroundUrl: null, WebsiteUrl: null, e.GameModes, e.Genres, e.Platforms))
            .ToList();
    }
}
