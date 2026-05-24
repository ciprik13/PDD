using System.Net;
using System.Net.Http.Json;

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

    [Fact]
    public async Task Post_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        var resp = await agriculture.Client.PostAsJsonAsync(
            "/api/detections",
            DetectionTestData.BuildCreateBody());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();
        var id = Guid.NewGuid();

        var resp = await agriculture.Client.PutAsJsonAsync(
            $"/api/detections/{id}",
            DetectionTestData.BuildUpdateBody(id));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_with_agriculture_role_returns_403()
    {
        var agriculture = await _factory.CreateAgricultureAsync();

        var resp = await agriculture.Client.DeleteAsync($"/api/detections/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
