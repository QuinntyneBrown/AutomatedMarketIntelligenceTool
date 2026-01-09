using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("ScheduledReports");

        builder.HasKey(r => r.ScheduledReportId);

        builder.Property(r => r.ScheduledReportId)
            .HasConversion(
                id => id.Value,
                value => new ScheduledReportId(value))
            .IsRequired();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.Format)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Schedule)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.ScheduledTime)
            .IsRequired();

        builder.Property(r => r.ScheduledDayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.ScheduledDayOfMonth);

        builder.Property(r => r.SearchCriteriaJson);

        builder.Property(r => r.CustomMarketId);

        builder.Property(r => r.OutputDirectory)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.FilenameTemplate)
            .HasMaxLength(200);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.LastRunAt);
        builder.Property(r => r.NextRunAt);

        builder.Property(r => r.SuccessfulRuns)
            .IsRequired();

        builder.Property(r => r.FailedRuns)
            .IsRequired();

        builder.Property(r => r.LastErrorMessage)
            .HasMaxLength(2000);

        builder.Property(r => r.EmailRecipients)
            .HasMaxLength(1000);

        builder.Property(r => r.IncludeStatistics)
            .IsRequired();

        builder.Property(r => r.IncludePriceTrends)
            .IsRequired();

        builder.Property(r => r.MaxListings)
            .IsRequired();

        builder.Property(r => r.RetentionCount)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);

        builder.Property(r => r.UpdatedBy)
            .HasMaxLength(100);

        // Unique constraint on TenantId + Name
        builder.HasIndex(r => new { r.TenantId, r.Name })
            .HasDatabaseName("UQ_ScheduledReport_Name")
            .IsUnique();

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_ScheduledReport_TenantId");

        builder.HasIndex(r => new { r.Status, r.NextRunAt })
            .HasDatabaseName("IX_ScheduledReport_DueReports");
    }
}
