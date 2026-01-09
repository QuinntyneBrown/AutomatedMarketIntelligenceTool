using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(r => r.ReportId);

        builder.Property(r => r.ReportId)
            .HasConversion(
                id => id.Value,
                value => new ReportId(value))
            .IsRequired();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Format)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.SearchCriteriaJson)
            .HasColumnName("SearchCriteria");

        builder.Property(r => r.FilePath)
            .HasMaxLength(500);

        builder.Property(r => r.FileSize);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CompletedAt);

        builder.Property(r => r.ErrorMessage);

        // Indexes
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_Report_TenantId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Report_Status");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Report_CreatedAt");
    }
}
