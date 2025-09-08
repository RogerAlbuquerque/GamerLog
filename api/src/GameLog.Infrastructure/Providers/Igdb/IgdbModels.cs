namespace GameLog.Infrastructure.Providers.Igdb;

// Wire format of the IGDB v4 API (snake_case JSON). Only the fields we use are modelled.
public sealed record IgdbImage(string? ImageId);

public sealed record IgdbNamed(string? Name);

public sealed record IgdbCompanyRef(IgdbNamed? Company, bool Developer, bool Publisher);

public sealed record IgdbWebsite(int? Category, string? Url);

public sealed record IgdbGame(
    long Id,
    string? Name,
    string? Summary,
    long? FirstReleaseDate,
    IgdbImage? Cover,
    List<IgdbImage>? Artworks,
    List<IgdbImage>? Screenshots,
    List<IgdbNamed>? Genres,
    List<IgdbNamed>? Platforms,
    List<IgdbNamed>? GameModes,
    List<IgdbCompanyRef>? InvolvedCompanies,
    List<IgdbWebsite>? Websites);

public sealed record TwitchToken(string AccessToken, int ExpiresIn);
