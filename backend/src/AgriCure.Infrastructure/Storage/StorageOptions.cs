namespace AgriCure.Infrastructure.Storage;

/// <summary>
/// Configuration for the MinIO / S3-compatible object storage layer.
/// Bound from the <c>Storage</c> configuration section.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Host:port the SDK should connect to (e.g. <c>localhost:9000</c>).</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Use HTTPS when talking to the storage backend.</summary>
    public bool UseSsl { get; set; }

    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Public URL base used by <c>GetPublicUrl</c>. Defaults to <c>{scheme}://{Endpoint}</c> when blank.
    /// Override when the bucket is exposed via a reverse-proxy hostname distinct from the SDK endpoint.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Bucket the picture layer uses for all reads/writes. The hosted provisioner creates it on startup.</summary>
    public string DefaultBucket { get; set; } = string.Empty;

    /// <summary>Per-upload size cap, in bytes. Enforced by upload validators / Kestrel + form limits.</summary>
    public long MaxUploadBytes { get; set; } = 25 * 1024 * 1024;

    /// <summary>Mime types the picture upload validator will accept.</summary>
    public IList<string> AllowedMimeTypes { get; set; } = new List<string>
    {
        "image/png",
        "image/jpeg",
        "image/webp",
    };
}
