using AgriCure.Application.Common.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace AgriCure.Infrastructure.Storage;

/// <summary>
/// MinIO-backed <see cref="IStorageService"/>. The same implementation works against
/// AWS S3 once <see cref="StorageOptions.Endpoint"/> is swapped to an S3 hostname,
/// since both speak the same wire protocol.
/// </summary>
internal sealed class MinioStorageService(
    IMinioClient client,
    IOptions<StorageOptions> options,
    ILogger<MinioStorageService> logger) : IStorageService
{
    private const string DefaultContentType = "application/octet-stream";

    private readonly StorageOptions _options = options.Value;

    public async Task UploadAsync(
        StorageIdentification identification,
        Stream stream,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var size = stream.CanSeek ? stream.Length - stream.Position : -1;

        var args = new PutObjectArgs()
            .WithBucket(identification.Bucket)
            .WithObject(identification.Key)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(contentType ?? DefaultContentType);

        await client.PutObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task UploadAsync(
        StorageIdentification identification,
        byte[] content,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var stream = new MemoryStream(content, writable: false);
        await UploadAsync(identification, stream, contentType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> GetContentAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(identification.Bucket)
            .WithObject(identification.Key)
            .WithCallbackStream((src, ct) => src.CopyToAsync(buffer, ct));

        await client.GetObjectAsync(args, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }

    public async Task<string?> TryGetContentAsStringAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = await GetContentAsync(identification, cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (BucketNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to read text content for {Bucket}/{Key}",
                identification.Bucket,
                identification.Key);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(identification.Bucket)
                .WithObject(identification.Key);
            await client.StatObjectAsync(args, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(identification.Bucket)
            .WithObject(identification.Key);

        await client.RemoveObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryDeleteAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await DeleteAsync(identification, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task<string> CopyFileAsync(
        StorageIdentification source,
        StorageIdentification destination,
        CancellationToken cancellationToken = default)
    {
        var copySource = new CopySourceObjectArgs()
            .WithBucket(source.Bucket)
            .WithObject(source.Key);

        var args = new CopyObjectArgs()
            .WithBucket(destination.Bucket)
            .WithObject(destination.Key)
            .WithCopyObjectSource(copySource);

        await client.CopyObjectAsync(args, cancellationToken).ConfigureAwait(false);
        return destination.Key;
    }

    public async Task MoveFileAsync(
        StorageIdentification source,
        StorageIdentification destination,
        CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(source, destination, cancellationToken).ConfigureAwait(false);
        await DeleteAsync(source, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StorageIdentification>> GetAllKeysAsync(
        StorageIdentification prefix,
        int maxKeys = 1000,
        CancellationToken cancellationToken = default)
    {
        var args = new ListObjectsArgs()
            .WithBucket(prefix.Bucket)
            .WithPrefix(prefix.Key)
            .WithRecursive(true);

        var results = new List<StorageIdentification>();
        await foreach (var item in client.ListObjectsEnumAsync(args, cancellationToken).ConfigureAwait(false))
        {
            if (results.Count >= maxKeys)
            {
                break;
            }
            results.Add(new StorageIdentification(prefix.Bucket, item.Key));
        }
        return results;
    }

    public async Task CopyAllAsync(
        StorageIdentification source,
        StorageIdentification dest,
        CancellationToken cancellationToken = default)
    {
        var keys = await GetAllKeysAsync(source, int.MaxValue, cancellationToken).ConfigureAwait(false);
        foreach (var key in keys)
        {
            var relative = key.Key.Length > source.Key.Length
                ? key.Key[source.Key.Length..]
                : key.Key;
            var destKey = string.IsNullOrEmpty(dest.Key)
                ? relative.TrimStart('/')
                : $"{dest.Key.TrimEnd('/')}/{relative.TrimStart('/')}";

            await CopyFileAsync(
                new StorageIdentification(source.Bucket, key.Key),
                new StorageIdentification(dest.Bucket, destKey),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public string GetPublicUrl(StorageIdentification identification)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? _options.PublicBaseUrl!.TrimEnd('/')
            : $"{(_options.UseSsl ? "https" : "http")}://{_options.Endpoint}";

        var encodedKey = string.Join('/', identification.Key
            .Split('/', StringSplitOptions.None)
            .Select(Uri.EscapeDataString));

        return $"{baseUrl}/{identification.Bucket}/{encodedKey}";
    }
}
