using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCure.Infrastructure;

public static class HangfireConfiguration
{
    public const string SchemaName = "hangfire";

    public static IServiceCollection AddHangfireInfrastructure(this IServiceCollection services)
    {
        services.AddHangfire((sp, cfg) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString(DependencyInjection.DefaultConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{DependencyInjection.DefaultConnectionStringName}' is required.");

            cfg.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    options => options.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions { SchemaName = SchemaName });
        });

        services.AddHangfireServer();

        return services;
    }
}
