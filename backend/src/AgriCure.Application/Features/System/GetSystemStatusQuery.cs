using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.System.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.System;

/// <summary>
/// Edge-device status derived from detection recency. If the most recent ingest is
/// within <see cref="FreshnessWindow"/>, the device is treated as online (camera + GPS
/// + model active). Tenant-scoped to the caller's plants.
/// </summary>
public sealed record GetSystemStatusQuery : IRequest<SystemStatusDto>;

internal sealed class GetSystemStatusQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser,
    TimeProvider time)
    : IRequestHandler<GetSystemStatusQuery, SystemStatusDto>
{
    public static readonly TimeSpan FreshnessWindow = TimeSpan.FromMinutes(5);

    public async Task<SystemStatusDto> Handle(
        GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        var nowUtc = time.GetUtcNow();
        var todayStart = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);

        var scoped = db.Detections.AsQueryable();
        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            scoped = scoped.Where(d => db.Plants
                .Any(p => p.Id == d.PlantId && p.OwnerUserId == userId));
        }

        var lastSeenAt = await scoped
            .OrderByDescending(d => d.Timestamp)
            .Select(d => (DateTimeOffset?)d.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingAlerts = await scoped
            .CountAsync(
                d => d.Timestamp >= todayStart && d.Severity != Severity.Healthy,
                cancellationToken);

        var isOnline = lastSeenAt is not null &&
                       nowUtc - lastSeenAt.Value <= FreshnessWindow;

        return new SystemStatusDto(
            DeviceStatus: isOnline ? DeviceStatus.Online : DeviceStatus.Offline,
            CameraConnected: isOnline,
            GpsActive: isOnline,
            ModelLoaded: isOnline,
            ModelName: null,
            SyncedAt: lastSeenAt,
            PendingAlerts: pendingAlerts);
    }
}
