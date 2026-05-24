using AgriCure.Domain.Pictures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class PictureConfiguration : IEntityTypeConfiguration<Picture>
{
    public void Configure(EntityTypeBuilder<Picture> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MimeType)
            .HasMaxLength(127)
            .IsRequired();

        builder.Property(p => p.SeoFilename)
            .HasMaxLength(200);

        builder.Property(p => p.AltAttribute)
            .HasMaxLength(300);

        builder.Property(p => p.TitleAttribute)
            .HasMaxLength(300);

        builder.Property(p => p.VirtualPath)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(p => p.VirtualPath)
            .IsUnique();
    }
}
