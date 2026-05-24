using System.Net;

namespace AgriCure.Api.IntegrationTests.Detections;

[Collection(IntegrationTestCollection.Name)]
public class DetectionsScopingTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public DetectionsScopingTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_list_with_plain_user_role_returns_403()
    {
        var plainUser = await _factory.CreatePlainUserAsync();

        var resp = await plainUser.Client.GetAsync("/api/detections");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
