using AgriCure.Domain.Detections;
using AgriCure.Domain.Treatments;

namespace AgriCure.Application.Features.Treatments.Common;

/// <summary>A record of a treatment applied to a plant, joined with the treatment's display fields.</summary>
public sealed record AppliedTreatmentDto(
    Guid Id,
    Guid TreatmentId,
    string TreatmentName,
    TreatmentType Type,
    DiseaseClass DiseaseClass,
    string Dosage,
    int PhiDays,
    string PlantId,
    int Row,
    DateTimeOffset AppliedAt,
    string? Notes);
