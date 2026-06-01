using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgriCure.Api.IntegrationTests.ApiKeys;

[Collection(IntegrationTestCollection.Name)]
public class AdminApiKeysControllerTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminApiKeysControllerTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_with_admin_and_valid_body_returns_201_with_plaintext_key()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();

        var resp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = agriculture.UserId, name = $"device-{Guid.NewGuid():N}" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var plaintext = doc.RootElement.GetProperty("plaintextKey").GetString();
        plaintext.Should().NotBeNullOrEmpty();
        plaintext.Should().MatchRegex("^pdd_live_[A-Za-z0-9_-]{43}$");
        doc.RootElement.GetProperty("tokenLast4").GetString()
            .Should().Be(plaintext![^4..]);
        doc.RootElement.GetProperty("scope").GetString().Should().Be("detections:ingest");
    }

    [Fact]
    public async Task Post_with_admin_and_non_agriculture_owner_returns_422()
    {
        var admin = await _factory.CreateAdminAsync();
        var plainUser = await _factory.CreatePlainUserAsync();

        var resp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = plainUser.UserId, name = $"device-{Guid.NewGuid():N}" });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_with_admin_and_unknown_owner_returns_422()
    {
        var admin = await _factory.CreateAdminAsync();

        var resp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = Guid.NewGuid(), name = $"device-{Guid.NewGuid():N}" });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_duplicate_active_name_for_same_owner_returns_422()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();
        var name = $"device-{Guid.NewGuid():N}";

        var first = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys", new { ownerUserId = agriculture.UserId, name });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys", new { ownerUserId = agriculture.UserId, name });
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_two_keys_with_different_names_for_same_owner_succeed()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();

        var first = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys", new { ownerUserId = agriculture.UserId, name = $"device-{Guid.NewGuid():N}-a" });
        var second = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys", new { ownerUserId = agriculture.UserId, name = $"device-{Guid.NewGuid():N}-b" });

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await admin.Client.GetAsync($"/api/admin/api-keys?ownerUserId={agriculture.UserId}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await list.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Get_list_filters_by_owner()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();
        var bob = await _factory.CreateAgricultureAsync();

        await admin.Client.PostAsJsonAsync("/api/admin/api-keys", new { ownerUserId = alice.UserId, name = $"a-{Guid.NewGuid():N}" });
        await admin.Client.PostAsJsonAsync("/api/admin/api-keys", new { ownerUserId = bob.UserId, name = $"b-{Guid.NewGuid():N}" });

        var resp = await admin.Client.GetAsync($"/api/admin/api-keys?ownerUserId={alice.UserId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var owners = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("ownerUserId").GetGuid())
            .ToList();
        owners.Should().OnlyContain(g => g == alice.UserId);
    }

    [Fact]
    public async Task Get_list_default_excludes_revoked()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();

        var createResp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = agriculture.UserId, name = $"rev-{Guid.NewGuid():N}" });
        var createJson = await createResp.Content.ReadAsStringAsync();
        var createdId = JsonDocument.Parse(createJson).RootElement.GetProperty("id").GetGuid();

        await admin.Client.DeleteAsync($"/api/admin/api-keys/{createdId}");

        var listResp = await admin.Client.GetAsync($"/api/admin/api-keys?ownerUserId={agriculture.UserId}");
        var listJson = await listResp.Content.ReadAsStringAsync();
        var ids = JsonDocument.Parse(listJson).RootElement.EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid())
            .ToList();
        ids.Should().NotContain(createdId);

        var listWithRevoked = await admin.Client.GetAsync($"/api/admin/api-keys?ownerUserId={agriculture.UserId}&includeRevoked=true");
        var withRevokedJson = await listWithRevoked.Content.ReadAsStringAsync();
        var idsWithRevoked = JsonDocument.Parse(withRevokedJson).RootElement.EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid())
            .ToList();
        idsWithRevoked.Should().Contain(createdId);
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_unknown()
    {
        var admin = await _factory.CreateAdminAsync();

        var resp = await admin.Client.GetAsync($"/api/admin/api-keys/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_active_returns_204_and_marks_revoked()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();

        var createResp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = agriculture.UserId, name = $"del-{Guid.NewGuid():N}" });
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        var delResp = await admin.Client.DeleteAsync($"/api/admin/api-keys/{id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await admin.Client.GetAsync($"/api/admin/api-keys/{id}");
        var json = await getResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("revokedAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        doc.RootElement.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Delete_already_revoked_returns_204_idempotently()
    {
        var admin = await _factory.CreateAdminAsync();
        var agriculture = await _factory.CreateAgricultureAsync();

        var createResp = await admin.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = agriculture.UserId, name = $"idem-{Guid.NewGuid():N}" });
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        (await admin.Client.DeleteAsync($"/api/admin/api-keys/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.Client.DeleteAsync($"/api/admin/api-keys/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_unknown_returns_404()
    {
        var admin = await _factory.CreateAdminAsync();

        var resp = await admin.Client.DeleteAsync($"/api/admin/api-keys/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Agriculture_caller_gets_403_on_all_endpoints()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        var listResp = await agriculture.Client.GetAsync("/api/admin/api-keys");
        listResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var getResp = await agriculture.Client.GetAsync($"/api/admin/api-keys/{Guid.NewGuid()}");
        getResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var postResp = await agriculture.Client.PostAsJsonAsync(
            "/api/admin/api-keys",
            new { ownerUserId = agriculture.UserId, name = "x" });
        postResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var delResp = await agriculture.Client.DeleteAsync($"/api/admin/api-keys/{Guid.NewGuid()}");
        delResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Plain_user_caller_gets_403()
    {
        var plain = await _factory.CreatePlainUserAsync();

        var listResp = await plain.Client.GetAsync("/api/admin/api-keys");
        listResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_caller_gets_401()
    {
        var anon = _factory.CreateClient();

        var listResp = await anon.GetAsync("/api/admin/api-keys");
        listResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
