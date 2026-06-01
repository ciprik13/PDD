using AgriCure.Domain.Detections;
using AgriCure.Domain.Pictures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class DetectionPictureConfiguration : IEntityTypeConfiguration<DetectionPicture>
{
    public void Configure(EntityTypeBuilder<DetectionPicture> builder)
    {
        builder.HasKey(dp => new { dp.DetectionId, dp.PictureId });

        builder.HasIndex(dp => dp.DetectionId)
            .IsUnique();

        builder.HasOne<Detection>()
            .WithMany()
            .HasForeignKey(dp => dp.DetectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Picture>()
            .WithMany()
            .HasForeignKey(dp => dp.PictureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
