using AgriCure.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Detections;

public sealed record DeleteDetectionCommand(Guid Id) : IRequest<Unit>;

internal sealed class DeleteDetectionCommandValidator : AbstractValidator<DeleteDetectionCommand>
{
    public DeleteDetectionCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class DeleteDetectionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteDetectionCommand, Unit>
{
    public async Task<Unit> Handle(DeleteDetectionCommand request, CancellationToken cancellationToken)
    {
        var detection = await db.Detections
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (detection is null)
        {
            return Unit.Value;
        }

        db.Detections.Remove(detection);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
