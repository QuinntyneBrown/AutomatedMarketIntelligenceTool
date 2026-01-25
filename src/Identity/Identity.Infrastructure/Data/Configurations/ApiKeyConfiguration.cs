using Identity.Core.Models.ApiKeyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(a => a.ApiKeyId);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.KeyPrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Scopes)
            .HasMaxLength(1000);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.KeyPrefix);
    }
}
