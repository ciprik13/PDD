using System.Net;
using System.Net.Http.Json;
using AgriCure.Application.Common.ApiKeys;
using AgriCure.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.ApiKeys;

[Collection(IntegrationTestCollection.Name)]
public class ApiKeyAuthenticationTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ApiKeyAuthenticationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Probe_without_header_returns_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/test-probe/ingest");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Probe_with_malformed_header_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, "not-a-key");

        var resp = await client.GetAsync("/test-probe/ingest");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Probe_with_valid_active_key_returns_200_and_resolves_owner_identity()
    {
        var (plaintext, ownerId) = await IssueKeyForFreshAgricultureUserAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, plaintext);

        var resp = await client.GetAsync("/test-probe/ingest");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<ProbeResponse>();
        body.Should().NotBeNull();
        body!.UserId.Should().Be(ownerId);
        body.IsAgriculture.Should().BeTrue();
        body.IsSystem.Should().BeTrue();
        body.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Probe_with_revoked_key_returns_401()
    {
        var (plaintext, ownerId) = await IssueKeyForFreshAgricultureUserAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var keys = await svc.ListAsync(ownerId, includeRevoked: false, default);
            await svc.RevokeAsync(keys.Single().Id, default);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, plaintext);

        var resp = await client.GetAsync("/test-probe/ingest");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Probe_when_owner_loses_agriculture_role_returns_401()
    {
        var (plaintext, ownerId) = await IssueKeyForFreshAgricultureUserAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AgriCure.Infrastructure.Identity.ApplicationUser>>();
            var user = await userManager.FindByIdAsync(ownerId.ToString());
            user.Should().NotBeNull();
            await userManager.RemoveFromRoleAsync(user!, AgriCure.Infrastructure.Identity.ApplicationRole.Agriculture);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, plaintext);

        var resp = await client.GetAsync("/test-probe/ingest");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string Plaintext, Guid OwnerUserId)> IssueKeyForFreshAgricultureUserAsync()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var admin = await GetOrCreateAdminUserIdAsync(scope.ServiceProvider);

        var created = await svc.IssueAsync(agriculture.UserId, $"test-{Guid.NewGuid():N}", admin, default);
        return (created.PlaintextKey, agriculture.UserId);
    }

    private static async Task<Guid> GetOrCreateAdminUserIdAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<AgriCure.Infrastructure.Identity.ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<AgriCure.Infrastructure.Identity.ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(AgriCure.Infrastructure.Identity.ApplicationRole.Admin))
        {
            await roleManager.CreateAsync(new AgriCure.Infrastructure.Identity.ApplicationRole { Name = AgriCure.Infrastructure.Identity.ApplicationRole.Admin });
        }

        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var user = new AgriCure.Infrastructure.Identity.ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(user, "P@ssw0rd!ABC");
        await userManager.AddToRoleAsync(user, AgriCure.Infrastructure.Identity.ApplicationRole.Admin);
        return user.Id;
    }

    private sealed record ProbeResponse(Guid? UserId, bool IsAdmin, bool IsAgriculture, bool IsSystem);
}
