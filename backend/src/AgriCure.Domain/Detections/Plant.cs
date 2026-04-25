namespace AgriCure.Domain.Detections;

public class Plant
{
    /// <summary>Human-readable code, e.g. "P023". Acts as PK.</summary>
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
