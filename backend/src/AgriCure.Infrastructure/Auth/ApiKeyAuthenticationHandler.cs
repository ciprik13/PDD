using System.Security.Claims;
using System.Text.Encodings.Web;
using AgriCure.Application.Common.ApiKeys;
using AgriCure.Infrastructure.ApiKeys;
using AgriCure.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgriCure.Infrastructure.Auth;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyService apiKeyService,
    UserManager<ApplicationUser> userManager)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private const int ExpectedPlaintextLength = 52; // "pdd_live_" (9) + 43 base64url chars

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthorization.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var plaintext = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(plaintext)
            || plaintext.Length != ExpectedPlaintextLength
            || !plaintext.StartsWith(ApiKeyAuthorization.KeyPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("Malformed API key.");
        }

        var hash = ApiKeyService.Sha256Hex(plaintext);
        var key = await apiKeyService.ResolveByTokenHashAsync(hash, Context.RequestAborted);
        if (key is null)
        {
            return AuthenticateResult.Fail("API key not recognised.");
        }

        var owner = await userManager.FindByIdAsync(key.OwnerUserId.ToString());
        if (owner is null)
        {
            return AuthenticateResult.Fail("API key owner no longer exists.");
        }

        if (!await userManager.IsInRoleAsync(owner, ApplicationRole.Agriculture))
        {
            return AuthenticateResult.Fail("API key owner no longer has the agriculture role.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, key.OwnerUserId.ToString()),
            new(ClaimTypes.Role, ApplicationRole.System),
            new(ClaimTypes.Role, ApplicationRole.Agriculture),
            new(ApiKeyAuthorization.ScopeClaimType, key.Scope),
            new(ApiKeyAuthorization.ApiKeyIdClaimType, key.Id.ToString()),
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthorization.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthorization.Scheme);

        var keyId = key.Id;
        _ = Task.Run(async () =>
        {
            try
            {
                await apiKeyService.TouchLastUsedAsync(keyId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to update LastUsedAt for API key {ApiKeyId}", keyId);
            }
        }, CancellationToken.None);

        return AuthenticateResult.Success(ticket);
    }
}
