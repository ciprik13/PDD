using AgriCure.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["self"])
    .AddNpgSql(
        sp => sp.GetRequiredService<IConfiguration>()
            .GetConnectionString(DependencyInjection.DefaultConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DependencyInjection.DefaultConnectionStringName}' is required."),
        name: "postgres",
        tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

app.MapGet("/", () => "AgriCure API up");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("self"),
    ResponseWriter = WriteHealthResponse,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse,
});

app.Run();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
        }),
    };
    return context.Response.WriteAsJsonAsync(payload);
}

/// <summary>
/// Public partial Program declaration so WebApplicationFactory&lt;Program&gt; can target it
/// from integration tests.
/// </summary>
public partial class Program;
