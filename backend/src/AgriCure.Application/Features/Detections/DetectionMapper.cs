using AgriCure.Application.Common.DTOs;
using AgriCure.Domain.Detections;

namespace AgriCure.Application.Features.Detections;

internal static class DetectionMapper
{
    public static DetectionDto ToDto(this Detection detection)
    {
        var ordered = detection.Predictions.OrderBy(p => p.Rank).ToArray();
        var allDtos = ordered.Select(ToDto).ToArray();
        var top = allDtos.Length > 0
            ? allDtos[0]
            : throw new InvalidOperationException(
                $"Detection {detection.Id} has no predictions; mapping requires at least one.");

        return new DetectionDto(
            Id: detection.Id,
            FrameId: detection.FrameId,
            Timestamp: detection.Timestamp,
            Severity: detection.Severity,
            TopPrediction: top,
            AllPredictions: allDtos,
            BoundingBox: detection.BoundingBox.ToDto(),
            InferenceMs: detection.InferenceMs,
            ConfidenceGatePassed: detection.ConfidenceGatePassed,
            Row: detection.Row,
            PlantId: detection.PlantId,
            PositionMeters: detection.PositionMeters);
    }

    public static ClassPredictionDto ToDto(this ClassPrediction p) =>
        new(p.DiseaseClass, p.Confidence, p.Label);

    public static BoundingBoxDto ToDto(this BoundingBox b) =>
        new(b.X, b.Y, b.Width, b.Height, b.DepthMeters, b.AffectedAreaPercent);
}
