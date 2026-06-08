using AgriCure.Domain.Detections;

namespace AgriCure.Application.Features.Plants.Common;

/// <summary>
/// One plant in the field with its current disease status, derived from its most recent
/// detection. Fields are null when the plant has no detections yet.
/// </summary>
public sealed record PlantSummaryDto(
    string PlantId,
    int? Row,
    Severity? LatestSeverity,
    string? LatestLabel,
    DiseaseClass? LatestDiseaseClass,
    DateTimeOffset? LastSeenAt,
    int DetectionCount);
