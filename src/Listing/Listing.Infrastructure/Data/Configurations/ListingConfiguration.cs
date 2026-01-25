using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Listing.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Listing.
/// </summary>
public sealed class ListingConfiguration : IEntityTypeConfiguration<Core.Entities.Listing>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Core.Entities.Listing> builder)
    {
        builder.ToTable("Listings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceListingId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.Make).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.VIN).HasMaxLength(20);
        builder.Property(x => x.ExteriorColor).HasMaxLength(50);
        builder.Property(x => x.InteriorColor).HasMaxLength(50);
        builder.Property(x => x.Engine).HasMaxLength(100);
        builder.Property(x => x.DealerName).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Province).HasMaxLength(50);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.ListingUrl).HasMaxLength(2048);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PrimaryImageHash).HasMaxLength(64);

        builder.Property(x => x.Condition).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.BodyStyle).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Transmission).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.FuelType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Drivetrain).HasConversion<string>().HasMaxLength(50);

        builder.Property(x => x.ImageUrls)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions)!
            )
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.Source, x.SourceListingId }).IsUnique();
        builder.HasIndex(x => x.VIN);
        builder.HasIndex(x => x.Make);
        builder.HasIndex(x => x.Model);
        builder.HasIndex(x => x.Year);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.Province);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.FirstSeenAt);
        builder.HasIndex(x => x.LastSeenAt);
        builder.HasIndex(x => x.DealerId);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.PrimaryImageHash);

        builder.HasMany(x => x.PriceHistory)
            .WithOne()
            .HasForeignKey(x => x.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.PriceHistory).AutoInclude(false);
    }
}
