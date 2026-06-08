using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Telemetry.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Telemetry;

/// <summary>
/// Latest stand position derived from the most recent detection. Tenant-scoped.
/// Returns an all-null position when the caller has no detections yet.
/// </summary>
public sealed record GetStandPositionQuery : IRequest<StandPositionDto>;

internal sealed class GetStandPositionQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetStandPositionQuery, StandPositionDto>
{
    public async Task<StandPositionDto> Handle(
        GetStandPositionQuery request, CancellationToken cancellationToken) =>
        await StandPositionReader.ReadAsync(db, currentUser, cancellationToken);
}

/// <summary>Shared derivation used by both the stand-position and camera-frame queries.</summary>
internal static class StandPositionReader
{
    public static async Task<StandPositionDto> ReadAsync(
        IApplicationDbContext db,
        ICurrentUserAccessor currentUser,
        CancellationToken cancellationToken)
    {
        var scoped = db.Detections.AsQueryable();
        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            scoped = scoped.Where(d => db.Plants
                .Any(p => p.Id == d.PlantId && p.OwnerUserId == userId));
        }

        var latest = await scoped
            .OrderByDescending(d => d.Timestamp)
            .Select(d => new { d.Row, d.PositionMeters })
            .FirstOrDefaultAsync(cancellationToken);

        var totalRows = await scoped
            .Select(d => d.Row)
            .Distinct()
            .CountAsync(cancellationToken);

        return new StandPositionDto(
            Gps: null,
            Row: latest?.Row,
            TotalRows: totalRows == 0 ? null : totalRows,
            PositionMeters: latest?.PositionMeters,
            HeightMeters: null,
            SpeedMs: null);
    }
}
