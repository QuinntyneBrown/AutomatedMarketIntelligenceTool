using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class WatchedListingConfiguration : IEntityTypeConfiguration<WatchedListing>
{
    public void Configure(EntityTypeBuilder<WatchedListing> builder)
    {
        builder.ToTable("WatchedListings");

        builder.HasKey(w => w.WatchedListingId);

        builder.Property(w => w.WatchedListingId)
            .HasConversion(
                id => id.Value,
                value => new WatchedListingId(value))
            .IsRequired();

        builder.Property(w => w.TenantId)
            .IsRequired();

        builder.Property(w => w.ListingId)
            .HasConversion(
                id => id.Value,
                value => new AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.ListingId(value))
            .IsRequired();

        builder.HasOne(w => w.Listing)
            .WithMany()
            .HasForeignKey(w => w.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(w => w.Notes)
            .HasMaxLength(500);

        builder.Property(w => w.NotifyOnPriceChange)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.NotifyOnRemoval)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(w => w.TenantId)
            .HasDatabaseName("IX_WatchedListing_TenantId");

        builder.HasIndex(w => w.ListingId)
            .HasDatabaseName("IX_WatchedListing_ListingId");

        // Unique constraint on TenantId + ListingId
        builder.HasIndex(w => new { w.TenantId, w.ListingId })
            .IsUnique()
            .HasDatabaseName("UQ_WatchedListing_TenantListing");
    }
}
