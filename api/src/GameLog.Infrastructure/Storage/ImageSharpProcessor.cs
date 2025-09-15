using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace GameLog.Infrastructure.Storage;

/// <summary>
/// Never stores uploaded bytes as they came in: the image is identified, bounded, decoded, resized
/// and re-encoded as WebP without metadata. That removes EXIF (GPS!), polyglot payloads and
/// oversized "decompression bomb" images in one pass.
/// </summary>
public sealed class ImageSharpProcessor : IImageProcessor
{
    public const int MaxInputBytes = 5 * 1024 * 1024;
    public const int MaxDimension = 8000;
    public const long MaxPixels = 40_000_000;

    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase) { "JPEG", "PNG", "WEBP" };

    public async Task<ProcessedImage> ProcessAsync(Stream input, ImageKind kind, CancellationToken ct = default)
    {
        using var buffer = await ReadBoundedAsync(input, ct);

        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw Invalid("The file is not a supported image. Use JPEG, PNG or WebP.");
        }

        var format = info.Metadata.DecodedImageFormat?.Name;
        if (format is null || !AllowedFormats.Contains(format))
        {
            throw Invalid("Unsupported image format. Use JPEG, PNG or WebP.");
        }

        if (info.Width < 1 || info.Height < 1 || info.Width > MaxDimension || info.Height > MaxDimension
            || (long)info.Width * info.Height > MaxPixels)
        {
            throw Invalid($"The image is too large. The maximum is {MaxDimension}x{MaxDimension} pixels.");
        }

        buffer.Position = 0;
        Image image;
        try
        {
            image = await Image.LoadAsync(buffer, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw Invalid("The image file is corrupt.");
        }

        using (image)
        {
            var (width, height, mode) = kind switch
            {
                ImageKind.Avatar => (256, 256, ResizeMode.Crop),
                ImageKind.Cover => (600, 900, ResizeMode.Max),
                _ => (1920, 1080, ResizeMode.Max),
            };

            image.Mutate(c => c.Resize(new ResizeOptions { Size = new Size(width, height), Mode = mode }));

            using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 82, SkipMetadata = true }, ct);
            return new ProcessedImage(output.ToArray(), "image/webp", "webp");
        }
    }

    private static async Task<MemoryStream> ReadBoundedAsync(Stream input, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await input.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxInputBytes)
            {
                buffer.Dispose();
                throw Invalid($"The file is too large. The maximum is {MaxInputBytes / (1024 * 1024)} MB.");
            }

            buffer.Write(chunk, 0, read);
        }

        if (buffer.Length == 0)
        {
            buffer.Dispose();
            throw Invalid("The file is empty.");
        }

        buffer.Position = 0;
        return buffer;
    }

    private static ValidationFailedException Invalid(string message) => new("file", message);
}
