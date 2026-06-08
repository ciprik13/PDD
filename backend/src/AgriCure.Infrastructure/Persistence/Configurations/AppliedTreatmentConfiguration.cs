using AgriCure.Domain.Detections;
using AgriCure.Domain.Treatments;
using AgriCure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class AppliedTreatmentConfiguration : IEntityTypeConfiguration<AppliedTreatment>
{
    public void Configure(EntityTypeBuilder<AppliedTreatment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PlantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasOne<Treatment>()
            .WithMany()
            .HasForeignKey(a => a.TreatmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Plant>()
            .WithMany()
            .HasForeignKey(a => a.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.AppliedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PlantId);
        builder.HasIndex(a => a.AppliedAt).IsDescending();
    }
}
