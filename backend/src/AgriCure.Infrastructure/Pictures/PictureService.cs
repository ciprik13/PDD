using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Common.Storage;
using AgriCure.Domain.Pictures;
using AgriCure.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgriCure.Infrastructure.Pictures;

internal sealed class PictureService(
    IStorageService storage,
    IApplicationDbContext db,
    IOptions<StorageOptions> options,
    ILogger<PictureService> logger) : IPictureService
{
    private const string PathPrefix = "pictures";

    private readonly StorageOptions _options = options.Value;

    public async Task<Picture> InsertAsync(
        byte[] binary,
        string mimeType,
        string? seoFilename = null,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binary);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        var pictureId = Guid.NewGuid();
        var virtualPath = BuildVirtualPath(pictureId, seoFilename, mimeType);

        await storage
            .UploadAsync(new StorageIdentification(_options.DefaultBucket, virtualPath), binary, mimeType, cancellationToken)
            .ConfigureAwait(false);

        var picture = new Picture
        {
            Id = pictureId,
            MimeType = mimeType,
            SeoFilename = seoFilename,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            IsNew = true,
            VirtualPath = virtualPath,
        };

        db.Pictures.Add(picture);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return picture;
    }

    public async Task<Picture> RegisterExternalAsync(
        string virtualPath,
        string mimeType,
        string? altAttribute = null,
        string? titleAttribute = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        var normalized = virtualPath.TrimStart('/');
        var exists = await storage
            .ExistsAsync(new StorageIdentification(_options.DefaultBucket, normalized), cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new NotFoundException(
                $"No object at path '{normalized}' in bucket '{_options.DefaultBucket}'.");
        }

        var picture = new Picture
        {
            Id = Guid.NewGuid(),
            MimeType = mimeType,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            IsNew = true,
            VirtualPath = normalized,
        };

        db.Pictures.Add(picture);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return picture;
    }

    public Task<Picture?> GetByIdAsync(Guid pictureId, CancellationToken cancellationToken = default) =>
        db.Pictures.FirstOrDefaultAsync(p => p.Id == pictureId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Picture>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, Picture>();
        }

        var rows = await db.Pictures
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(p => p.Id);
    }

    public async Task<string?> GetUrlAsync(Guid pictureId, CancellationToken cancellationToken = default)
    {
        var picture = await GetByIdAsync(pictureId, cancellationToken).ConfigureAwait(false);
        return picture is null ? null : GetUrl(picture);
    }

    public string GetUrl(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        return storage.GetPublicUrl(new StorageIdentification(_options.DefaultBucket, picture.VirtualPath));
    }

    public async Task<byte[]?> GetBinaryAsync(Picture picture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(picture);

        try
        {
            await using var stream = await storage
                .GetContentAsync(new StorageIdentification(_options.DefaultBucket, picture.VirtualPath), cancellationToken)
                .ConfigureAwait(false);

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            return ms.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to read binary for picture {PictureId} at {VirtualPath}",
                picture.Id,
                picture.VirtualPath);
            return null;
        }
    }

    public async Task DeleteAsync(Picture picture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await storage
            .TryDeleteAsync(new StorageIdentification(_options.DefaultBucket, picture.VirtualPath), cancellationToken)
            .ConfigureAwait(false);

        db.Pictures.Remove(picture);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Picture> UpdateMetadataAsync(
        Guid pictureId,
        string? altAttribute,
        string? titleAttribute,
        CancellationToken cancellationToken = default)
    {
        var picture = await GetByIdAsync(pictureId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"No picture with id {pictureId}.");

        picture.AltAttribute = altAttribute;
        picture.TitleAttribute = titleAttribute;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return picture;
    }

    public async Task<bool> RenameToDescriptiveKeyAsync(
        Guid pictureId,
        string seoFilename,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seoFilename);

        var picture = await GetByIdAsync(pictureId, cancellationToken).ConfigureAwait(false);
        if (picture is null)
        {
            return false;
        }

        var newPath = BuildVirtualPath(picture.Id, seoFilename, picture.MimeType);
        if (string.Equals(newPath, picture.VirtualPath, StringComparison.Ordinal))
        {
            return true;
        }

        var bucket = _options.DefaultBucket;
        var alreadyTaken = await db.Pictures
            .AnyAsync(p => p.VirtualPath == newPath && p.Id != picture.Id, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyTaken)
        {
            return false;
        }

        await storage
            .MoveFileAsync(
                new StorageIdentification(bucket, picture.VirtualPath),
                new StorageIdentification(bucket, newPath),
                cancellationToken)
            .ConfigureAwait(false);

        picture.VirtualPath = newPath;
        picture.SeoFilename = seoFilename;
        picture.IsNew = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task MarkProcessedAsync(Guid pictureId, CancellationToken cancellationToken = default)
    {
        var picture = await GetByIdAsync(pictureId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"No picture with id {pictureId}.");

        if (!picture.IsNew)
        {
            return;
        }

        picture.IsNew = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildVirtualPath(Guid pictureId, string? seoFilename, string mimeType)
    {
        var extension = ExtensionFor(mimeType);
        var slug = string.IsNullOrWhiteSpace(seoFilename)
            ? pictureId.ToString("N")
            : $"{pictureId:N}-{Slugify(seoFilename)}";
        return $"{PathPrefix}/{slug}.{extension}";
    }

    private static string ExtensionFor(string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "image/png" => "png",
        "image/jpeg" or "image/jpg" => "jpg",
        "image/webp" => "webp",
        "image/gif" => "gif",
        "image/bmp" => "bmp",
        "image/svg+xml" => "svg",
        _ => "bin",
    };

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }
        return slug.Trim('-');
    }
}
