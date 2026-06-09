using AgriCure.Domain.Detections;

namespace AgriCure.Application.Features.Passports.Common;

public enum PassportEventType
{
    Created,
    Healthy,
    Symptom,
    Disease,
    Treatment,
    Resolved,
}

/// <summary>One entry in a plant's life timeline.</summary>
/// <param name="ImageUrl">Public URL of the frame captured for this detection, when one is linked. Null for non-detection events.</param>
public sealed record PassportEventDto(
    string Id,
    PassportEventType Type,
    DateTimeOffset Timestamp,
    string Title,
    string Description,
    Guid? DetectionId = null,
    double? Confidence = null,
    Severity? Severity = null,
    string? ImageUrl = null);

/// <summary>A plant's "passport": identity, current status, full event history and a severity sparkline.</summary>
public sealed record PlantPassportDto(
    string Id,
    int PlantIndex,
    int Row,
    DateTimeOffset CreatedAt,
    Severity CurrentStatus,
    IReadOnlyList<PassportEventDto> Events,
    IReadOnlyList<SeverityPointDto> SeverityHistory);

/// <summary>A single point on the passport severity sparkline. <paramref name="Value"/> is 0–100.</summary>
public sealed record SeverityPointDto(string Date, double Value);
