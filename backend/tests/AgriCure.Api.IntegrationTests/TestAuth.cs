using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AgriCure.Api.IntegrationTests;

internal static class TestAuth
{
    /// <summary>
    /// Registers a fresh user and returns an authenticated <see cref="HttpClient"/>.
    /// The default <c>Authorization</c> header is set; use it directly for protected calls.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this IntegrationTestWebAppFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd!ABC";

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password });
        resp.EnsureSuccessStatusCode();

        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private sealed record AuthResponse(string AccessToken);
}
