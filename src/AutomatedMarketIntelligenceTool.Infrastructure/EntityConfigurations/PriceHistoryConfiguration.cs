using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistory");

        builder.HasKey(ph => ph.PriceHistoryId);

        builder.Property(ph => ph.PriceHistoryId)
            .HasConversion(
                id => id.Value,
                value => new PriceHistoryId(value))
            .IsRequired();

        builder.Property(ph => ph.TenantId)
            .IsRequired();

        builder.Property(ph => ph.ListingId)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(ph => ph.Price)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(ph => ph.ObservedAt)
            .IsRequired();

        builder.Property(ph => ph.PriceChange)
            .HasPrecision(12, 2);

        builder.Property(ph => ph.ChangePercentage)
            .HasPrecision(5, 2);

        builder.Property(ph => ph.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ph => ph.TenantId)
            .HasDatabaseName("IX_PriceHistory_TenantId");

        builder.HasIndex(ph => ph.ListingId)
            .HasDatabaseName("IX_PriceHistory_ListingId");
    }
}
