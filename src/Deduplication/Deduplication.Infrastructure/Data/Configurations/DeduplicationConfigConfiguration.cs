using Deduplication.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deduplication.Infrastructure.Data.Configurations;

public class DeduplicationConfigConfiguration : IEntityTypeConfiguration<DeduplicationConfig>
{
    public void Configure(EntityTypeBuilder<DeduplicationConfig> builder)
    {
        builder.ToTable("DeduplicationConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TitleSimilarityThreshold)
            .HasPrecision(5, 4);

        builder.Property(x => x.ImageHashSimilarityThreshold)
            .HasPrecision(5, 4);

        builder.Property(x => x.OverallMatchThreshold)
            .HasPrecision(5, 4);

        builder.Property(x => x.ReviewThreshold)
            .HasPrecision(5, 4);

        builder.Property(x => x.TitleWeight)
            .HasPrecision(5, 4);

        builder.Property(x => x.VinWeight)
            .HasPrecision(5, 4);

        builder.Property(x => x.ImageHashWeight)
            .HasPrecision(5, 4);

        builder.Property(x => x.PriceWeight)
            .HasPrecision(5, 4);

        builder.Property(x => x.MileageWeight)
            .HasPrecision(5, 4);

        builder.Property(x => x.LocationWeight)
            .HasPrecision(5, 4);

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
