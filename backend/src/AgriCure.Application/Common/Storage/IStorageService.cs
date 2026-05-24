namespace AgriCure.Application.Common.Storage;

/// <summary>
/// Low-level object storage abstraction. Implementations target a specific backend
/// (MinIO today, AWS S3 later) — callers stay backend-agnostic.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Copies every object under <paramref name="source"/>'s key (treated as a prefix)
    /// into <paramref name="dest"/>'s bucket, preserving relative keys.
    /// </summary>
    Task CopyAllAsync(
        StorageIdentification source,
        StorageIdentification dest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every object whose key starts with <paramref name="prefix"/>'s key.
    /// </summary>
    Task<IReadOnlyList<StorageIdentification>> GetAllKeysAsync(
        StorageIdentification prefix,
        int maxKeys = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an object. Throws if the object is missing.</summary>
    Task DeleteAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an object. Returns <c>false</c> if the object is missing; never throws on a missing key.</summary>
    Task<bool> TryDeleteAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads a stream as an object.</summary>
    Task UploadAsync(
        StorageIdentification identification,
        Stream stream,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads a byte buffer as an object.</summary>
    Task UploadAsync(
        StorageIdentification identification,
        byte[] content,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads an object into a fresh <see cref="MemoryStream"/> (rewound to position 0).</summary>
    Task<Stream> GetContentAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an object as text (UTF-8). Returns <c>null</c> when the object is missing or on download error.
    /// Intended for small text payloads only; large binaries should use <see cref="GetContentAsync"/>.
    /// </summary>
    Task<string?> TryGetContentAsStringAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default);

    /// <summary>Returns whether the object exists.</summary>
    Task<bool> ExistsAsync(
        StorageIdentification identification,
        CancellationToken cancellationToken = default);

    /// <summary>Server-side copy from <paramref name="source"/> to <paramref name="destination"/>. Returns the destination's ETag.</summary>
    Task<string> CopyFileAsync(
        StorageIdentification source,
        StorageIdentification destination,
        CancellationToken cancellationToken = default);

    /// <summary>Copy + delete-source; equivalent to a rename when both ends share a bucket.</summary>
    Task MoveFileAsync(
        StorageIdentification source,
        StorageIdentification destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Composes a public URL for an object using the configured <c>PublicBaseUrl</c>. No network call.
    /// Assumes the bucket has a public-read policy applied; otherwise the URL will return 403.
    /// </summary>
    string GetPublicUrl(StorageIdentification identification);
}
