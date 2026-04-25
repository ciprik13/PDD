using AgriCure.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public const string DefaultSchema = "app";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);
    }
}
