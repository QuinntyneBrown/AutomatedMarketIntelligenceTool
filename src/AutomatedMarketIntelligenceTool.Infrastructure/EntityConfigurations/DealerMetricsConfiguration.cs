using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class DealerMetricsConfiguration : IEntityTypeConfiguration<DealerMetrics>
{
    public void Configure(EntityTypeBuilder<DealerMetrics> builder)
    {
        builder.ToTable("DealerMetrics");

        builder.HasKey(m => m.DealerMetricsId);

        builder.Property(m => m.DealerMetricsId)
            .HasConversion(
                id => id.Value,
                value => new DealerMetricsId(value))
            .IsRequired();

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.DealerId)
            .HasConversion(
                id => id.Value,
                value => new DealerId(value))
            .IsRequired();

        // Core reliability metrics
        builder.Property(m => m.ReliabilityScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        // Listing metrics
        builder.Property(m => m.TotalListingsHistorical).IsRequired();
        builder.Property(m => m.ActiveListingsCount).IsRequired();
        builder.Property(m => m.SoldListingsCount).IsRequired();
        builder.Property(m => m.ExpiredListingsCount).IsRequired();

        // Time-based metrics
        builder.Property(m => m.AverageDaysOnMarket).IsRequired();
        builder.Property(m => m.MedianDaysOnMarket).IsRequired();
        builder.Property(m => m.MinDaysOnMarket).IsRequired();
        builder.Property(m => m.MaxDaysOnMarket).IsRequired();

        // Pricing metrics
        builder.Property(m => m.AverageListingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(m => m.AveragePriceReduction)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(m => m.AveragePriceReductionPercent).IsRequired();
        builder.Property(m => m.PriceReductionCount).IsRequired();

        // Relisting metrics
        builder.Property(m => m.RelistingCount).IsRequired();
        builder.Property(m => m.RelistingRate).IsRequired();
        builder.Property(m => m.IsFrequentRelister).IsRequired();

        // Data quality metrics
        builder.Property(m => m.VinProvidedRate).IsRequired();
        builder.Property(m => m.ImageProvidedRate).IsRequired();
        builder.Property(m => m.DescriptionQualityScore).IsRequired();

        // Timestamps
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.LastAnalyzedAt).IsRequired();
        builder.Property(m => m.NextScheduledAnalysis);

        // Indexes
        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_DealerMetrics_TenantId");

        builder.HasIndex(m => m.DealerId)
            .IsUnique()
            .HasDatabaseName("IX_DealerMetrics_DealerId");

        builder.HasIndex(m => new { m.TenantId, m.ReliabilityScore })
            .HasDatabaseName("IX_DealerMetrics_TenantReliability");

        builder.HasIndex(m => new { m.TenantId, m.IsFrequentRelister })
            .HasDatabaseName("IX_DealerMetrics_TenantRelister");

        builder.HasIndex(m => m.LastAnalyzedAt)
            .HasDatabaseName("IX_DealerMetrics_LastAnalyzed");
    }
}
