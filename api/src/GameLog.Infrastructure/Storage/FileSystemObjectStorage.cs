using GameLog.Application.Abstractions;
using GameLog.Application.Common;

namespace GameLog.Infrastructure.Storage;

/// <summary>
/// Stores objects as files under one root directory. Handy for development without MinIO;
/// the S3 implementation is the production default.
/// </summary>
public sealed class FileSystemObjectStorage : IObjectStorage
{
    private readonly string root;

    public FileSystemObjectStorage(string root)
    {
        this.root = Path.GetFullPath(root);
        Directory.CreateDirectory(this.root);
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write to a temp file first so readers never see a half-written object.
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var file = File.Create(temp))
        {
            await content.CopyToAsync(file, ct);
        }

        File.Move(temp, path, overwrite: true);
    }

    public Task<StoredObject?> GetAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<StoredObject?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<StoredObject?>(new StoredObject(stream, ObjectKeys.ContentTypeFor(key)));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string PathFor(string key)
    {
        if (!ObjectKeys.IsValid(key))
        {
            throw new ArgumentException("Invalid object key.", nameof(key));
        }

        var full = Path.GetFullPath(Path.Combine(root, key));
        // Defence in depth: the resolved path must stay inside the root.
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid object key.", nameof(key));
        }

        return full;
    }
}
