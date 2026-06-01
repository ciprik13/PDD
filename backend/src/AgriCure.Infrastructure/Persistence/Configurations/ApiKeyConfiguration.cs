using AgriCure.Domain.Identity;
using AgriCure.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCure.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(k => k.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(k => k.TokenLast4)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(k => k.Scope)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(k => k.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(k => k.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(k => k.TokenHash).IsUnique();
        builder.HasIndex(k => k.OwnerUserId);

        builder.Ignore(k => k.IsActive);
    }
}
