namespace AgriCure.Domain.Detections;

public sealed record BoundingBox(
    double X,
    double Y,
    double Width,
    double Height,
    double DepthMeters,
    double AffectedAreaPercent);
