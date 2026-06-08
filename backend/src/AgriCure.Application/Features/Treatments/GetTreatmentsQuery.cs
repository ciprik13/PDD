using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Treatments.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Treatments;

/// <summary>
/// Recommended treatments for a disease class, ordered by rank (biological-first).
/// Reference data — visible to any authenticated user, not tenant-scoped.
/// </summary>
public sealed record GetTreatmentsQuery(DiseaseClass DiseaseClass) : IRequest<IReadOnlyList<TreatmentDto>>;

internal sealed class GetTreatmentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTreatmentsQuery, IReadOnlyList<TreatmentDto>>
{
    public async Task<IReadOnlyList<TreatmentDto>> Handle(
        GetTreatmentsQuery request, CancellationToken cancellationToken)
    {
        var treatments = await db.Treatments
            .Where(t => t.DiseaseClass == request.DiseaseClass)
            .OrderBy(t => t.Rank)
            .ToListAsync(cancellationToken);

        return treatments.Select(t => t.ToDto()).ToArray();
    }
}
