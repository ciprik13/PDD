using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Treatments;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Treatments;

/// <summary>
/// Record that a treatment was applied to a plant in the field. Available to admin and
/// agriculture users; agriculture users may only apply to plants they own.
/// </summary>
public sealed record RecordAppliedTreatmentCommand(
    Guid TreatmentId,
    string PlantId,
    DateTimeOffset AppliedAt,
    string? Notes) : IRequest<Guid>;

internal sealed class RecordAppliedTreatmentCommandValidator : AbstractValidator<RecordAppliedTreatmentCommand>
{
    public RecordAppliedTreatmentCommandValidator()
    {
        RuleFor(c => c.TreatmentId).NotEqual(Guid.Empty);
        RuleFor(c => c.PlantId).NotEmpty().MaximumLength(64);
        RuleFor(c => c.AppliedAt).NotEqual(default(DateTimeOffset));
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}

internal sealed class RecordAppliedTreatmentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<RecordAppliedTreatmentCommand, Guid>
{
    public async Task<Guid> Handle(
        RecordAppliedTreatmentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var treatmentExists = await db.Treatments
            .AnyAsync(t => t.Id == request.TreatmentId, cancellationToken);
        if (!treatmentExists)
        {
            throw new UnprocessableEntityException(
                nameof(RecordAppliedTreatmentCommand.TreatmentId),
                "No treatment exists with that id.");
        }

        var plant = await db.Plants
            .FirstOrDefaultAsync(p => p.Id == request.PlantId, cancellationToken);

        // Agriculture users may only apply to plants they own. Unknown / unowned plants
        // are reported the same way so existence isn't leaked across tenants.
        if (plant is null || (!currentUser.IsAdmin && plant.OwnerUserId != userId))
        {
            throw new UnprocessableEntityException(
                nameof(RecordAppliedTreatmentCommand.PlantId),
                "Plant not found or not owned by you.");
        }

        var applied = new AppliedTreatment
        {
            Id = Guid.NewGuid(),
            TreatmentId = request.TreatmentId,
            PlantId = request.PlantId,
            AppliedAt = request.AppliedAt,
            Notes = request.Notes,
            AppliedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.AppliedTreatments.Add(applied);
        await db.SaveChangesAsync(cancellationToken);

        return applied.Id;
    }
}
