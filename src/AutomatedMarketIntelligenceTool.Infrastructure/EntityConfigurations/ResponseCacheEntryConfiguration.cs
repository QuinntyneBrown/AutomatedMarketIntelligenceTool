using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ResponseCacheEntryConfiguration : IEntityTypeConfiguration<ResponseCacheEntry>
{
    public void Configure(EntityTypeBuilder<ResponseCacheEntry> builder)
    {
        builder.ToTable("ResponseCache");

        builder.HasKey(c => c.CacheEntryId);

        builder.Property(c => c.CacheEntryId)
            .HasConversion(
                id => id.Value,
                value => ResponseCacheEntryId.FromGuid(value))
            .IsRequired();

        builder.Property(c => c.CacheKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Url)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.ResponseData)
            .IsRequired();

        builder.Property(c => c.ContentType)
            .HasMaxLength(100);

        builder.Property(c => c.CachedAt)
            .IsRequired();

        builder.Property(c => c.ExpiresAt)
            .IsRequired();

        builder.Property(c => c.HitCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.ResponseSizeBytes)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.CacheKey)
            .IsUnique()
            .HasDatabaseName("UQ_ResponseCache_CacheKey");

        builder.HasIndex(c => c.ExpiresAt)
            .HasDatabaseName("IX_ResponseCache_ExpiresAt");

        builder.HasIndex(c => c.Url)
            .HasDatabaseName("IX_ResponseCache_Url");
    }
}
