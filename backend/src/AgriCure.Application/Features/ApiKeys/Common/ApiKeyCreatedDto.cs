namespace AgriCure.Application.Features.ApiKeys.Common;

/// <summary>
/// Returned only at create time. The PlaintextKey field is the ONLY place the
/// caller ever sees the plaintext value — it is never persisted, only its
/// SHA-256 hash, so this is the only chance to record it.
/// </summary>
public sealed record ApiKeyCreatedDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string PlaintextKey,
    string TokenLast4,
    string Scope,
    DateTimeOffset CreatedAt);
