using Identity.Core.Models.UserAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.UserProfileId);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.BusinessName)
            .HasMaxLength(200);

        builder.Property(p => p.StreetAddress)
            .HasMaxLength(500);

        builder.Property(p => p.City)
            .HasMaxLength(100);

        builder.Property(p => p.Province)
            .HasMaxLength(50);

        builder.Property(p => p.PostalCode)
            .HasMaxLength(10);

        builder.Property(p => p.Phone)
            .HasMaxLength(20);

        builder.Property(p => p.HstNumber)
            .HasMaxLength(20);

        builder.Property(p => p.LogoUrl)
            .HasMaxLength(500);
    }
}
