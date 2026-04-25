using AgriCure.Domain.Detections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class ClassPredictionConfiguration : IEntityTypeConfiguration<ClassPrediction>
{
    public void Configure(EntityTypeBuilder<ClassPrediction> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Label)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(p => new { p.DetectionId, p.Rank });
    }
}
