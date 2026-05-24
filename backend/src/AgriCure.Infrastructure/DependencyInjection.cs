using System.Text;
using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Common.Storage;
using AgriCure.Infrastructure.Auth;
using AgriCure.Infrastructure.Identity;
using AgriCure.Infrastructure.Persistence;
using AgriCure.Infrastructure.Pictures;
using AgriCure.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minio;

namespace AgriCure.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(DefaultConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{DefaultConnectionStringName}' is required.");

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    AppDbContext.DefaultSchema));
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(opts =>
            {
                opts.Password.RequiredLength = 8;
                opts.Password.RequireDigit = true;
                opts.Password.RequireLowercase = true;
                opts.Password.RequireUppercase = true;
                opts.Password.RequireNonAlphanumeric = false;
                opts.User.RequireUniqueEmail = true;
                opts.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddRoleManager<RoleManager<ApplicationRole>>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .ValidateOnStart();

        services.AddOptions<AdminSeedOptions>()
            .BindConfiguration(AdminSeedOptions.SectionName);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((jwtBearerOpts, jwtOpts) =>
            {
                var jwt = jwtOpts.Value;
                jwtBearerOpts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "Storage:Endpoint is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.AccessKey), "Storage:AccessKey is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey), "Storage:SecretKey is required.")
            .ValidateOnStart();

        services.AddSingleton<IMinioClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var builder = new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey);

            if (!string.IsNullOrWhiteSpace(opts.Region))
            {
                builder = builder.WithRegion(opts.Region);
            }

            if (opts.UseSsl)
            {
                builder = builder.WithSSL();
            }

            return builder.Build();
        });

        services.AddSingleton<IStorageService, MinioStorageService>();
        services.AddHostedService<MinioBucketProvisioner>();

        services.AddScoped<IPictureService, PictureService>();

        return services;
    }
}
