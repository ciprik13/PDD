using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Pictures;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Pictures;

public sealed record DeletePictureCommand(Guid Id) : IRequest<Unit>;

internal sealed class DeletePictureCommandValidator : AbstractValidator<DeletePictureCommand>
{
    public DeletePictureCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class DeletePictureCommandHandler(IPictureService pictures)
    : IRequestHandler<DeletePictureCommand, Unit>
{
    public async Task<Unit> Handle(DeletePictureCommand request, CancellationToken cancellationToken)
    {
        var picture = await pictures.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No picture with id {request.Id}.");

        await pictures.DeleteAsync(picture, cancellationToken);
        return Unit.Value;
    }
}
