using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Plants.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Plants;

/// <summary>
/// All plants the caller can see, each with its latest disease label/severity.
/// Tenant-scoped: agriculture users see plants they own; admin sees every plant.
/// Ordered by most-recently-seen first, then by plant id.
/// </summary>
public sealed record GetPlantsQuery : IRequest<IReadOnlyList<PlantSummaryDto>>;

internal sealed class GetPlantsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetPlantsQuery, IReadOnlyList<PlantSummaryDto>>
{
    public async Task<IReadOnlyList<PlantSummaryDto>> Handle(
        GetPlantsQuery request, CancellationToken cancellationToken)
    {
        var plantsQuery = db.Plants.AsQueryable();

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            plantsQuery = plantsQuery.Where(p => p.OwnerUserId == userId);
        }

        var plantIds = await plantsQuery.Select(p => p.Id).ToListAsync(cancellationToken);
        if (plantIds.Count == 0)
        {
            return [];
        }

        var counts = await db.Detections
            .Where(d => plantIds.Contains(d.PlantId))
            .GroupBy(d => d.PlantId)
            .Select(g => new { PlantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlantId, x => x.Count, cancellationToken);

        // Latest detection per plant via correlated "no newer row exists" — translatable
        // and index-friendly on (Timestamp). Predictions are auto-included.
        var latest = await db.Detections
            .Where(d => plantIds.Contains(d.PlantId))
            .Where(d => !db.Detections.Any(d2 =>
                d2.PlantId == d.PlantId && d2.Timestamp > d.Timestamp))
            .ToListAsync(cancellationToken);

        var latestByPlant = latest
            .GroupBy(d => d.PlantId)
            .ToDictionary(g => g.Key, g => g.First());

        return plantIds
            .Select(id =>
            {
                latestByPlant.TryGetValue(id, out var d);
                var top = d?.Predictions.OrderBy(p => p.Rank).FirstOrDefault();
                return new PlantSummaryDto(
                    PlantId: id,
                    Row: d?.Row,
                    LatestSeverity: d?.Severity,
                    LatestLabel: top?.Label,
                    LatestDiseaseClass: top?.DiseaseClass,
                    LastSeenAt: d?.Timestamp,
                    DetectionCount: counts.TryGetValue(id, out var c) ? c : 0);
            })
            .OrderByDescending(p => p.LastSeenAt ?? DateTimeOffset.MinValue)
            .ThenBy(p => p.PlantId)
            .ToArray();
    }
}
