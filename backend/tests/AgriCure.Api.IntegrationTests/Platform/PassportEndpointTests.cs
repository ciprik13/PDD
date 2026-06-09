using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriCure.Api.IntegrationTests.Detections;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Platform;

[Collection(IntegrationTestCollection.Name)]
public class PassportEndpointTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task Get_passport_returns_404_for_unknown_plant()
    {
        var admin = await factory.CreateAdminAsync();

        var resp = await admin.Client.GetAsync($"/api/passports/UNKNOWN-{Guid.NewGuid():N}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_passport_returns_404_when_agriculture_does_not_own_plant()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();
        var bob = await factory.CreateAgricultureAsync();

        var bobPlant = await SeedPlantWithDetectionAsync(admin.Client, bob.UserId);

        var resp = await alice.Client.GetAsync($"/api/passports/{bobPlant}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_passport_builds_timeline_from_detections_and_applied_treatments()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        var plantId = await SeedPlantWithDetectionAsync(admin.Client, alice.UserId);

        // Apply a treatment so the passport timeline includes a treatment event.
        var treatmentId = await FirstTreatmentIdAsync(alice.Client, "early_blight");
        var applyResp = await alice.Client.PostAsJsonAsync("/api/treatments/applied", new
        {
            treatmentId,
            plantId,
            appliedAt = DateTimeOffset.UtcNow,
            notes = "full row",
        });
        applyResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var resp = await alice.Client.GetAsync($"/api/passports/{plantId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.GetProperty("id").GetString().Should().Be($"passport-{plantId}");
        root.GetProperty("currentStatus").GetString().Should().Be("warning");

        var eventTypes = root.GetProperty("events").EnumerateArray()
            .Select(e => e.GetProperty("type").GetString())
            .ToList();
        eventTypes.Should().Contain("created");
        eventTypes.Should().Contain("symptom");   // warning detection
        eventTypes.Should().Contain("treatment");

        root.GetProperty("severityHistory").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_passport_includes_image_url_on_detection_events_that_have_a_picture()
    {
        var admin = await factory.CreateAdminAsync();
        var alice = await factory.CreateAgricultureAsync();

        var (plantId, detectionId) = await SeedPlantWithDetectionAndPictureAsync(admin.Client, alice.UserId);

        var resp = await alice.Client.GetAsync($"/api/passports/{plantId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var events = doc.RootElement.GetProperty("events").EnumerateArray().ToList();

        // The detection event carries the linked picture's public URL.
        var detectionEvent = events.Single(e => e.GetProperty("id").GetString() == $"det-{detectionId}");
        detectionEvent.GetProperty("imageUrl").GetString().Should().NotBeNullOrWhiteSpace();

        // The "created" event has no picture, so its imageUrl stays null.
        var createdEvent = events.Single(e => e.GetProperty("type").GetString() == "created");
        createdEvent.GetProperty("imageUrl").ValueKind.Should().Be(JsonValueKind.Null);
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
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
                OwnerUserId = ownerUserId,
            });
            await db.SaveChangesAsync();
        }

        var createResp = await adminClient.PostAsJsonAsync(
            "/api/detections", DetectionTestData.BuildCreateBody(plantId));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        return plantId;
    }

    // Seeds a plant + detection, then links a Picture to that detection (as the ingest path does),
    // returning both ids so the test can assert the detection event surfaces the picture URL.
    private async Task<(string PlantId, Guid DetectionId)> SeedPlantWithDetectionAndPictureAsync(
        HttpClient adminClient, Guid ownerUserId)
    {
        var plantId = $"P-{Guid.NewGuid():N}"[..16];

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AgriCure.Infrastructure.Persistence.AppDbContext>();
            db.Plants.Add(new AgriCure.Domain.Detections.Plant
            {
                Id = plantId,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
                OwnerUserId = ownerUserId,
            });
            await db.SaveChangesAsync();
        }

        var createResp = await adminClient.PostAsJsonAsync(
            "/api/detections", DetectionTestData.BuildCreateBody(plantId));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var detectionId = createdDoc.RootElement.GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AgriCure.Infrastructure.Persistence.AppDbContext>();
            var pictureId = Guid.NewGuid();
            db.Pictures.Add(new AgriCure.Domain.Pictures.Picture
            {
                Id = pictureId,
                MimeType = "image/png",
                VirtualPath = $"detections/{detectionId:N}/frame.png",
                IsNew = false,
            });
            db.DetectionPictures.Add(new AgriCure.Domain.Detections.DetectionPicture
            {
                DetectionId = detectionId,
                PictureId = pictureId,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        return (plantId, detectionId);
    }
}
