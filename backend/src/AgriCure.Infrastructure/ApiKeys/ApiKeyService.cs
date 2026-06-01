using System.Security.Cryptography;
using System.Text;
using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.ApiKeys.Common;
using AgriCure.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Infrastructure.ApiKeys;

internal sealed class ApiKeyService(
    IApplicationDbContext db,
    TimeProvider time) : IApiKeyService
{
    public const string KeyPrefix = "pdd_live_";
    public const string DefaultScope = "detections:ingest";
    private const int RandomBytes = 32; // 256 bits entropy
    private static readonly TimeSpan LastUsedThrottle = TimeSpan.FromMinutes(5);

    public async Task<ApiKeyCreatedDto> IssueAsync(
        Guid ownerUserId,
        string name,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var plaintext = GeneratePlaintextKey();
        var hash = Sha256Hex(plaintext);
        var last4 = plaintext[^4..];
        var now = time.GetUtcNow();

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name,
            TokenHash = hash,
            TokenLast4 = last4,
            Scope = DefaultScope,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApiKeyCreatedDto(
            apiKey.Id,
            apiKey.OwnerUserId,
            apiKey.Name,
            plaintext,
            apiKey.TokenLast4,
            apiKey.Scope,
            apiKey.CreatedAt);
    }

    public async Task<bool> RevokeAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken)
            .ConfigureAwait(false);

        if (key is null)
        {
            return false;
        }

        if (key.RevokedAt is null)
        {
            key.RevokedAt = time.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<ApiKeyDto?> GetByIdAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken)
            .ConfigureAwait(false);
        return key?.ToDto();
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(
        Guid? ownerUserId,
        bool includeRevoked,
        CancellationToken cancellationToken = default)
    {
        var query = db.ApiKeys.AsQueryable();

        if (ownerUserId is { } owner)
        {
            query = query.Where(k => k.OwnerUserId == owner);
        }

        if (!includeRevoked)
        {
            query = query.Where(k => k.RevokedAt == null);
        }

        var rows = await query
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(k => k.ToDto()).ToList();
    }

    public Task<ApiKey?> ResolveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.ApiKeys.FirstOrDefaultAsync(
            k => k.TokenHash == tokenHash && k.RevokedAt == null,
            cancellationToken);

    public async Task TouchLastUsedAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();

        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken)
            .ConfigureAwait(false);

        if (key is null)
        {
            return;
        }

        if (key.LastUsedAt is { } previous && now - previous < LastUsedThrottle)
        {
            return;
        }

        key.LastUsedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string GeneratePlaintextKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomBytes);
        var encoded = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return KeyPrefix + encoded;
    }

    internal static string Sha256Hex(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
