using AgriCure.Domain.Detections;

namespace AgriCure.Domain.Treatments;

/// <summary>
/// A recommended treatment for a given <see cref="DiseaseClass"/>. Reference data —
/// seeded from agronomist input, read-only at runtime. One row = one product
/// recommendation for one disease; a product may appear for several diseases as
/// separate rows. Ordering within a disease is by <see cref="Rank"/> (1 = first choice,
/// biological-first policy).
/// </summary>
public sealed class Treatment
{
    public Guid Id { get; set; }

    /// <summary>The disease this recommendation targets.</summary>
    public DiseaseClass DiseaseClass { get; set; }

    /// <summary>Product name, e.g. "Trichoderma harzianum".</summary>
    public string Name { get; set; } = string.Empty;

    public TreatmentType Type { get; set; }

    /// <summary>1 = first recommended choice for this disease. Lower ranks shown first.</summary>
    public int Rank { get; set; }

    /// <summary>Human-readable application guidance.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Dosage instruction, e.g. "2g/L foliar spray".</summary>
    public string Dosage { get; set; } = string.Empty;

    /// <summary>Days to wait before re-applying.</summary>
    public int RepeatAfterDays { get; set; }

    /// <summary>Pre-harvest interval in days (0 for most biologicals).</summary>
    public int PhiDays { get; set; }

    public CostLevel CostLevel { get; set; }
}
