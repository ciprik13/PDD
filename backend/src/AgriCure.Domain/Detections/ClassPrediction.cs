namespace AgriCure.Domain.Detections;

public class ClassPrediction
{
    public Guid Id { get; set; }

    public Guid DetectionId { get; set; }

    public DiseaseClass DiseaseClass { get; set; }

    public double Confidence { get; set; }

    public string Label { get; set; } = string.Empty;

    /// <summary>0 = top prediction (highest confidence). Preserved for stable read order.</summary>
    public int Rank { get; set; }
}
