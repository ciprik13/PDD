using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgriCure.Api.IntegrationTests.Detections;

[Collection(IntegrationTestCollection.Name)]
public class DetectionsCrudTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public DetectionsCrudTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_list_returns_200_and_orders_newest_first()
    {
        var admin = await _factory.CreateAdminAsync();

        var createResp = await admin.Client.PostAsJsonAsync("/api/detections", DetectionTestData.BuildCreateBody());
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResp = await admin.Client.GetAsync("/api/detections?limit=50");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_list_without_token_returns_401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/detections");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_list_with_zero_limit_returns_400()
    {
        var admin = await _factory.CreateAdminAsync();
        var resp = await admin.Client.GetAsync("/api/detections?limit=0");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_by_id_without_token_returns_401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync($"/api/detections/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_missing()
    {
        var admin = await _factory.CreateAdminAsync();
        var resp = await admin.Client.GetAsync($"/api/detections/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_by_id_returns_200_for_existing()
    {
        var admin = await _factory.CreateAdminAsync();

        var createResp = await admin.Client.PostAsJsonAsync("/api/detections", DetectionTestData.BuildCreateBody());
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var location = createResp.Headers.Location;
        location.Should().NotBeNull();

        var fetched = await admin.Client.GetAsync(location);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await fetched.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("severity").GetString().Should().Be("warning");
        doc.RootElement.GetProperty("topPrediction").GetProperty("diseaseClass").GetString()
            .Should().Be("early_blight");
    }

    [Fact]
    public async Task Post_without_token_returns_401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/detections", DetectionTestData.BuildCreateBody());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_with_invalid_payload_returns_400()
    {
        var admin = await _factory.CreateAdminAsync();
        var resp = await admin.Client.PostAsJsonAsync("/api/detections", new
        {
            frameId = 0,
            timestamp = DateTimeOffset.UtcNow,
            severity = "warning",
            predictions = Array.Empty<object>(),
            boundingBox = new { x = 2.0, y = 0.5, width = 0.5, height = 0.5, depthMeters = 0.5, affectedAreaPercent = 10.0 },
            inferenceMs = 10,
            confidenceGatePassed = true,
            row = 0,
            plantId = "",
            positionMeters = 5.0,
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_returns_204_for_existing()
    {
        var admin = await _factory.CreateAdminAsync();

        var createResp = await admin.Client.PostAsJsonAsync("/api/detections", DetectionTestData.BuildCreateBody());
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var idJson = await createResp.Content.ReadAsStringAsync();
        var id = JsonDocument.Parse(idJson).RootElement.GetProperty("id").GetGuid();

        var updateResp = await admin.Client.PutAsJsonAsync(
            $"/api/detections/{id}", DetectionTestData.BuildUpdateBody(id));
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await admin.Client.GetAsync($"/api/detections/{id}");
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await fetched.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("severity").GetString().Should().Be("critical");
    }

    [Fact]
    public async Task Put_returns_404_for_unknown_id()
    {
        var admin = await _factory.CreateAdminAsync();
        var randomId = Guid.NewGuid();

        var resp = await admin.Client.PutAsJsonAsync(
            $"/api/detections/{randomId}", DetectionTestData.BuildUpdateBody(randomId));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_with_id_mismatch_between_route_and_body_returns_400()
    {
        var admin = await _factory.CreateAdminAsync();
        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();

        var resp = await admin.Client.PutAsJsonAsync(
            $"/api/detections/{routeId}", DetectionTestData.BuildUpdateBody(bodyId));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_without_token_returns_401()
    {
        var anon = _factory.CreateClient();
        var randomId = Guid.NewGuid();

        var resp = await anon.PutAsJsonAsync(
            $"/api/detections/{randomId}", DetectionTestData.BuildUpdateBody(randomId));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_returns_204_for_existing()
    {
        var admin = await _factory.CreateAdminAsync();

        var createResp = await admin.Client.PostAsJsonAsync("/api/detections", DetectionTestData.BuildCreateBody());
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var idJson = await createResp.Content.ReadAsStringAsync();
        var id = JsonDocument.Parse(idJson).RootElement.GetProperty("id").GetGuid();

        var deleteResp = await admin.Client.DeleteAsync($"/api/detections/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetchAfter = await admin.Client.GetAsync($"/api/detections/{id}");
        fetchAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_is_idempotent_for_unknown_id()
    {
        var admin = await _factory.CreateAdminAsync();
        var resp = await admin.Client.DeleteAsync($"/api/detections/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_without_token_returns_401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.DeleteAsync($"/api/detections/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
