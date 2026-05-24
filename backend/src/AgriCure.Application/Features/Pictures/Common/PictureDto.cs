namespace AgriCure.Application.Features.Pictures.Common;

/// <summary>API surface representation of a stored picture.</summary>
/// <param name="Id">Picture identifier.</param>
/// <param name="MimeType">Stored mime type (e.g. <c>image/png</c>).</param>
/// <param name="SeoFilename">SEO-friendly slug embedded in the storage key; <c>null</c> for fresh uploads with no name.</param>
/// <param name="AltAttribute">Value for the HTML <c>img</c> alt attribute.</param>
/// <param name="TitleAttribute">Value for the HTML <c>img</c> title attribute.</param>
/// <param name="IsNew">True until the picture has been served / processed at least once.</param>
/// <param name="VirtualPath">Object key inside the configured storage bucket.</param>
/// <param name="Url">Publicly fetchable URL (assumes the bucket has a public-read policy).</param>
public sealed record PictureDto(
    Guid Id,
    string MimeType,
    string? SeoFilename,
    string? AltAttribute,
    string? TitleAttribute,
    bool IsNew,
    string VirtualPath,
    string Url);
