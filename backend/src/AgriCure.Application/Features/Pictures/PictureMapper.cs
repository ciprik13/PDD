using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using AgriCure.Domain.Pictures;

namespace AgriCure.Application.Features.Pictures;

internal static class PictureMapper
{
    public static PictureDto ToDto(this Picture picture, IPictureService pictures) =>
        new(
            picture.Id,
            picture.MimeType,
            picture.SeoFilename,
            picture.AltAttribute,
            picture.TitleAttribute,
            picture.IsNew,
            picture.VirtualPath,
            pictures.GetUrl(picture));
}
