using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriCure.Api.IntegrationTests.Detections;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Platform;

[Collection(IntegrationTestCollection.Name)]
public class PlantsEndpointTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task Get_plants_with_plain_user_returns_403()
    {
        var plain = await factory.CreatePlainUserAsync();

        var resp = await plain.Client.GetAsync("/api/plants");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_plants_for_agriculture_returns_only_owned_plants_with_latest_label()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();
        var bob = await factory.CreateAgricultureAsync();

        var alicePlant = await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);
        var bobPlant = await SeedPlantWithDetectionAsync(admin.Client, bob.UserId);

        var resp = await alice.Client.GetAsync("/api/plants");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var plants = doc.RootElement.EnumerateArray().ToList();

        var ids = plants.Select(p => p.GetProperty("plantId").GetString()).ToList();
        ids.Should().Contain(alicePlant);
        ids.Should().NotContain(bobPlant);

        var mine = plants.First(p => p.GetProperty("plantId").GetString() == alicePlant);
        mine.GetProperty("latestLabel").GetString().Should().Be("Early Blight");
        mine.GetProperty("latestSeverity").GetString().Should().Be("warning");
        mine.GetProperty("detectionCount").GetInt32().Should().BeGreaterThan(0);
    }

    private async Task<string> SeedPlantWithDetectionAsync(HttpClient adminClient, Guid ownerUserId)
    {
        var plantId = $"P-{Guid.NewGuid():N}"[..16];

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AgriCure.Infrastructure.Persistence.AppDbContext>();
            db.Plants.Add(new AgriCure.Domain.Detections.Plant
            {
                Id = plantId,
                CreatedAt = DateTimeOffset.UtcNow,
                OwnerUserId = ownerUserId,
            });
            await db.SaveChangesAsync();
        }

        var createResp = await adminClient.PostAsJsonAsync(
            "/api/detections", DetectionTestData.BuildCreateBody(plantId));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        return plantId;
    }
}
