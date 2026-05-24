using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Pictures;

/// <summary>Backend-proxied upload command. The controller has already extracted the bytes from the form.</summary>
public sealed record UploadPictureCommand(
    byte[] Content,
    string MimeType,
    string? SeoFilename,
    string? AltAttribute,
    string? TitleAttribute) : IRequest<PictureDto>;

internal sealed class UploadPictureCommandValidator : AbstractValidator<UploadPictureCommand>
{
    public UploadPictureCommandValidator()
    {
        RuleFor(c => c.Content)
            .NotNull()
            .Must(b => b.Length > 0).WithMessage("Uploaded file is empty.");

        RuleFor(c => c.MimeType)
            .NotEmpty()
            .MaximumLength(127);

        RuleFor(c => c.SeoFilename)
            .MaximumLength(200);

        RuleFor(c => c.AltAttribute)
            .MaximumLength(300);

        RuleFor(c => c.TitleAttribute)
            .MaximumLength(300);
    }
}

internal sealed class UploadPictureCommandHandler(IPictureService pictures)
    : IRequestHandler<UploadPictureCommand, PictureDto>
{
    public async Task<PictureDto> Handle(UploadPictureCommand request, CancellationToken cancellationToken)
    {
        var picture = await pictures.InsertAsync(
            request.Content,
            request.MimeType,
            request.SeoFilename,
            request.AltAttribute,
            request.TitleAttribute,
            cancellationToken);

        return picture.ToDto(pictures);
    }
}
