namespace GameLog.Infrastructure.Storage;

/// <summary>
/// Object storage settings. <c>Provider</c> is "s3" (MinIO or any S3-compatible service, the Docker
/// default) or "filesystem" (a local folder, handy without Docker). Credentials come from the environment.
/// </summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    public string Provider { get; set; } = "s3";
    public string? ServiceUrl { get; set; }
    public string Bucket { get; set; } = "gamelog";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string Path { get; set; } = "storage";
}
