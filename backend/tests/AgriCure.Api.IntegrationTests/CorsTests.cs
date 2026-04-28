using System.Net;

namespace AgriCure.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CorsTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public CorsTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preflight_from_allowed_origin_returns_cors_headers()
    {
        var client = _factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle().Which.Should().Be("http://localhost:5173");
    }

    [Fact]
    public async Task Preflight_from_disallowed_origin_omits_cors_headers()
    {
        var client = _factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        preflight.Headers.Add("Origin", "http://evil.example.com");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
