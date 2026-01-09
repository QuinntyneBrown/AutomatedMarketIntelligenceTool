using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class RelistingPatternConfiguration : IEntityTypeConfiguration<RelistingPattern>
{
    public void Configure(EntityTypeBuilder<RelistingPattern> builder)
    {
        builder.ToTable("RelistingPatterns");

        builder.HasKey(p => p.RelistingPatternId);

        builder.Property(p => p.RelistingPatternId)
            .HasConversion(
                id => id.Value,
                value => new RelistingPatternId(value))
            .IsRequired();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.CurrentListingId)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(p => p.PreviousListingId)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(p => p.DealerId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? new DealerId(value.Value) : null);

        // Pattern detection details
        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.MatchConfidence)
            .IsRequired();

        builder.Property(p => p.MatchMethod)
            .HasMaxLength(50)
            .IsRequired();

        // Price changes
        builder.Property(p => p.PreviousPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CurrentPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PriceChange)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PriceChangePercent);

        // Timing information
        builder.Property(p => p.PreviousDeactivatedAt).IsRequired();
        builder.Property(p => p.CurrentListedAt).IsRequired();
        builder.Property(p => p.DaysBetweenListings).IsRequired();
        builder.Property(p => p.PreviousDaysOnMarket).IsRequired();

        // Vehicle information
        builder.Property(p => p.Vin)
            .HasMaxLength(17);

        builder.Property(p => p.Make)
            .HasMaxLength(50);

        builder.Property(p => p.Model)
            .HasMaxLength(100);

        builder.Property(p => p.Year);

        // Flags
        builder.Property(p => p.IsSuspiciousPattern)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.SuspiciousReason)
            .HasMaxLength(500);

        // Timestamps
        builder.Property(p => p.DetectedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_RelistingPattern_TenantId");

        builder.HasIndex(p => p.DealerId)
            .HasDatabaseName("IX_RelistingPattern_DealerId");

        builder.HasIndex(p => p.CurrentListingId)
            .HasDatabaseName("IX_RelistingPattern_CurrentListing");

        builder.HasIndex(p => p.PreviousListingId)
            .HasDatabaseName("IX_RelistingPattern_PreviousListing");

        builder.HasIndex(p => new { p.TenantId, p.IsSuspiciousPattern })
            .HasDatabaseName("IX_RelistingPattern_TenantSuspicious");

        builder.HasIndex(p => p.DetectedAt)
            .HasDatabaseName("IX_RelistingPattern_DetectedAt");

        builder.HasIndex(p => p.Type)
            .HasDatabaseName("IX_RelistingPattern_Type");
    }
}
