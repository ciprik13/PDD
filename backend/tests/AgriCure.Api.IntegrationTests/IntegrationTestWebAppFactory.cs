using AgriCure.Api.IntegrationTests.TestSurfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AgriCure.Api.IntegrationTests;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _postgres.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "agricure-tests",
                ["Jwt:Audience"] = "agricure-tests",
                ["Jwt:SigningKey"] = "test-only-signing-key-must-be-long-enough-for-hmacsha256",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                // Storage settings — required because StorageOptions ValidateOnStart.
                // Tests don't actually upload anything but the validator runs at startup.
                ["Storage:Endpoint"] = "localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(ApiKeyProbeController).Assembly);
        });
    }
}
