using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Domain.Pictures;

namespace AgriCure.Api.IntegrationTests;

/// <summary>
/// In-process stub for integration tests. Skips the MinIO storage upload and writes
/// the Picture row directly to the DB so FK constraints are satisfied.
/// </summary>
internal sealed class FakePictureService(IApplicationDbContext db) : IPictureService
{
    public async Task<Picture> InsertAsync(
        byte[] binary,
        string mimeType,
        string? seoFilename = null,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default)
    {
        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            MimeType = mimeType,
            SeoFilename = seoFilename,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            IsNew = true,
            VirtualPath = $"pictures/fake-{Guid.NewGuid():N}.bin",
        };

        db.Pictures.Add(picture);
        await db.SaveChangesAsync(cancellationToken);
        return picture;
    }

    public Task<Picture> RegisterExternalAsync(
        string virtualPath,
        string mimeType,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used in integration tests.");

    public Task<Picture?> GetByIdAsync(Guid pictureId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Picture?>(null);

    public Task<IReadOnlyDictionary<Guid, Picture>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Picture>>(new Dictionary<Guid, Picture>());

    public Task<string?> GetUrlAsync(Guid pictureId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    // Mirror the real service's "base + path" composition so callers get a usable URL.
    public string GetUrl(Picture picture) => $"https://fake-storage.local/{picture.VirtualPath}";

    public Task<byte[]?> GetBinaryAsync(Picture picture, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);

    public Task DeleteAsync(Picture picture, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<Picture> UpdateMetadataAsync(
        Guid pictureId,
        string? altAttribute,
        string? titleAttribute,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used in integration tests.");

    public Task<bool> RenameToDescriptiveKeyAsync(
        Guid pictureId,
        string seoFilename,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task MarkProcessedAsync(Guid pictureId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
