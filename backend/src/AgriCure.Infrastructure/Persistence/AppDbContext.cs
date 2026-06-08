using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using AgriCure.Domain.Pictures;
using AgriCure.Domain.Treatments;
using AgriCure.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    public const string DefaultSchema = "app";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Detection> Detections => Set<Detection>();

    public DbSet<ClassPrediction> Predictions => Set<ClassPrediction>();

    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<Picture> Pictures => Set<Picture>();

    public DbSet<DetectionPicture> DetectionPictures => Set<DetectionPicture>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<Treatment> Treatments => Set<Treatment>();

    public DbSet<AppliedTreatment> AppliedTreatments => Set<AppliedTreatment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
