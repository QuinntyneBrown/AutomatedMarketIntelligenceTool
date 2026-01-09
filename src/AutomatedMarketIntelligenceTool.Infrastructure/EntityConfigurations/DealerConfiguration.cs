using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class DealerConfiguration : IEntityTypeConfiguration<Dealer>
{
    public void Configure(EntityTypeBuilder<Dealer> builder)
    {
        builder.ToTable("Dealers");

        builder.HasKey(d => d.DealerId);

        builder.Property(d => d.DealerId)
            .HasConversion(
                id => id.Value,
                value => new DealerId(value))
            .IsRequired();

        builder.Property(d => d.TenantId)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.NormalizedName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.City)
            .HasMaxLength(100);

        builder.Property(d => d.State)
            .HasMaxLength(50);

        builder.Property(d => d.Phone)
            .HasMaxLength(20);

        builder.Property(d => d.ListingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.FirstSeenAt)
            .IsRequired();

        builder.Property(d => d.LastSeenAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(d => d.TenantId)
            .HasDatabaseName("IX_Dealer_TenantId");

        builder.HasIndex(d => d.NormalizedName)
            .HasDatabaseName("IX_Dealer_NormalizedName");

        builder.HasIndex(d => new { d.TenantId, d.NormalizedName })
            .HasDatabaseName("IX_Dealer_TenantNormalizedName");
    }
}
