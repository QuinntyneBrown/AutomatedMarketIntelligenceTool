using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class CustomMarketConfiguration : IEntityTypeConfiguration<CustomMarket>
{
    public void Configure(EntityTypeBuilder<CustomMarket> builder)
    {
        builder.ToTable("CustomMarkets");

        builder.HasKey(m => m.CustomMarketId);

        builder.Property(m => m.CustomMarketId)
            .HasConversion(
                id => id.Value,
                value => new CustomMarketId(value))
            .IsRequired();

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.PostalCodes)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(m => m.Provinces)
            .HasMaxLength(500);

        builder.Property(m => m.CenterLatitude);
        builder.Property(m => m.CenterLongitude);
        builder.Property(m => m.RadiusKm);

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.Priority)
            .IsRequired();

        builder.Property(m => m.ConfigurationJson);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(100);

        builder.Property(m => m.UpdatedBy)
            .HasMaxLength(100);

        // Unique constraint on TenantId + Name
        builder.HasIndex(m => new { m.TenantId, m.Name })
            .HasDatabaseName("UQ_CustomMarket_Name")
            .IsUnique();

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_CustomMarket_TenantId");

        builder.HasIndex(m => new { m.TenantId, m.IsActive })
            .HasDatabaseName("IX_CustomMarket_Active");
    }
}
