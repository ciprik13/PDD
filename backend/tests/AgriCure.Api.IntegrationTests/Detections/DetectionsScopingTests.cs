using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Detections;

[Collection(IntegrationTestCollection.Name)]
public class DetectionsScopingTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public DetectionsScopingTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_list_with_plain_user_role_returns_403()
    {
        var plainUser = await _factory.CreatePlainUserAsync();

        var resp = await plainUser.Client.GetAsync("/api/detections");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        var resp = await agriculture.Client.PostAsJsonAsync(
            "/api/detections",
            DetectionTestData.BuildCreateBody());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();
        var id = Guid.NewGuid();

        var resp = await agriculture.Client.PutAsJsonAsync(
            $"/api/detections/{id}",
            DetectionTestData.BuildUpdateBody(id));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        var resp = await agriculture.Client.DeleteAsync($"/api/detections/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_list_for_agriculture_returns_only_own_detections()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();
        var bob   = await _factory.CreateAgricultureAsync();

        var alicePlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: alice.UserId);
        var bobPlantId   = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: bob.UserId);
        var orphanPlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: null);

        var aliceResp = await alice.Client.GetAsync("/api/detections?limit=500");
        aliceResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await aliceResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var plantIds = doc.RootElement.EnumerateArray()
            .Select(d => d.GetProperty("plantId").GetString())
            .ToList();

        plantIds.Should().Contain(alicePlantId);
        plantIds.Should().NotContain(bobPlantId);
        plantIds.Should().NotContain(orphanPlantId);
    }

    [Fact]
    public async Task Get_list_for_admin_returns_all_detections()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();

        var alicePlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: alice.UserId);
        var orphanPlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: null);

        var resp = await admin.Client.GetAsync("/api/detections?limit=500");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var plantIds = doc.RootElement.EnumerateArray()
            .Select(d => d.GetProperty("plantId").GetString())
            .ToList();

        plantIds.Should().Contain(alicePlantId);
        plantIds.Should().Contain(orphanPlantId);
    }

    [Fact]
    public async Task Get_by_id_returns_404_when_agriculture_does_not_own_plant()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();
        var bob   = await _factory.CreateAgricultureAsync();

        var bobPlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: bob.UserId);
        var bobDetectionId = await GetFirstDetectionIdForPlantAsync(admin.Client, bobPlantId);

        var resp = await alice.Client.GetAsync($"/api/detections/{bobDetectionId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_by_id_returns_404_when_plant_is_unowned_and_caller_is_agriculture()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();

        var orphanPlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: null);
        var orphanDetectionId = await GetFirstDetectionIdForPlantAsync(admin.Client, orphanPlantId);

        var resp = await alice.Client.GetAsync($"/api/detections/{orphanDetectionId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_by_id_returns_200_when_agriculture_owns_plant()
    {
        var admin = await _factory.CreateAdminAsync();
        var alice = await _factory.CreateAgricultureAsync();

        var alicePlantId = await SeedPlantAndDetectionAsync(admin.Client, ownerUserId: alice.UserId);
        var aliceDetectionId = await GetFirstDetectionIdForPlantAsync(admin.Client, alicePlantId);

        var resp = await alice.Client.GetAsync($"/api/detections/{aliceDetectionId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> GetFirstDetectionIdForPlantAsync(HttpClient adminClient, string plantId)
    {
        var resp = await adminClient.GetAsync("/api/detections?limit=500");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var match = doc.RootElement.EnumerateArray()
            .First(d => d.GetProperty("plantId").GetString() == plantId);

        return match.GetProperty("id").GetGuid();
    }

    // Seeds a plant with the given owner via direct DbContext write, then creates a
    // detection on it via the admin's POST /api/detections. Returns the plantId.
    private async Task<string> SeedPlantAndDetectionAsync(HttpClient adminClient, Guid? ownerUserId)
    {
        var plantId = $"P-{Guid.NewGuid():N}".Substring(0, 16);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgriCure.Infrastructure.Persistence.AppDbContext>();
            db.Plants.Add(new AgriCure.Domain.Detections.Plant
            {
                Id = plantId,
                CreatedAt = DateTimeOffset.UtcNow,
                OwnerUserId = ownerUserId,
            });
            await db.SaveChangesAsync();
        }

        var createResp = await adminClient.PostAsJsonAsync("/api/detections",
            DetectionTestData.BuildCreateBody(plantId));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        return plantId;
    }
}
