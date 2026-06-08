using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Dashboard.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Dashboard;

/// <summary>Dashboard headline stats, tenant-scoped to the caller's visible plants.</summary>
public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

internal sealed class GetDashboardStatsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser,
    TimeProvider time)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(
        GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var nowUtc = time.GetUtcNow();
        var todayStart = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);
        var yesterdayStart = todayStart.AddDays(-1);

        var scoped = ScopedDetections(out var plantsScoped);

        var todays = await scoped
            .Where(d => d.Timestamp >= todayStart && d.Timestamp < tomorrowStart)
            .ToListAsync(cancellationToken);

        var yesterdayCount = await scoped
            .CountAsync(d => d.Timestamp >= yesterdayStart && d.Timestamp < todayStart, cancellationToken);

        var rowsScanned = todays.Select(d => d.Row).Distinct().Count();

        var totalRows = await scoped
            .Select(d => d.Row)
            .Distinct()
            .CountAsync(cancellationToken);

        var plantsTracked = await plantsScoped.CountAsync(cancellationToken);

        var avgConfidence = todays.Count == 0
            ? 0
            : Math.Round(
                todays.Average(d =>
                    d.Predictions.OrderBy(p => p.Rank).Select(p => p.Confidence).FirstOrDefault()),
                4);

        return new DashboardStatsDto(
            DetectionsToday: todays.Count,
            DetectionsDelta: todays.Count - yesterdayCount,
            AvgConfidence: avgConfidence,
            RowsScanned: rowsScanned,
            TotalRows: totalRows,
            PlantsTracked: plantsTracked);
    }

    private IQueryable<Detection> ScopedDetections(out IQueryable<Domain.Detections.Plant> plantsScoped)
    {
        var detections = db.Detections.AsQueryable();
        var plants = db.Plants.AsQueryable();

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            plants = plants.Where(p => p.OwnerUserId == userId);
            detections = detections.Where(d => db.Plants
                .Any(p => p.Id == d.PlantId && p.OwnerUserId == userId));
        }

        plantsScoped = plants;
        return detections;
    }
}
