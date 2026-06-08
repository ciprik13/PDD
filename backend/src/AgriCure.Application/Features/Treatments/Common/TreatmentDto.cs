using AgriCure.Domain.Detections;
using AgriCure.Domain.Treatments;

namespace AgriCure.Application.Features.Treatments.Common;

/// <summary>A recommended treatment for a disease class. Read model for the dashboard.</summary>
public sealed record TreatmentDto(
    Guid Id,
    DiseaseClass DiseaseClass,
    string Name,
    TreatmentType Type,
    int Rank,
    string Description,
    string Dosage,
    int RepeatAfterDays,
    int PhiDays,
    CostLevel CostLevel,
    IReadOnlyList<string> Tags);
