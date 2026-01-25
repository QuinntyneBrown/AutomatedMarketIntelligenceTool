using Deduplication.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deduplication.Infrastructure.Data.Configurations;

public class DuplicateMatchConfiguration : IEntityTypeConfiguration<DuplicateMatch>
{
    public void Configure(EntityTypeBuilder<DuplicateMatch> builder)
    {
        builder.ToTable("DuplicateMatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OverallScore)
            .HasPrecision(5, 4);

        builder.Property(x => x.Confidence)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.OwnsOne(x => x.ScoreBreakdown, sb =>
        {
            sb.Property(s => s.TitleScore).HasPrecision(5, 4);
            sb.Property(s => s.VinScore).HasPrecision(5, 4);
            sb.Property(s => s.ImageHashScore).HasPrecision(5, 4);
            sb.Property(s => s.PriceScore).HasPrecision(5, 4);
            sb.Property(s => s.MileageScore).HasPrecision(5, 4);
            sb.Property(s => s.LocationScore).HasPrecision(5, 4);
            sb.Property(s => s.MatchReason).HasMaxLength(500);
        });

        builder.HasIndex(x => x.SourceListingId);
        builder.HasIndex(x => x.TargetListingId);
        builder.HasIndex(x => x.Confidence);
        builder.HasIndex(x => new { x.SourceListingId, x.TargetListingId }).IsUnique();
    }
}
