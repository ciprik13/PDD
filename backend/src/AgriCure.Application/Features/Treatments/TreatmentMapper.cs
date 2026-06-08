using AgriCure.Application.Features.Treatments.Common;
using AgriCure.Domain.Treatments;

namespace AgriCure.Application.Features.Treatments;

internal static class TreatmentMapper
{
    public static TreatmentDto ToDto(this Treatment t) =>
        new(
            Id: t.Id,
            DiseaseClass: t.DiseaseClass,
            Name: t.Name,
            Type: t.Type,
            Rank: t.Rank,
            Description: t.Description,
            Dosage: t.Dosage,
            RepeatAfterDays: t.RepeatAfterDays,
            PhiDays: t.PhiDays,
            CostLevel: t.CostLevel,
            Tags: BuildTags(t));

    public static AppliedTreatmentDto ToDto(this AppliedTreatment a, Treatment treatment, int row) =>
        new(
            Id: a.Id,
            TreatmentId: a.TreatmentId,
            TreatmentName: treatment.Name,
            Type: treatment.Type,
            DiseaseClass: treatment.DiseaseClass,
            Dosage: treatment.Dosage,
            PhiDays: treatment.PhiDays,
            PlantId: a.PlantId,
            Row: row,
            AppliedAt: a.AppliedAt,
            Notes: a.Notes);

    private static string[] BuildTags(Treatment t)
    {
        var typeTag = t.Type == TreatmentType.Biological ? "Biological" : "Chemical";
        var phiTag = $"PHI: {t.PhiDays} days";
        var costTag = t.CostLevel switch
        {
            CostLevel.Low => "Low cost",
            CostLevel.Medium => "Moderate cost",
            CostLevel.High => "High cost",
            _ => "Unknown cost",
        };
        return [typeTag, phiTag, costTag];
    }
}
