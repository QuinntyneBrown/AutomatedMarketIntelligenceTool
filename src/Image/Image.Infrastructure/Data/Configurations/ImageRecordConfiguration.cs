using Image.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Image.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for ImageRecord.
/// </summary>
public sealed class ImageRecordConfiguration : IEntityTypeConfiguration<ImageRecord>
{
    public void Configure(EntityTypeBuilder<ImageRecord> builder)
    {
        builder.ToTable("Images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.Hash)
            .HasMaxLength(64);

        builder.Property(x => x.ContentType)
            .HasMaxLength(100);

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1024);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.Hash);
        builder.HasIndex(x => x.SourceUrl);
        builder.HasIndex(x => x.ListingId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
