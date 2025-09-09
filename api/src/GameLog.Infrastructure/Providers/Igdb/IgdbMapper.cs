using GameLog.Application.Catalog;

namespace GameLog.Infrastructure.Providers.Igdb;

public static class IgdbMapper
{
    public const string ProviderName = "igdb";
    private const int OfficialWebsiteCategory = 1;

    public static ExternalGame? ToExternal(IgdbGame g, string imageBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(g.Name))
        {
            return null; // unusable record
        }

        var companies = g.InvolvedCompanies ?? [];
        var background = g.Artworks?.FirstOrDefault(a => !string.IsNullOrEmpty(a.ImageId))
                         ?? g.Screenshots?.FirstOrDefault(a => !string.IsNullOrEmpty(a.ImageId));

        return new ExternalGame(
            ProviderName,
            g.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Truncate(g.Name.Trim(), 200)!,
            Truncate(g.Summary?.Trim(), 4000),
            g.FirstReleaseDate is { } unix ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime) : null,
            Truncate(companies.FirstOrDefault(c => c.Developer)?.Company?.Name, 200),
            Truncate(companies.FirstOrDefault(c => c.Publisher)?.Company?.Name, 200),
            ImageUrl(imageBaseUrl, "t_cover_big", g.Cover?.ImageId),
            ImageUrl(imageBaseUrl, "t_1080p", background?.ImageId),
            Truncate(g.Websites?.FirstOrDefault(w => w.Category == OfficialWebsiteCategory)?.Url, 500),
            Names(g.GameModes, 60),
            Names(g.Genres, 60),
            Names(g.Platforms, 60));
    }

    private static string? ImageUrl(string baseUrl, string size, string? imageId) =>
        string.IsNullOrWhiteSpace(imageId) ? null : $"{baseUrl.TrimEnd('/')}/{size}/{imageId}.jpg";

    private static IReadOnlyList<string> Names(List<IgdbNamed>? items, int max) =>
        (items ?? [])
            .Select(i => Truncate(i.Name?.Trim(), max))
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
