namespace AgriCure.Application.Common.Auth;

public interface IJwtTokenService
{
    AccessToken IssueAccessToken(Guid userId, string email, IEnumerable<string> roles);

    string GenerateRefreshTokenValue();
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
