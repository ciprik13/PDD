using AgriCure.Domain.Identity;

namespace AgriCure.Application.Features.ApiKeys.Common;

public static class ApiKeyMapper
{
    public static ApiKeyDto ToDto(this ApiKey key) =>
        new(
            key.Id,
            key.OwnerUserId,
            key.Name,
            key.TokenLast4,
            key.Scope,
            key.CreatedAt,
            key.LastUsedAt,
            key.RevokedAt,
            key.IsActive);
}
