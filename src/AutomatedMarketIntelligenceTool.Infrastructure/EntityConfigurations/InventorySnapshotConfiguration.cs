using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class InventorySnapshotConfiguration : IEntityTypeConfiguration<InventorySnapshot>
{
    public void Configure(EntityTypeBuilder<InventorySnapshot> builder)
    {
        builder.ToTable("InventorySnapshots");

        builder.HasKey(s => s.InventorySnapshotId);

        builder.Property(s => s.InventorySnapshotId)
            .HasConversion(
                id => id.Value,
                value => new InventorySnapshotId(value))
            .IsRequired();

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.DealerId)
            .HasConversion(
                id => id.Value,
                value => new DealerId(value))
            .IsRequired();

        // Snapshot timing
        builder.Property(s => s.SnapshotDate)
            .IsRequired();

        builder.Property(s => s.PeriodType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Inventory counts
        builder.Property(s => s.TotalListings).IsRequired();
        builder.Property(s => s.NewListingsAdded).IsRequired();
        builder.Property(s => s.ListingsRemoved).IsRequired();
        builder.Property(s => s.ListingsSold).IsRequired();
        builder.Property(s => s.ListingsExpired).IsRequired();
        builder.Property(s => s.ListingsRelisted).IsRequired();

        // Inventory value
        builder.Property(s => s.TotalInventoryValue)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(s => s.AverageListingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(s => s.MedianListingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(s => s.MinListingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(s => s.MaxListingPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Price changes
        builder.Property(s => s.PriceIncreasesCount).IsRequired();
        builder.Property(s => s.PriceDecreasesCount).IsRequired();
        builder.Property(s => s.TotalPriceReduction)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(s => s.AveragePriceChange)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Age distribution
        builder.Property(s => s.ListingsUnder30Days).IsRequired();
        builder.Property(s => s.Listings30To60Days).IsRequired();
        builder.Property(s => s.Listings60To90Days).IsRequired();
        builder.Property(s => s.ListingsOver90Days).IsRequired();
        builder.Property(s => s.AverageDaysOnMarket).IsRequired();

        // Vehicle type distribution (stored as JSON)
        builder.Property(s => s.CountByMake)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.CountByYear)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<int, int>())
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.CountByCondition)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
            .HasColumnType("nvarchar(max)");

        // Trends
        builder.Property(s => s.InventoryChangeFromPrevious);
        builder.Property(s => s.InventoryChangePercentFromPrevious);
        builder.Property(s => s.ValueChangeFromPrevious)
            .HasColumnType("decimal(18,2)");
        builder.Property(s => s.ValueChangePercentFromPrevious);

        // Metadata
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("IX_InventorySnapshot_TenantId");

        builder.HasIndex(s => s.DealerId)
            .HasDatabaseName("IX_InventorySnapshot_DealerId");

        builder.HasIndex(s => new { s.TenantId, s.DealerId, s.SnapshotDate })
            .HasDatabaseName("IX_InventorySnapshot_TenantDealerDate");

        builder.HasIndex(s => new { s.TenantId, s.PeriodType, s.SnapshotDate })
            .HasDatabaseName("IX_InventorySnapshot_TenantPeriodDate");

        builder.HasIndex(s => s.SnapshotDate)
            .HasDatabaseName("IX_InventorySnapshot_Date");

        // Unique constraint: one snapshot per dealer per day per period type
        builder.HasIndex(s => new { s.TenantId, s.DealerId, s.SnapshotDate, s.PeriodType })
            .IsUnique()
            .HasDatabaseName("UX_InventorySnapshot_DealerDatePeriod");
    }
}
