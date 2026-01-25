using Microsoft.EntityFrameworkCore;
using VehicleAggregation.Core.Entities;

namespace VehicleAggregation.Infrastructure.Data;

public class VehicleDbContext : DbContext
{
    public VehicleDbContext(DbContextOptions<VehicleDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VIN).HasMaxLength(17);
            entity.Property(e => e.Make).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Trim).HasMaxLength(100);
            entity.Property(e => e.BodyStyle).HasMaxLength(50);
            entity.Property(e => e.Transmission).HasMaxLength(50);
            entity.Property(e => e.Drivetrain).HasMaxLength(50);
            entity.Property(e => e.FuelType).HasMaxLength(50);
            entity.Property(e => e.ExteriorColor).HasMaxLength(50);
            entity.Property(e => e.InteriorColor).HasMaxLength(50);
            entity.Property(e => e.BestPrice).HasPrecision(18, 2);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 2);
            entity.Property(e => e.LowestPrice).HasPrecision(18, 2);
            entity.Property(e => e.HighestPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.VIN);
            entity.HasIndex(e => new { e.Make, e.Model, e.Year });

            entity.OwnsMany(e => e.Listings, listing =>
            {
                listing.ToTable("VehicleListings");
                listing.WithOwner().HasForeignKey("VehicleId");
                listing.HasKey(l => l.Id);
                listing.Property(l => l.Source).HasMaxLength(100);
                listing.Property(l => l.DealerName).HasMaxLength(200);
                listing.Property(l => l.Price).HasPrecision(18, 2);
                listing.HasIndex(l => l.ListingId);
            });
        });
    }
}
