using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Pictures;

/// <summary>
/// Registers an object the external sync server has already uploaded directly to MinIO.
/// The caller supplies the path the external server returned.
/// </summary>
public sealed record RegisterPictureCommand(
    string VirtualPath,
    string MimeType,
    string? AltAttribute,
    string? TitleAttribute) : IRequest<PictureDto>;

internal sealed class RegisterPictureCommandValidator : AbstractValidator<RegisterPictureCommand>
{
    public RegisterPictureCommandValidator()
    {
        RuleFor(c => c.VirtualPath)
            .NotEmpty()
            .MaximumLength(500)
            .Must(p => !p.StartsWith('/'))
                .WithMessage("VirtualPath must not start with '/'.");

        RuleFor(c => c.MimeType)
            .NotEmpty()
            .MaximumLength(127);

        RuleFor(c => c.AltAttribute)
            .MaximumLength(300);

        RuleFor(c => c.TitleAttribute)
            .MaximumLength(300);
    }
}

internal sealed class RegisterPictureCommandHandler(IPictureService pictures)
    : IRequestHandler<RegisterPictureCommand, PictureDto>
{
    public async Task<PictureDto> Handle(RegisterPictureCommand request, CancellationToken cancellationToken)
    {
        var picture = await pictures.RegisterExternalAsync(
            request.VirtualPath,
            request.MimeType,
            request.AltAttribute,
            request.TitleAttribute,
            cancellationToken);

        return picture.ToDto(pictures);
    }
}
