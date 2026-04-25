using System.Globalization;
using AgriCure.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        "logs/agricure-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddControllers();
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

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

app.MapControllers();
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
