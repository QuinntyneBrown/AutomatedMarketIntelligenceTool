using Listing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Listing.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for PriceHistory.
/// </summary>
public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.ListingId);
        builder.HasIndex(x => x.RecordedAt);
    }
}
