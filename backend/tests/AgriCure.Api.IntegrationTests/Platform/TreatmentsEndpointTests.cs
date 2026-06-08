using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriCure.Api.IntegrationTests.Detections;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Platform;

[Collection(IntegrationTestCollection.Name)]
public class TreatmentsEndpointTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task Get_treatments_returns_seeded_catalog_biological_first()
    {
        var alice = await factory.CreateAgricultureAsync();

        var resp = await alice.Client.GetAsync("/api/treatments?diseaseClass=late_blight");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var entries = doc.RootElement.EnumerateArray().ToList();

        entries.Should().NotBeEmpty();
        entries[0].GetProperty("rank").GetInt32().Should().Be(1);
        entries[0].GetProperty("type").GetString().Should().Be("biological");
        entries[0].GetProperty("tags").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_treatments_with_unknown_disease_returns_422()
    {
        var alice = await factory.CreateAgricultureAsync();

        var resp = await alice.Client.GetAsync("/api/treatments?diseaseClass=not_a_disease");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Record_applied_treatment_then_list_returns_it()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        var plantId = await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);
        var treatmentId = await FirstTreatmentIdAsync(alice.Client, "early_blight");

        var applyResp = await alice.Client.PostAsJsonAsync("/api/treatments/applied", new
        {
            treatmentId,
            plantId,
            appliedAt = DateTimeOffset.UtcNow,
            notes = (string?)null,
        });
        applyResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResp = await alice.Client.GetAsync($"/api/treatments/applied?plantId={plantId}");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var records = doc.RootElement.EnumerateArray().ToList();

        records.Should().ContainSingle();
        records[0].GetProperty("plantId").GetString().Should().Be(plantId);
        records[0].GetProperty("treatmentId").GetString().Should().Be(treatmentId);
        records[0].GetProperty("row").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task Record_applied_treatment_on_unowned_plant_returns_422()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();
        var bob = await factory.CreateAgricultureAsync();

        var bobPlant = await SeedPlantWithDetectionAsync(admin.Client, bob.UserId);
        var treatmentId = await FirstTreatmentIdAsync(alice.Client, "early_blight");

        var applyResp = await alice.Client.PostAsJsonAsync("/api/treatments/applied", new
        {
            treatmentId,
            plantId = bobPlant,
            appliedAt = DateTimeOffset.UtcNow,
            notes = (string?)null,
        });

        applyResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<string> FirstTreatmentIdAsync(HttpClient client, string diseaseClass)
    {
        var resp = await client.GetAsync($"/api/treatments?diseaseClass={diseaseClass}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement[0].GetProperty("id").GetString()!;
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
