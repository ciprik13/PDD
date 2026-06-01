namespace AgriCure.Application.Features.ApiKeys.Common;

/// <summary>Metadata-only representation of an API key. Never contains the plaintext.</summary>
public sealed record ApiKeyDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string TokenLast4,
    string Scope,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt,
    bool IsActive);
