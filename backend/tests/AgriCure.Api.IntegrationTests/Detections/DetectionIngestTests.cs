using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgriCure.Application.Common.ApiKeys;
using AgriCure.Infrastructure.Auth;
using AgriCure.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Api.IntegrationTests.Detections;

[Collection(IntegrationTestCollection.Name)]
public class DetectionIngestTests
{
    private static readonly byte[] TinyPng = new byte[]
    {
        0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
        0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1f, 0x15, 0xc4, 0x89,
    };

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IntegrationTestWebAppFactory _factory;

    public DetectionIngestTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Without_header_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata("plant-anon")));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task With_malformed_key_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, "not-a-key");
        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata("plant-mal")));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task With_valid_key_and_new_plant_returns_201_and_links_picture()
    {
        var (key, ownerId) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var plantId = $"plant-{Guid.NewGuid():N}";
        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata(plantId)));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("isNewPlant").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("isDuplicate").GetBoolean().Should().BeFalse();
        var detectionId = doc.RootElement.GetProperty("detectionId").GetGuid();
        var pictureId = doc.RootElement.GetProperty("pictureId").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.DetectionPictures.AnyAsync(dp =>
            dp.DetectionId == detectionId && dp.PictureId == pictureId))
            .Should().BeTrue();
        (await db.Plants.SingleAsync(p => p.Id == plantId)).OwnerUserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task With_valid_key_on_existing_owned_plant_returns_201_and_does_not_create_plant()
    {
        var (key, ownerId) = await IssueKeyAsync();
        var plantId = $"plant-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Plants.Add(new AgriCure.Domain.Detections.Plant
            {
                Id = plantId,
                OwnerUserId = ownerId,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata(plantId)));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("isNewPlant").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task With_plant_owned_by_other_user_returns_409()
    {
        var (key, _) = await IssueKeyAsync();
        var plantId = $"plant-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AgriCure.Infrastructure.Identity.ApplicationUser>>();
            var other = new AgriCure.Infrastructure.Identity.ApplicationUser
            {
                UserName = $"other-{Guid.NewGuid():N}@example.com",
                Email = $"other-{Guid.NewGuid():N}@example.com",
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(other, "P@ssw0rd!ABC");

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Plants.Add(new AgriCure.Domain.Detections.Plant
            {
                Id = plantId,
                OwnerUserId = other.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata(plantId)));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Duplicate_PlantId_FrameId_returns_200_with_isDuplicate_true()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var plantId = $"plant-{Guid.NewGuid():N}";
        var meta = BuildMetadata(plantId, frameId: 12345);

        var first = await client.PostAsync("/api/detections/ingest", BuildForm(meta));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstId = JsonDocument.Parse(await first.Content.ReadAsStringAsync())
            .RootElement.GetProperty("detectionId").GetGuid();

        var second = await client.PostAsync("/api/detections/ingest", BuildForm(meta));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDoc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        secondDoc.RootElement.GetProperty("isDuplicate").GetBoolean().Should().BeTrue();
        secondDoc.RootElement.GetProperty("detectionId").GetGuid().Should().Be(firstId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Detections.CountAsync(d => d.PlantId == plantId && d.FrameId == 12345)).Should().Be(1);
    }

    [Fact]
    public async Task Missing_file_returns_400()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var content = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(BuildMetadata("p"), JsonOpts)), "metadata" },
        };

        var resp = await client.PostAsync("/api/detections/ingest", content);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_metadata_returns_400()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "frame.png");

        var resp = await client.PostAsync("/api/detections/ingest", content);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Malformed_metadata_json_returns_400()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var content = new MultipartFormDataContent
        {
            { new StringContent("{not-json"), "metadata" },
        };
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "frame.png");

        var resp = await client.PostAsync("/api/detections/ingest", content);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Metadata_with_empty_predictions_returns_422()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var meta = BuildMetadata("plant-empty-pred");
        var badMeta = meta with { Predictions = Array.Empty<MetaPrediction>() };

        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(badMeta));

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Disallowed_mime_returns_422()
    {
        var (key, _) = await IssueKeyAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var content = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(BuildMetadata("plant-pdf"), JsonOpts)), "metadata" },
        };
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "frame.pdf");

        var resp = await client.PostAsync("/api/detections/ingest", content);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Revoked_key_returns_401()
    {
        var (key, ownerId) = await IssueKeyAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var keys = await svc.ListAsync(ownerId, includeRevoked: false, default);
            await svc.RevokeAsync(keys.Single().Id, default);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata("plant-revoked")));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Owner_lost_agriculture_role_returns_401()
    {
        var (key, ownerId) = await IssueKeyAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AgriCure.Infrastructure.Identity.ApplicationUser>>();
            var user = await userManager.FindByIdAsync(ownerId.ToString());
            user.Should().NotBeNull();
            await userManager.RemoveFromRoleAsync(user!, AgriCure.Infrastructure.Identity.ApplicationRole.Agriculture);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthorization.HeaderName, key);

        var resp = await client.PostAsync("/api/detections/ingest", BuildForm(BuildMetadata("plant-norole")));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string PlaintextKey, Guid OwnerUserId)> IssueKeyAsync()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var admin = await CreateAdminUserIdAsync(scope.ServiceProvider);
        var created = await svc.IssueAsync(agriculture.UserId, $"device-{Guid.NewGuid():N}", admin, default);
        return (created.PlaintextKey, agriculture.UserId);
    }

    private static async Task<Guid> CreateAdminUserIdAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<AgriCure.Infrastructure.Identity.ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<AgriCure.Infrastructure.Identity.ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(AgriCure.Infrastructure.Identity.ApplicationRole.Admin))
        {
            await roleManager.CreateAsync(new AgriCure.Infrastructure.Identity.ApplicationRole
            {
                Name = AgriCure.Infrastructure.Identity.ApplicationRole.Admin,
            });
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

    private static MultipartFormDataContent BuildForm(MetaPayload metadata, string mime = "image/png")
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
        content.Add(fileContent, "file", "frame.png");

        var metaJson = JsonSerializer.Serialize(metadata, JsonOpts);
        content.Add(new StringContent(metaJson, Encoding.UTF8, "application/json"), "metadata");

        return content;
    }

    private static MetaPayload BuildMetadata(string plantId, long frameId = 1) =>
        new(
            FrameId: frameId,
            Timestamp: DateTimeOffset.UtcNow,
            Severity: "warning",
            Predictions: new[]
            {
                new MetaPrediction(DiseaseClass: "early_blight", Confidence: 0.92, Label: "Early Blight"),
                new MetaPrediction(DiseaseClass: "healthy", Confidence: 0.06, Label: "Healthy"),
            },
            BoundingBox: new MetaBoundingBox(0.3, 0.45, 0.2, 0.25, 0.5, 12.5),
            InferenceMs: 32,
            ConfidenceGatePassed: true,
            Row: 7,
            PlantId: plantId,
            PositionMeters: 12.4);

    private sealed record MetaPayload(
        long FrameId,
        DateTimeOffset Timestamp,
        string Severity,
        IReadOnlyList<MetaPrediction> Predictions,
        MetaBoundingBox BoundingBox,
        int InferenceMs,
        bool ConfidenceGatePassed,
        int Row,
        string PlantId,
        double PositionMeters);

    private sealed record MetaPrediction(string DiseaseClass, double Confidence, string Label);

    private sealed record MetaBoundingBox(
        double X, double Y, double Width, double Height, double DepthMeters, double AffectedAreaPercent);
}
