using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class DeduplicationConfigConfiguration : IEntityTypeConfiguration<DeduplicationConfig>
{
    public void Configure(EntityTypeBuilder<DeduplicationConfig> builder)
    {
        builder.ToTable("DeduplicationConfig");

        builder.HasKey(c => c.ConfigId);

        builder.Property(c => c.ConfigId)
            .HasConversion(
                id => id.Value,
                value => new DeduplicationConfigId(value))
            .IsRequired();

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.ConfigKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ConfigValue)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        // Unique constraint on TenantId + ConfigKey
        builder.HasIndex(c => new { c.TenantId, c.ConfigKey })
            .HasDatabaseName("UQ_DedupConfig_Key")
            .IsUnique();

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_DedupConfig_TenantId");
    }
}
