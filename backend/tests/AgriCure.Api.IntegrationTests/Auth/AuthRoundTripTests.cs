using System.Net;
using System.Net.Http.Json;

namespace AgriCure.Api.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public class AuthRoundTripTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AuthRoundTripTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_login_refresh_logout_full_cycle()
    {
        var client = _factory.CreateClient();
        var email = $"roundtrip-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd!ABC";

        // 1. Register
        var registerResp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password });
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        registered!.AccessToken.Should().NotBeNullOrEmpty();
        registered.RefreshToken.Should().NotBeNullOrEmpty();

        // 2. Login with the same credentials produces a fresh token pair.
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        loggedIn!.RefreshToken.Should().NotBe(registered.RefreshToken);

        // 3. Refresh rotates both tokens.
        var refreshResp = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = loggedIn.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refreshResp.Content.ReadFromJsonAsync<AuthResponse>();
        rotated!.RefreshToken.Should().NotBe(loggedIn.RefreshToken);

        // 4. The old refresh token is now revoked — re-using it must fail.
        var reuseResp = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = loggedIn.RefreshToken });
        reuseResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 5. Logout revokes the active refresh token.
        var logoutResp = await client.PostAsJsonAsync("/api/auth/logout",
            new { RefreshToken = rotated.RefreshToken });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. Refresh after logout must fail.
        var afterLogoutResp = await client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = rotated.RefreshToken });
        afterLogoutResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@example.com", Password = "Whatever1!" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "weakpw@example.com", Password = "short" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record AuthResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);
}
