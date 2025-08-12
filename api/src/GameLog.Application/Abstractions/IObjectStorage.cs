namespace GameLog.Application.Abstractions;

public sealed record StoredObject(Stream Content, string ContentType);

/// <summary>S3-compatible object storage. Keys are server-generated; clients never choose them.</summary>
public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    Task<StoredObject?> GetAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}

public enum ImageKind
{
    Avatar,
    Cover,
    Background,
}

public sealed record ProcessedImage(byte[] Data, string ContentType, string Extension);

/// <summary>Validates, decodes, resizes and re-encodes untrusted uploads (strips metadata and polyglot payloads).</summary>
public interface IImageProcessor
{
    /// <exception cref="Common.ValidationFailedException">The file is not an acceptable image.</exception>
    Task<ProcessedImage> ProcessAsync(Stream input, ImageKind kind, CancellationToken ct = default);
}
