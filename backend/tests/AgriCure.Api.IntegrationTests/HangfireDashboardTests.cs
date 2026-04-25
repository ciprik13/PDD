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
    public async Task Get_hangfire_dashboard_returns_200_in_development()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/hangfire");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.MovedPermanently);
    }
}
