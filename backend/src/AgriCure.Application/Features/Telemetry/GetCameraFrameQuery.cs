using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Telemetry.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Telemetry;

/// <summary>
/// Metadata for the most recent captured frame, derived from the latest detection.
/// Tenant-scoped. Frame fields are null when the caller has no detections yet.
/// </summary>
public sealed record GetCameraFrameQuery : IRequest<CameraFrameDto>;

internal sealed class GetCameraFrameQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetCameraFrameQuery, CameraFrameDto>
{
    public async Task<CameraFrameDto> Handle(
        GetCameraFrameQuery request, CancellationToken cancellationToken)
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
            .Select(d => new
            {
                d.FrameId,
                d.Timestamp,
                Depth = d.BoundingBox!.DepthMeters,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var position = await StandPositionReader.ReadAsync(db, currentUser, cancellationToken);

        return new CameraFrameDto(
            FrameId: latest?.FrameId,
            Timestamp: latest?.Timestamp,
            DepthMeters: latest?.Depth,
            Position: position);
    }
}
