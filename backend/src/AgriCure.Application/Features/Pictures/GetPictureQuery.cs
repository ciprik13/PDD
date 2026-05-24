using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Pictures;

public sealed record GetPictureQuery(Guid Id) : IRequest<PictureDto>;

internal sealed class GetPictureQueryValidator : AbstractValidator<GetPictureQuery>
{
    public GetPictureQueryValidator()
    {
        RuleFor(q => q.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class GetPictureQueryHandler(IPictureService pictures)
    : IRequestHandler<GetPictureQuery, PictureDto>
{
    public async Task<PictureDto> Handle(GetPictureQuery request, CancellationToken cancellationToken)
    {
        var picture = await pictures.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No picture with id {request.Id}.");

        return picture.ToDto(pictures);
    }
}
