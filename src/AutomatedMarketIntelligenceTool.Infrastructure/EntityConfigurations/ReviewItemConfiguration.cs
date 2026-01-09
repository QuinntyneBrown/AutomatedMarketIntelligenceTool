using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable("ReviewQueue");

        builder.HasKey(r => r.ReviewItemId);

        builder.Property(r => r.ReviewItemId)
            .HasConversion(
                id => id.Value,
                value => new ReviewItemId(value))
            .IsRequired();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.Listing1Id)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(r => r.Listing2Id)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(r => r.ConfidenceScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(r => r.MatchMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.FieldScores)
            .HasMaxLength(2000);

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ReviewItemStatus.Pending);

        builder.Property(r => r.Resolution)
            .HasConversion<int>()
            .HasDefaultValue(ResolutionDecision.None);

        builder.Property(r => r.ResolvedAt);

        builder.Property(r => r.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(r => r.Notes)
            .HasMaxLength(2000);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(r => r.DomainEvents);

        // Indexes
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_Review_TenantId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Review_Status");

        builder.HasIndex(r => new { r.Listing1Id, r.Listing2Id })
            .HasDatabaseName("IX_Review_Listings");
    }
}
