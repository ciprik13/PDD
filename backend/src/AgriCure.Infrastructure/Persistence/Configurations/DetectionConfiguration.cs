using AgriCure.Domain.Detections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class DetectionConfiguration : IEntityTypeConfiguration<Detection>
{
    public void Configure(EntityTypeBuilder<Detection> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.PlantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.OwnsOne(d => d.BoundingBox, bb =>
        {
            bb.Property(b => b.X).HasColumnName("BoundingBoxX");
            bb.Property(b => b.Y).HasColumnName("BoundingBoxY");
            bb.Property(b => b.Width).HasColumnName("BoundingBoxWidth");
            bb.Property(b => b.Height).HasColumnName("BoundingBoxHeight");
            bb.Property(b => b.DepthMeters).HasColumnName("BoundingBoxDepthMeters");
            bb.Property(b => b.AffectedAreaPercent).HasColumnName("BoundingBoxAffectedAreaPercent");
        });

        builder.HasMany(d => d.Predictions)
            .WithOne()
            .HasForeignKey(p => p.DetectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Plant>()
            .WithMany()
            .HasForeignKey(d => d.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Timestamp)
            .IsDescending();

        builder.HasIndex(d => d.Row);

        builder.Navigation(d => d.Predictions).AutoInclude();
    }
}
