namespace AgriCure.Application.Features.System.Common;

public enum DeviceStatus
{
    Online,
    Offline,
    Error,
}

/// <summary>
/// Edge-device health, inferred from how recently detections have been ingested.
/// <paramref name="ModelName"/> is null when no model name has been configured.
/// </summary>
public sealed record SystemStatusDto(
    DeviceStatus DeviceStatus,
    bool CameraConnected,
    bool GpsActive,
    bool ModelLoaded,
    string? ModelName,
    DateTimeOffset? SyncedAt,
    int PendingAlerts);
