namespace AgriCure.Domain.Identity;

/// <summary>
/// Long-lived authentication credential bound to an agriculture user, used by the
/// external AI/sync server to push detection results. Admin-managed: issued and
/// revoked from the admin REST surface; never re-issued (rotation = create new,
/// revoke old).
/// </summary>
public sealed class ApiKey
{
    public Guid Id { get; set; }

    /// <summary>ApplicationUser.Id of the agriculture user this key belongs to.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Human-readable label (e.g. "north-field-camera-1"). Max 100 chars.</summary>
    public string Name { get; set; } = default!;

    /// <summary>SHA-256 hex of the plaintext key. Lowercase, length 64.</summary>
    public string TokenHash { get; set; } = default!;

    /// <summary>Last 4 chars of the plaintext key, shown in list views for visual reference.</summary>
    public string TokenLast4 { get; set; } = default!;

    /// <summary>Permission scope. Today the only value is "detections:ingest".</summary>
    public string Scope { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>ApplicationUser.Id of the admin who issued the key. Audit trail.</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Updated on successful auth, throttled to 5-minute granularity. Null until first use.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>Null = active. Non-null = revoked; the timestamp records when.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null;
}
