using AgriCure.Domain.Detections;
using AgriCure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasMaxLength(64);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OwnerUserId);
    }
}
