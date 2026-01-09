using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.VehicleId);

        builder.Property(v => v.VehicleId)
            .HasConversion(
                id => id.Value,
                value => new VehicleId(value))
            .IsRequired();

        builder.Property(v => v.TenantId)
            .IsRequired();

        builder.Property(v => v.PrimaryVin)
            .HasMaxLength(17);

        builder.Property(v => v.Make)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Year)
            .IsRequired();

        builder.Property(v => v.Trim)
            .HasMaxLength(100);

        builder.Property(v => v.BestPrice)
            .HasColumnType("decimal(12,2)");

        builder.Property(v => v.AveragePrice)
            .HasColumnType("decimal(12,2)");

        builder.Property(v => v.ListingCount)
            .IsRequired();

        builder.Property(v => v.FirstSeenDate)
            .IsRequired();

        builder.Property(v => v.LastSeenDate)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt);

        // Indexes
        builder.HasIndex(v => v.TenantId);
        builder.HasIndex(v => v.PrimaryVin);
        builder.HasIndex(v => new { v.Make, v.Model, v.Year });

        // Ignore domain events if any
        builder.Ignore("DomainEvents");
    }
}
