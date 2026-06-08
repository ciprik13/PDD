using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriCure.Api.IntegrationTests.Detections;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Platform;

[Collection(IntegrationTestCollection.Name)]
public class DashboardEndpointTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task Get_dashboard_stats_counts_todays_detections_for_owner()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);

        var resp = await alice.Client.GetAsync("/api/dashboard/stats");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.GetProperty("detectionsToday").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("plantsTracked").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("avgConfidence").GetDouble().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_system_status_reports_online_when_a_detection_is_recent()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);

        var resp = await alice.Client.GetAsync("/api/system/status");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("deviceStatus").GetString().Should().Be("online");
    }

    [Fact]
    public async Task Get_stand_position_returns_row_from_latest_detection()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);

        var resp = await alice.Client.GetAsync("/api/stand/position");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("row").GetInt32().Should().Be(7);
    }

    private async Task SeedPlantWithDetectionAsync(HttpClient adminClient, Guid ownerUserId)
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
    }
}
