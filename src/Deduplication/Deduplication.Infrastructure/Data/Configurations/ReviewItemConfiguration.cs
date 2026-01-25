using Deduplication.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deduplication.Infrastructure.Data.Configurations;

public class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable("ReviewItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchScore)
            .HasPrecision(5, 4);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ReviewNotes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.SourceListingId);
        builder.HasIndex(x => x.TargetListingId);
        builder.HasIndex(x => x.DuplicateMatchId);
    }
}
