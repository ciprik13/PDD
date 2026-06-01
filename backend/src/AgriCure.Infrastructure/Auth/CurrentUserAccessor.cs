using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AgriCure.Application.Common.Auth;
using AgriCure.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace AgriCure.Infrastructure.Auth;

internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // JwtTokenService uses JwtSecurityTokenHandler to write tokens; the legacy
            // handler maps "sub" → ClaimTypes.NameIdentifier during validation.
            // Read NameIdentifier first; fall back to raw "sub" in case the pipeline
            // switches to JsonWebTokenHandler later.
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        User?.IsInRole(ApplicationRole.Admin) == true;

    public bool IsAgriculture =>
        User?.IsInRole(ApplicationRole.Agriculture) == true;

    public bool IsSystem =>
        User?.IsInRole(ApplicationRole.System) == true;

    public Guid RequireUserId() =>
        UserId ?? throw new AuthenticationFailedException("Caller is not authenticated.");
}
