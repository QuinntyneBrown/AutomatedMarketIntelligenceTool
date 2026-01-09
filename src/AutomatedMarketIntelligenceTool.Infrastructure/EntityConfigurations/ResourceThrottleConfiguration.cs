using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ResourceThrottleConfiguration : IEntityTypeConfiguration<ResourceThrottle>
{
    public void Configure(EntityTypeBuilder<ResourceThrottle> builder)
    {
        builder.ToTable("ResourceThrottles");

        builder.HasKey(t => t.ResourceThrottleId);

        builder.Property(t => t.ResourceThrottleId)
            .HasConversion(
                id => id.Value,
                value => new ResourceThrottleId(value))
            .IsRequired();

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.ResourceType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.MaxValue)
            .IsRequired();

        builder.Property(t => t.TimeWindow)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CurrentUsage)
            .IsRequired();

        builder.Property(t => t.WindowStartTime)
            .IsRequired();

        builder.Property(t => t.IsEnabled)
            .IsRequired();

        builder.Property(t => t.Action)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.WarningThresholdPercent)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(100);

        // Unique constraint on TenantId + ResourceType
        builder.HasIndex(t => new { t.TenantId, t.ResourceType })
            .HasDatabaseName("UQ_ResourceThrottle_Type")
            .IsUnique();

        builder.HasIndex(t => t.TenantId)
            .HasDatabaseName("IX_ResourceThrottle_TenantId");

        builder.HasIndex(t => new { t.TenantId, t.IsEnabled })
            .HasDatabaseName("IX_ResourceThrottle_Enabled");
    }
}
