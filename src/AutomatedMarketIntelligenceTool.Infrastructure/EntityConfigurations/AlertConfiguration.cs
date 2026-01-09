using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(a => a.AlertId);

        builder.Property(a => a.AlertId)
            .HasConversion(
                id => id.Value,
                value => new AlertId(value))
            .IsRequired();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.CriteriaJson)
            .HasMaxLength(2000)
            .IsRequired()
            .HasColumnName("Criteria");

        builder.Property(a => a.NotificationMethod)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.NotificationTarget)
            .HasMaxLength(500);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.LastTriggeredAt);

        builder.Property(a => a.TriggerCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_Alert_TenantId");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Alert_IsActive");

        builder.HasIndex(a => new { a.TenantId, a.Name })
            .HasDatabaseName("IX_Alert_TenantName");
    }
}
