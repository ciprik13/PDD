namespace AgriCure.Domain.Detections;

public class Detection
{
    public Guid Id { get; set; }

    public long FrameId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public Severity Severity { get; set; }

    public List<ClassPrediction> Predictions { get; set; } = [];

    public BoundingBox BoundingBox { get; set; } = default!;

    public int InferenceMs { get; set; }

    public bool ConfidenceGatePassed { get; set; }

    public int Row { get; set; }

    public string PlantId { get; set; } = string.Empty;

    public double PositionMeters { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
