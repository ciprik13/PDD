using AgriCure.Application.Common.Interfaces;
using AgriCure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
