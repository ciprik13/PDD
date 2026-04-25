using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Identity;
using Microsoft.Extensions.Options;

namespace AgriCure.Application.Features.Auth;

internal sealed class AuthTokenIssuer(
    IJwtTokenService tokens,
    IApplicationDbContext db,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider time)
{
    public async Task<AuthResultDto> IssueAsync(
        IdentityUserContext user, CancellationToken cancellationToken)
    {
        var accessToken = tokens.IssueAccessToken(user.UserId, user.Email, user.Roles);
        var refreshValue = tokens.GenerateRefreshTokenValue();
        var refreshExpiresAt = time.GetUtcNow().AddDays(jwtOptions.Value.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshValue,
            CreatedAt = time.GetUtcNow(),
            ExpiresAt = refreshExpiresAt,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshValue,
            refreshExpiresAt);
    }
}
