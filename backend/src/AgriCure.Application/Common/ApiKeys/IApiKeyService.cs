using AgriCure.Application.Features.ApiKeys.Common;
using AgriCure.Domain.Identity;

namespace AgriCure.Application.Common.ApiKeys;

/// <summary>
/// Lifecycle and lookup operations for system API keys. Implementation owns
/// plaintext generation, SHA-256 hashing, and the DB read/write paths.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a fresh plaintext key, stores its hash, and returns the plaintext
    /// once for the caller to hand to the device. Subsequent reads of this key
    /// will never expose the plaintext again.
    /// </summary>
    Task<ApiKeyCreatedDto> IssueAsync(
        Guid ownerUserId,
        string name,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-revokes a key by setting RevokedAt. Returns false if no key exists with
    /// that id. Idempotent: revoking an already-revoked key returns true without
    /// updating RevokedAt again.
    /// </summary>
    Task<bool> RevokeAsync(Guid keyId, CancellationToken cancellationToken = default);

    Task<ApiKeyDto?> GetByIdAsync(Guid keyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApiKeyDto>> ListAsync(
        Guid? ownerUserId,
        bool includeRevoked,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Used by the ApiKey authentication handler. Returns the active key matching
    /// the given hash, or null if missing or revoked.
    /// </summary>
    Task<ApiKey?> ResolveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fire-and-forget update of LastUsedAt. Internally throttles to 5 minutes
    /// so we don't write-amplify on every request.
    /// </summary>
    Task TouchLastUsedAsync(Guid keyId, CancellationToken cancellationToken = default);
}
