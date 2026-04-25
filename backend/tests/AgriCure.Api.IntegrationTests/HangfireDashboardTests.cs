using System.Net;

namespace AgriCure.Api.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class HangfireDashboardTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public HangfireDashboardTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_hangfire_dashboard_returns_401_when_anonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/hangfire");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
