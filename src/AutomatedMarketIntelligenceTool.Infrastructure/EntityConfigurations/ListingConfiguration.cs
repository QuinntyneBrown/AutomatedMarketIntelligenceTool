using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");

        builder.HasKey(l => l.ListingId);

        builder.Property(l => l.ListingId)
            .HasConversion(
                id => id.Value,
                value => new ListingId(value))
            .IsRequired();

        builder.Property(l => l.TenantId)
            .IsRequired();

        builder.Property(l => l.ExternalId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.SourceSite)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.ListingUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.Make)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Year)
            .IsRequired();

        builder.Property(l => l.Trim)
            .HasMaxLength(100);

        builder.Property(l => l.Price)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(l => l.Mileage);

        builder.Property(l => l.Vin)
            .HasMaxLength(17);

        builder.Property(l => l.City)
            .HasMaxLength(100);

        builder.Property(l => l.Province)
            .HasMaxLength(2);

        builder.Property(l => l.PostalCode)
            .HasMaxLength(7);

        builder.Property(l => l.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("CAD");

        builder.Property(l => l.Condition)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.Transmission)
            .HasConversion<int?>();

        builder.Property(l => l.FuelType)
            .HasConversion<int?>();

        builder.Property(l => l.BodyStyle)
            .HasConversion<int?>();

        builder.Property(l => l.Drivetrain)
            .HasConversion<int?>();

        builder.Property(l => l.ExteriorColor)
            .HasMaxLength(50);

        builder.Property(l => l.InteriorColor)
            .HasMaxLength(50);

        builder.Property(l => l.SellerType)
            .HasConversion<int?>();

        builder.Property(l => l.SellerName)
            .HasMaxLength(200);

        builder.Property(l => l.SellerPhone)
            .HasMaxLength(20);

        builder.Property(l => l.Description);

        builder.Property(l => l.ImageUrls)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(4000);

        builder.Property(l => l.ListingDate);

        builder.Property(l => l.DaysOnMarket);

        builder.Property(l => l.FirstSeenDate)
            .IsRequired();

        builder.Property(l => l.LastSeenDate)
            .IsRequired();

        builder.Property(l => l.IsNewListing)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.DeactivatedAt);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.UpdatedAt);

        // Ignore domain events - they are not persisted
        builder.Ignore(l => l.DomainEvents);

        // Indexes
        builder.HasIndex(l => l.TenantId)
            .HasDatabaseName("IX_Listing_TenantId");

        builder.HasIndex(l => l.Vin)
            .HasDatabaseName("IX_Listing_Vin");

        builder.HasIndex(l => new { l.Make, l.Model })
            .HasDatabaseName("IX_Listing_MakeModel");

        builder.HasIndex(l => l.Price)
            .HasDatabaseName("IX_Listing_Price");

        builder.HasIndex(l => l.Year)
            .HasDatabaseName("IX_Listing_Year");

        builder.HasIndex(l => l.Condition)
            .HasDatabaseName("IX_Listing_Condition");

        builder.HasIndex(l => l.Transmission)
            .HasDatabaseName("IX_Listing_Transmission");

        builder.HasIndex(l => l.FuelType)
            .HasDatabaseName("IX_Listing_FuelType");

        builder.HasIndex(l => l.BodyStyle)
            .HasDatabaseName("IX_Listing_BodyStyle");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("IX_Listing_IsActive");

        builder.HasIndex(l => l.FirstSeenDate)
            .HasDatabaseName("IX_Listing_FirstSeenDate");

        // Unique constraint on ExternalId + SourceSite + TenantId
        builder.HasIndex(l => new { l.SourceSite, l.ExternalId, l.TenantId })
            .IsUnique()
            .HasDatabaseName("UQ_Listing_External");
    }
}
