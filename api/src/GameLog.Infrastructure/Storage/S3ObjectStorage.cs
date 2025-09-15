using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using GameLog.Application.Abstractions;
using GameLog.Application.Common;
using Microsoft.Extensions.Options;

namespace GameLog.Infrastructure.Storage;

/// <summary>S3-compatible storage (AWS S3, MinIO, ...). The bucket is private; objects are only served through the API.</summary>
public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly AmazonS3Client client;
    private readonly string bucket;
    private readonly SemaphoreSlim bucketLock = new(1, 1);
    private bool bucketReady;

    public S3ObjectStorage(IOptions<StorageOptions> options)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AccessKey) || string.IsNullOrWhiteSpace(o.SecretKey))
        {
            throw new InvalidOperationException("Storage:AccessKey and Storage:SecretKey must be set for the s3 provider.");
        }

        bucket = o.Bucket;
        client = new AmazonS3Client(o.AccessKey, o.SecretKey, new AmazonS3Config
        {
            ServiceURL = o.ServiceUrl,
            ForcePathStyle = true, // MinIO and most self-hosted services need path-style addressing
        });
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        Guard(key);
        await EnsureBucketAsync(ct);

        // The SDK needs a seekable stream to know the content length.
        var seekable = content;
        if (!content.CanSeek)
        {
            seekable = new MemoryStream();
            await content.CopyToAsync(seekable, ct);
            seekable.Position = 0;
        }

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = seekable,
            ContentType = contentType,
            AutoCloseStream = false,
        }, ct);
    }

    public async Task<StoredObject?> GetAsync(string key, CancellationToken ct = default)
    {
        Guard(key);
        try
        {
            var response = await client.GetObjectAsync(bucket, key, ct);
            var type = string.IsNullOrEmpty(response.Headers.ContentType) ? ObjectKeys.ContentTypeFor(key) : response.Headers.ContentType;
            return new StoredObject(response.ResponseStream, type);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        Guard(key);
        await client.DeleteObjectAsync(bucket, key, ct); // S3 deletes are idempotent
    }

    public void Dispose() => client.Dispose();

    private static void Guard(string key)
    {
        if (!ObjectKeys.IsValid(key))
        {
            throw new ArgumentException("Invalid object key.", nameof(key));
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (bucketReady)
        {
            return;
        }

        await bucketLock.WaitAsync(ct);
        try
        {
            if (!bucketReady)
            {
                if (!await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket))
                {
                    await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, ct);
                }

                bucketReady = true;
            }
        }
        finally
        {
            bucketLock.Release();
        }
    }
}
