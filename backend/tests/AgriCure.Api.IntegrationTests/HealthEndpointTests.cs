using System.Net;
using System.Net.Http.Json;

namespace AgriCure.Api.IntegrationTests;

public class HealthEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
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
    public async Task Get_health_ready_returns_healthy_with_postgres_check()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().ContainSingle(c => c.Name == "postgres" && c.Status == "Healthy");
    }

    private sealed record HealthResponse(string Status, HealthCheck[] Checks);

    private sealed record HealthCheck(string Name, string Status, string? Description);
}
