using AgriCure.Domain.Detections;

namespace AgriCure.Domain.Treatments;

/// <summary>
/// A record that a <see cref="Treatment"/> was applied to a <see cref="Plant"/> in the
/// field. Written by an agriculture user (or admin) and surfaced in the treatment
/// history and on the plant passport timeline. Ownership/visibility is derived from
/// the referenced plant's owner, mirroring how detections are scoped.
/// </summary>
public sealed class AppliedTreatment
{
    public Guid Id { get; set; }

    /// <summary>The recommended treatment that was applied.</summary>
    public Guid TreatmentId { get; set; }

    /// <summary>The plant the treatment was applied to (FK to <see cref="Plant.Id"/>).</summary>
    public string PlantId { get; set; } = string.Empty;

    /// <summary>When the treatment was applied in the field.</summary>
    public DateTimeOffset AppliedAt { get; set; }

    /// <summary>Optional free-text note (e.g. "full row", weather conditions).</summary>
    public string? Notes { get; set; }

    /// <summary>ApplicationUser.Id of whoever recorded the application. Audit trail.</summary>
    public Guid AppliedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
