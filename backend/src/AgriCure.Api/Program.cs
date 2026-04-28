using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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

const string CorsPolicyName = "AgriCureFrontend";

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

builder.Services.AddProblemDetails();

// Browser-origin allowlist for the React/Vite dashboard.
// Empty list = no cross-origin requests are accepted (safe default for prod
// until operators populate Cors:AllowedOrigins via env / appsettings).
// Bearer tokens travel in the Authorization header, so AllowCredentials is
// intentionally not set — that lets us keep the simple WithOrigins() check
// instead of pinning to a specific scheme+host triple.
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?.Where(o => !string.IsNullOrWhiteSpace(o))
        .ToArray() ?? [];

    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    });

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

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

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// Swagger UI is open in Development; outside Development it requires the admin role.
// Browsers don't auto-send the bearer header — production admins need a tool that
// can attach `Authorization: Bearer <token>` (e.g. a browser extension or curl).
if (!app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/swagger") &&
            !ctx.User.IsInRole(AgriCure.Infrastructure.Identity.ApplicationRole.Admin))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Authentication failed.",
                status = StatusCodes.Status401Unauthorized,
                detail = "Swagger UI requires the admin role outside Development.",
            });
            return;
        }
        await next();
    });
}

app.UseSwagger();
app.UseSwaggerUI();

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
