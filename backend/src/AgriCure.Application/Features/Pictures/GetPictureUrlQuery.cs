using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Pictures;

public sealed record GetPictureUrlQuery(Guid Id) : IRequest<PictureUrlDto>;

internal sealed class GetPictureUrlQueryValidator : AbstractValidator<GetPictureUrlQuery>
{
    public GetPictureUrlQueryValidator()
    {
        RuleFor(q => q.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class GetPictureUrlQueryHandler(IPictureService pictures)
    : IRequestHandler<GetPictureUrlQuery, PictureUrlDto>
{
    public async Task<PictureUrlDto> Handle(GetPictureUrlQuery request, CancellationToken cancellationToken)
    {
        var url = await pictures.GetUrlAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No picture with id {request.Id}.");

        return new PictureUrlDto(url);
    }
}
