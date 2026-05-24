namespace AgriCure.Domain.Detections;

public class Plant
{
    /// <summary>Human-readable code, e.g. "P023". Acts as PK.</summary>
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// ApplicationUser.Id of the agriculture user who owns this plant. Null = unowned
    /// (admin-only visibility). Configured as a foreign key in PlantConfiguration.
    /// </summary>
    public Guid? OwnerUserId { get; set; }
}
