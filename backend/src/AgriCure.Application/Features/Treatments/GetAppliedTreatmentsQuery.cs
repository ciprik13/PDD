using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Treatments.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Treatments;

/// <summary>
/// Treatment-application history, newest first. Optionally filtered to one plant.
/// Tenant-scoped: agriculture users see applications on plants they own; admin sees all.
/// </summary>
public sealed record GetAppliedTreatmentsQuery(int Limit = 50, string? PlantId = null)
    : IRequest<IReadOnlyList<AppliedTreatmentDto>>;

internal sealed class GetAppliedTreatmentsQueryValidator : AbstractValidator<GetAppliedTreatmentsQuery>
{
    public GetAppliedTreatmentsQueryValidator()
    {
        RuleFor(q => q.Limit).GreaterThan(0);
    }
}

internal sealed class GetAppliedTreatmentsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetAppliedTreatmentsQuery, IReadOnlyList<AppliedTreatmentDto>>
{
    public const int MaxLimit = 200;

    public async Task<IReadOnlyList<AppliedTreatmentDto>> Handle(
        GetAppliedTreatmentsQuery request, CancellationToken cancellationToken)
    {
        var clamped = Math.Min(request.Limit, MaxLimit);

        var query = db.AppliedTreatments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.PlantId))
        {
            query = query.Where(a => a.PlantId == request.PlantId);
        }

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(a => db.Plants
                .Any(p => p.Id == a.PlantId && p.OwnerUserId == userId));
        }

        var applied = await query
            .OrderByDescending(a => a.AppliedAt)
            .Take(clamped)
            .ToListAsync(cancellationToken);

        if (applied.Count == 0)
        {
            return [];
        }

        var treatmentIds = applied.Select(a => a.TreatmentId).Distinct().ToArray();
        var treatments = await db.Treatments
            .Where(t => treatmentIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        // Row is a property of where the plant physically is — read it from that plant's
        // most recent detection. Plants with no detection yet report row 0.
        var plantIds = applied.Select(a => a.PlantId).Distinct().ToArray();
        var rowByPlant = await db.Detections
            .Where(d => plantIds.Contains(d.PlantId))
            .GroupBy(d => d.PlantId)
            .Select(g => new
            {
                PlantId = g.Key,
                Row = g.OrderByDescending(d => d.Timestamp).Select(d => d.Row).First(),
            })
            .ToDictionaryAsync(x => x.PlantId, x => x.Row, cancellationToken);

        return applied
            .Where(a => treatments.ContainsKey(a.TreatmentId))
            .Select(a => a.ToDto(
                treatments[a.TreatmentId],
                rowByPlant.TryGetValue(a.PlantId, out var row) ? row : 0))
            .ToArray();
    }
}
