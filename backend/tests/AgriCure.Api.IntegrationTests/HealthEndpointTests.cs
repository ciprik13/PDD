using System.Net;
using System.Net.Http.Json;

namespace AgriCure.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class HealthEndpointTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public HealthEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_health_returns_healthy_with_self_check()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().ContainSingle().Which.Name.Should().Be("self");
    }

    [Fact]
    public async Task Get_health_ready_returns_healthy_with_postgres_and_hangfire_checks()
    {
        var client = _factory.CreateClient();

        // Hangfire server registration is async — poll until ready (max ~15s).
        HealthResponse? body = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var response = await client.GetAsync("/health/ready");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
            body = await response.Content.ReadFromJsonAsync<HealthResponse>();
            if (body?.Status == "Healthy") break;
            await Task.Delay(500);
        }

        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().Contain(c => c.Name == "postgres" && c.Status == "Healthy");
        body.Checks.Should().Contain(c => c.Name == "hangfire" && c.Status == "Healthy");
    }

    private sealed record HealthResponse(string Status, HealthCheck[] Checks);

    private sealed record HealthCheck(string Name, string Status, string? Description);
}
