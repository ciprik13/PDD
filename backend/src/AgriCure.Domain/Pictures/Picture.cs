namespace AgriCure.Domain.Pictures;

/// <summary>
/// A stored binary asset (typically an image). The owning bucket is configured globally on
/// <c>StorageOptions.DefaultBucket</c> — only the path within that bucket is persisted here.
/// Relationships to other entities live in per-feature mapping tables (e.g. <c>DetectionPicture</c>),
/// not on this row, so <c>Picture</c> stays category-agnostic.
/// </summary>
public sealed class Picture
{
    public Guid Id { get; set; }

    /// <summary>Mime type (e.g. <c>image/png</c>). Drives the storage <c>Content-Type</c> header.</summary>
    public string MimeType { get; set; } = default!;

    /// <summary>Human-readable slug embedded in the object key (without folder or extension).</summary>
    public string? SeoFilename { get; set; }

    /// <summary>Value for the HTML <c>img</c> alt attribute.</summary>
    public string? AltAttribute { get; set; }

    /// <summary>Value for the HTML <c>img</c> title attribute.</summary>
    public string? TitleAttribute { get; set; }

    /// <summary>True until the picture has been served / processed at least once.</summary>
    public bool IsNew { get; set; }

    /// <summary>Object key relative to <c>StorageOptions.DefaultBucket</c>. Unique.</summary>
    public string VirtualPath { get; set; } = default!;
}
