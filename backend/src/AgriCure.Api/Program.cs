using System.Globalization;
using AgriCure.Api.Hangfire;
using AgriCure.Application;
using AgriCure.Application.Jobs;
using AgriCure.Infrastructure;
using AgriCure.Infrastructure.Identity;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHangfireInfrastructure();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgriCure API",
        Version = "v1",
        Description = "Plant disease detection API for the AgriCure dashboard.",
    });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT bearer auth. Paste the access token from POST /api/auth/login.",
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer",
            },
        }] = Array.Empty<string>(),
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "AgriCure.Api.xml");
    if (File.Exists(xmlPath))
    {
        opts.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddTransient<DailyDetectionSummaryJob>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["self"])
    .AddNpgSql(
        sp => sp.GetRequiredService<IConfiguration>()
            .GetConnectionString(AgriCure.Infrastructure.DependencyInjection.DefaultConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{AgriCure.Infrastructure.DependencyInjection.DefaultConnectionStringName}' is required."),
        name: "postgres",
        tags: ["ready"])
    .AddHangfire(opts => opts.MinimumAvailableServers = 1, name: "hangfire", tags: ["ready"]);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

await app.Services.SeedIdentityAsync();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AdminRoleDashboardFilter()],
});

RecurringJob.AddOrUpdate<DailyDetectionSummaryJob>(
    "daily-detection-summary",
    job => job.ExecuteAsync(),
    Cron.Daily);

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
