using AgriCure.Application.Common.Pictures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
                // IPictureService is replaced by FakePictureService below so no real MinIO is needed.
                ["Storage:Endpoint"] = "localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the real MinIO-backed picture service with an in-process stub
            // that writes the Picture row directly to the DB and skips storage calls.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPictureService));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            services.AddScoped<IPictureService, FakePictureService>();
        });
    }
}
