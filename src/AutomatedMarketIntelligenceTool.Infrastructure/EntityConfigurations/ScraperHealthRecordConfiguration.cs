using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ScraperHealthRecordConfiguration : IEntityTypeConfiguration<ScraperHealthRecord>
{
    public void Configure(EntityTypeBuilder<ScraperHealthRecord> builder)
    {
        builder.ToTable("ScraperHealth");

        builder.HasKey(r => r.ScraperHealthRecordId);

        builder.Property(r => r.ScraperHealthRecordId)
            .HasConversion(
                id => id.Value,
                value => new ScraperHealthRecordId(value))
            .IsRequired();

        builder.Property(r => r.SiteName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.RecordedAt)
            .IsRequired();

        builder.Property(r => r.SuccessRate)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(r => r.ListingsFound)
            .IsRequired();

        builder.Property(r => r.ErrorCount)
            .IsRequired();

        builder.Property(r => r.AverageResponseTime)
            .IsRequired();

        builder.Property(r => r.LastError)
            .HasMaxLength(2000);

        builder.Property(r => r.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.MissingElementCount)
            .IsRequired();

        builder.Property(r => r.MissingElementsJson)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(r => r.SiteName)
            .HasDatabaseName("IX_Health_Site");

        builder.HasIndex(r => r.RecordedAt)
            .HasDatabaseName("IX_Health_RecordedAt");
    }
}
