using AgriCure.Domain.Pictures;

namespace AgriCure.Application.Common.Pictures;

/// <summary>
/// Domain-aware picture management on top of <see cref="Storage.IStorageService"/>.
/// Callers think in terms of <see cref="Picture"/> rows + <c>VirtualPath</c>; the bucket
/// is implicit (read from <c>StorageOptions.DefaultBucket</c>) and never crosses this interface.
/// </summary>
public interface IPictureService
{
    /// <summary>Uploads bytes directly to storage and creates the Picture row.</summary>
    Task<Picture> InsertAsync(
        byte[] binary,
        string mimeType,
        string? seoFilename = null,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an object that has already been uploaded directly to the configured bucket
    /// by an external sync server. Validates that the object exists before persisting.
    /// </summary>
    /// <exception cref="Common.Exceptions.NotFoundException">The object does not exist at <paramref name="virtualPath"/>.</exception>
    Task<Picture> RegisterExternalAsync(
        string virtualPath,
        string mimeType,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default);

    Task<Picture?> GetByIdAsync(Guid pictureId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Picture>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<string?> GetUrlAsync(Guid pictureId, CancellationToken cancellationToken = default);

    /// <summary>Sync convenience overload when the entity is already loaded.</summary>
    string GetUrl(Picture picture);

    /// <summary>Downloads the underlying object bytes. Returns <c>null</c> when the object is missing.</summary>
    Task<byte[]?> GetBinaryAsync(Picture picture, CancellationToken cancellationToken = default);

    /// <summary>Deletes the Picture row and the underlying storage object. Idempotent.</summary>
    Task DeleteAsync(Picture picture, CancellationToken cancellationToken = default);

    Task<Picture> UpdateMetadataAsync(
        Guid pictureId,
        string? altAttribute,
        string? titleAttribute,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the storage object to a slug-based key (<c>pictures/{guid}-{seoFilename}.{ext}</c>),
    /// updates <c>VirtualPath</c>, <c>SeoFilename</c>, and flips <c>IsNew</c> to <c>false</c>.
    /// Returns <c>false</c> when the picture is missing or the destination path is already in use.
    /// </summary>
    Task<bool> RenameToDescriptiveKeyAsync(
        Guid pictureId,
        string seoFilename,
        CancellationToken cancellationToken = default);

    /// <summary>Flips <c>IsNew</c> to <c>false</c>. Called once the picture has been served / processed.</summary>
    Task MarkProcessedAsync(Guid pictureId, CancellationToken cancellationToken = default);
}
