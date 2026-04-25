using AgriCure.Domain.Detections;

namespace AgriCure.Application.Common.DTOs;

public sealed record DetectionDto(
    Guid Id,
    long FrameId,
    DateTimeOffset Timestamp,
    Severity Severity,
    ClassPredictionDto TopPrediction,
    IReadOnlyList<ClassPredictionDto> AllPredictions,
    BoundingBoxDto BoundingBox,
    int InferenceMs,
    bool ConfidenceGatePassed,
    int Row,
    string PlantId,
    double PositionMeters);

public sealed record ClassPredictionDto(
    DiseaseClass DiseaseClass,
    double Confidence,
    string Label);

public sealed record BoundingBoxDto(
    double X,
    double Y,
    double Width,
    double Height,
    double DepthMeters,
    double AffectedAreaPercent);
