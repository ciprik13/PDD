using AgriCure.Application.Common.Auth;
using AgriCure.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AgriCure.Infrastructure.Auth;

internal sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<IdentityOperationResult> RegisterAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return new IdentityOperationResult(
                UserId: null,
                Errors: createResult.Errors.Select(ToInfo).ToArray());
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRole.User);
        if (!roleResult.Succeeded)
        {
            return new IdentityOperationResult(
                UserId: null,
                Errors: roleResult.Errors.Select(ToInfo).ToArray());
        }

        return new IdentityOperationResult(user.Id, Array.Empty<IdentityErrorInfo>());
    }

    public async Task<IdentityUserContext?> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return null;

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return new IdentityUserContext(user.Id, user.Email ?? email, roles);
    }

    public async Task<IdentityUserContext?> GetUserContextAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return new IdentityUserContext(user.Id, user.Email ?? string.Empty, roles);
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }
        return await userManager.IsInRoleAsync(user, roleName);
    }

    private static IdentityErrorInfo ToInfo(IdentityError error) =>
        new(error.Code, error.Description);
}
