namespace GameLog.Infrastructure.Providers.Igdb;

/// <summary>
/// IGDB access goes through Twitch client credentials. Set <c>Igdb__ClientId</c> and
/// <c>Igdb__ClientSecret</c> in the environment; without them imports report the provider as unavailable.
/// </summary>
public sealed class IgdbOptions
{
    public const string Section = "Igdb";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string TokenUrl { get; set; } = "https://id.twitch.tv/oauth2/token";
    public string ApiUrl { get; set; } = "https://api.igdb.com/v4";
    public string ImageUrl { get; set; } = "https://images.igdb.com/igdb/image/upload";
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
