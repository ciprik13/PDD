using AgriCure.Domain.Detections;
using AgriCure.Domain.Treatments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgriCure.Infrastructure.Persistence;

/// <summary>
/// Seeds the read-only treatment recommendation catalog (agronomist reference data).
/// Idempotent: runs once when the table is empty and never overwrites operator edits.
/// This is the only seeded business data in the platform — everything else is real
/// detection data pushed by the edge devices.
/// </summary>
public static class TreatmentCatalogSeeder
{
    public static async Task SeedTreatmentCatalogAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        if (await db.Treatments.AnyAsync())
        {
            return;
        }

        db.Treatments.AddRange(Catalog);
        await db.SaveChangesAsync();

        sp.GetRequiredService<ILogger<AppDbContext>>()
            .LogInformation("Seeded {Count} treatment catalog entries.", Catalog.Length);
    }

    // Deterministic ids so re-running against a fresh DB yields stable rows.
    private static Treatment New(
        string id,
        DiseaseClass disease,
        string name,
        TreatmentType type,
        int rank,
        string description,
        string dosage,
        int repeatAfterDays,
        int phiDays,
        CostLevel cost) =>
        new()
        {
            Id = new Guid(id),
            DiseaseClass = disease,
            Name = name,
            Type = type,
            Rank = rank,
            Description = description,
            Dosage = dosage,
            RepeatAfterDays = repeatAfterDays,
            PhiDays = phiDays,
            CostLevel = cost,
        };

    private static readonly Treatment[] Catalog =
    [
        // Late blight (Phytophthora infestans)
        New("a0000000-0000-0000-0000-000000000001", DiseaseClass.LateBlight, "Trichoderma harzianum",
            TreatmentType.Biological, 1, "Biological first line. Apply as a foliar spray at the first sign; safe up to harvest.",
            "2g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-000000000002", DiseaseClass.LateBlight, "Chlorothalonil 75% WP",
            TreatmentType.Chemical, 2, "Protectant fungicide. Use when spread exceeds three rows. Wear protective equipment.",
            "1.5g/L", 10, 7, CostLevel.Medium),

        // Early blight (Alternaria solani)
        New("a0000000-0000-0000-0000-000000000003", DiseaseClass.EarlyBlight, "Bacillus subtilis",
            TreatmentType.Biological, 1, "Biological control effective on early lesions. Repeat weekly during humid spells.",
            "3g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-000000000004", DiseaseClass.EarlyBlight, "Mancozeb 80% WP",
            TreatmentType.Chemical, 2, "Broad-spectrum protectant. Apply on a 10-day cycle once symptoms are confirmed.",
            "2.5g/L", 10, 5, CostLevel.Medium),

        // Fusarium wilt (Fusarium oxysporum)
        New("a0000000-0000-0000-0000-000000000005", DiseaseClass.FusariumWilt, "Trichoderma viride soil drench",
            TreatmentType.Biological, 1, "Soil-borne pathogen — apply as a root-zone drench. Remove and destroy wilted plants.",
            "5g/L soil drench", 14, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-000000000006", DiseaseClass.FusariumWilt, "Carbendazim 50% WP",
            TreatmentType.Chemical, 2, "Systemic drench for severe infections. Rotate to avoid resistance build-up.",
            "1g/L soil drench", 15, 7, CostLevel.High),

        // Powdery mildew
        New("a0000000-0000-0000-0000-000000000007", DiseaseClass.PowderyMildew, "Potassium bicarbonate",
            TreatmentType.Biological, 1, "Contact biological/mineral spray. Excellent eradicant on early powdery mildew.",
            "5g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-000000000008", DiseaseClass.PowderyMildew, "Sulphur 80% WG",
            TreatmentType.Chemical, 2, "Mineral fungicide. Do not apply above 30°C to avoid leaf scorch.",
            "2g/L", 10, 1, CostLevel.Low),

        // Bacterial spot
        New("a0000000-0000-0000-0000-000000000009", DiseaseClass.BacterialSpot, "Bacillus amyloliquefaciens",
            TreatmentType.Biological, 1, "Biological suppressant. Improve airflow and avoid overhead irrigation.",
            "3g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-00000000000a", DiseaseClass.BacterialSpot, "Copper hydroxide 77% WP",
            TreatmentType.Chemical, 2, "Copper bactericide. Tank-mix with mancozeb for resistant strains.",
            "2g/L", 7, 5, CostLevel.Medium),

        // Leaf mold (Passalora fulva)
        New("a0000000-0000-0000-0000-00000000000b", DiseaseClass.LeafMold, "Bacillus subtilis",
            TreatmentType.Biological, 1, "Reduce greenhouse humidity below 85%. Apply biological at first yellowing.",
            "3g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-00000000000c", DiseaseClass.LeafMold, "Chlorothalonil 75% WP",
            TreatmentType.Chemical, 2, "Protectant fungicide for established leaf mold. Ensure full leaf coverage.",
            "1.5g/L", 10, 7, CostLevel.Medium),

        // Septoria leaf spot
        New("a0000000-0000-0000-0000-00000000000d", DiseaseClass.SeptoriaLeafSpot, "Trichoderma harzianum",
            TreatmentType.Biological, 1, "Remove infected lower leaves. Apply biological and mulch to limit soil splash.",
            "2g/L foliar spray", 7, 0, CostLevel.Low),
        New("a0000000-0000-0000-0000-00000000000e", DiseaseClass.SeptoriaLeafSpot, "Mancozeb 80% WP",
            TreatmentType.Chemical, 2, "Protectant on a 7–10 day schedule during wet weather.",
            "2.5g/L", 10, 5, CostLevel.Medium),

        // Spider mites (Tetranychus urticae)
        New("a0000000-0000-0000-0000-00000000000f", DiseaseClass.SpiderMites, "Phytoseiulus persimilis (predatory mites)",
            TreatmentType.Biological, 1, "Release predatory mites for biological control. Raise humidity to slow mite reproduction.",
            "Release per supplier rate", 14, 0, CostLevel.Medium),
        New("a0000000-0000-0000-0000-000000000010", DiseaseClass.SpiderMites, "Abamectin 1.8% EC",
            TreatmentType.Chemical, 2, "Miticide for heavy infestations. Target leaf undersides; rotate chemistry.",
            "0.5ml/L", 14, 3, CostLevel.High),
    ];
}
