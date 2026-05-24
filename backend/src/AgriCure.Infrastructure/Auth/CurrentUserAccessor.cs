using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AgriCure.Application.Common.Auth;
using AgriCure.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace AgriCure.Infrastructure.Auth;

internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            // ASP.NET Core's JsonWebTokenHandler (default since .NET 7) preserves
            // raw "sub". Older JwtSecurityTokenHandler maps it to NameIdentifier.
            // Read both for safety.
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        httpContextAccessor.HttpContext?.User?.IsInRole(ApplicationRole.Admin) == true;

    public bool IsAgriculture =>
        httpContextAccessor.HttpContext?.User?.IsInRole(ApplicationRole.Agriculture) == true;

    public Guid RequireUserId() =>
        UserId ?? throw new AuthenticationFailedException("Caller is not authenticated.");
}
