namespace AgriCure.Application.Features.Telemetry.Common;

public sealed record GpsCoordinatesDto(double Lat, double Lon);

/// <summary>
/// Where the scanning stand is. Only <see cref="Row"/>, <see cref="PositionMeters"/> and
/// <see cref="TotalRows"/> are backed by stored detection data; GPS, height and speed are
/// null here (live GPS is proxied separately from the device at <c>/api/camera/gps</c>).
/// </summary>
public sealed record StandPositionDto(
    GpsCoordinatesDto? Gps,
    int? Row,
    int? TotalRows,
    double? PositionMeters,
    double? HeightMeters,
    double? SpeedMs);

/// <summary>
/// Metadata about the most recent captured frame, derived from the latest detection.
/// Resolution/fps/recording state come from the live device, not stored data, so they
/// are omitted here.
/// </summary>
public sealed record CameraFrameDto(
    long? FrameId,
    DateTimeOffset? Timestamp,
    double? DepthMeters,
    StandPositionDto Position);
