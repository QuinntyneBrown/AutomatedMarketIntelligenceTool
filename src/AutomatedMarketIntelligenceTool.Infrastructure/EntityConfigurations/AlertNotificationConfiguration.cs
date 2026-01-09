using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class AlertNotificationConfiguration : IEntityTypeConfiguration<AlertNotification>
{
    public void Configure(EntityTypeBuilder<AlertNotification> builder)
    {
        builder.ToTable("AlertNotifications");

        builder.HasKey(an => an.NotificationId);

        builder.Property(an => an.NotificationId)
            .IsRequired();

        builder.Property(an => an.TenantId)
            .IsRequired();

        builder.Property(an => an.AlertId)
            .HasConversion(
                id => id.Value,
                value => new AlertId(value))
            .IsRequired();

        builder.HasOne(an => an.Alert)
            .WithMany()
            .HasForeignKey(an => an.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(an => an.ListingId)
            .HasConversion(
                id => id.Value,
                value => new AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.ListingId(value))
            .IsRequired();

        builder.HasOne(an => an.Listing)
            .WithMany()
            .HasForeignKey(an => an.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(an => an.SentAt)
            .IsRequired();

        builder.Property(an => an.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(an => an.ErrorMessage)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(an => an.TenantId)
            .HasDatabaseName("IX_AlertNotification_TenantId");

        builder.HasIndex(an => an.AlertId)
            .HasDatabaseName("IX_AlertNotification_AlertId");

        builder.HasIndex(an => an.SentAt)
            .HasDatabaseName("IX_AlertNotification_SentAt");
    }
}
