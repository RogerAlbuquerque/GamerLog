namespace GameLog.Application.Common;

public static class MediaUrls
{
    public const string Prefix = "/api/v1/media/";

    /// <summary>Turns a storage key into the public path served by the media endpoint.</summary>
    public static string? For(string? key) => string.IsNullOrEmpty(key) ? null : Prefix + key;
}
