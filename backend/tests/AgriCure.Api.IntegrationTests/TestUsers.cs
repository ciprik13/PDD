using System.Net.Http.Headers;
using System.Net.Http.Json;
using AgriCure.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests;

internal static class TestUsers
{
    private const string Password = "P@ssw0rd!ABC";

    public static Task<AuthenticatedUser> CreateAdminAsync(
        this IntegrationTestWebAppFactory factory) =>
        factory.CreateUserInRoleAsync(ApplicationRole.Admin);

    public static Task<AuthenticatedUser> CreateAgricultureAsync(
        this IntegrationTestWebAppFactory factory) =>
        factory.CreateUserInRoleAsync(ApplicationRole.Agriculture);

    public static Task<AuthenticatedUser> CreatePlainUserAsync(
        this IntegrationTestWebAppFactory factory) =>
        factory.CreateUserInRoleAsync(ApplicationRole.User);

    private static async Task<AuthenticatedUser> CreateUserInRoleAsync(
        this IntegrationTestWebAppFactory factory,
        string roleName)
    {
        Guid userId;
        string email;

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }

            email = $"{roleName}-{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user, Password);
            createResult.Succeeded.Should().BeTrue(
                because: $"user create should succeed: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            roleResult.Succeeded.Should().BeTrue(
                because: $"role assign should succeed: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");

            userId = user.Id;
        }

        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password });
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return new AuthenticatedUser(client, userId, email);
    }

    public sealed record AuthenticatedUser(HttpClient Client, Guid UserId, string Email);

    private sealed record AuthResponse(string AccessToken);
}
