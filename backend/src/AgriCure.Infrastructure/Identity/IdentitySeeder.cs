using AgriCure.Application.Common.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgriCure.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await EnsureRolesAsync(sp);
        await EnsureAdminUserAsync(sp);
    }

    private static async Task EnsureRolesAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in new[] { ApplicationRole.Admin, ApplicationRole.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }
    }

    private static async Task EnsureAdminUserAsync(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(options.Email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, options.Password);
        var logger = sp.GetRequiredService<ILogger<ApplicationUser>>();

        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Admin user seed failed: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, ApplicationRole.Admin);
        logger.LogInformation("Admin user {Email} seeded.", options.Email);
    }
}
