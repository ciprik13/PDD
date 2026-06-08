using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgriCure.Api.IntegrationTests.Platform;

[Collection(IntegrationTestCollection.Name)]
public class AdminUsersTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task Create_agriculture_user_then_it_appears_in_the_agriculture_list()
    {
        var admin = await factory.CreateAdminAsync();
        var email = $"grower-{Guid.NewGuid():N}@example.com";

        var createResp = await admin.Client.PostAsJsonAsync("/api/admin/users", new
        {
            email,
            password = "P@ssw0rd!ABC",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        using var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        created.RootElement.GetProperty("email").GetString().Should().Be(email);
        created.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).Should().Contain("agriculture");

        var listResp = await admin.Client.GetAsync("/api/admin/users?role=agriculture");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var list = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        list.RootElement.EnumerateArray()
            .Select(u => u.GetProperty("email").GetString())
            .Should().Contain(email);
    }

    [Fact]
    public async Task Create_user_with_agriculture_role_returns_403()
    {
        var agriculture = await factory.CreateAgricultureAsync();

        var resp = await agriculture.Client.PostAsJsonAsync("/api/admin/users", new
        {
            email = $"x-{Guid.NewGuid():N}@example.com",
            password = "P@ssw0rd!ABC",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Grant_then_revoke_agriculture_role_for_an_existing_user()
    {
        var admin = await factory.CreateAdminAsync();
        var plain = await factory.CreatePlainUserAsync();

        var grant = await admin.Client.PutAsJsonAsync(
            $"/api/admin/users/{plain.UserId}/agriculture", new { assigned = true });
        grant.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterGrant = await admin.Client.GetAsync("/api/admin/users?role=agriculture");
        using (var doc = JsonDocument.Parse(await afterGrant.Content.ReadAsStringAsync()))
        {
            doc.RootElement.EnumerateArray()
                .Select(u => u.GetProperty("id").GetGuid())
                .Should().Contain(plain.UserId);
        }

        var revoke = await admin.Client.PutAsJsonAsync(
            $"/api/admin/users/{plain.UserId}/agriculture", new { assigned = false });
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterRevoke = await admin.Client.GetAsync("/api/admin/users?role=agriculture");
        using (var doc = JsonDocument.Parse(await afterRevoke.Content.ReadAsStringAsync()))
        {
            doc.RootElement.EnumerateArray()
                .Select(u => u.GetProperty("id").GetGuid())
                .Should().NotContain(plain.UserId);
        }
    }
}
