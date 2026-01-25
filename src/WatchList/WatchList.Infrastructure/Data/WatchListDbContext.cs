using Microsoft.EntityFrameworkCore;
using WatchList.Core.Entities;

namespace WatchList.Infrastructure.Data;

public class WatchListDbContext : DbContext
{
    public WatchListDbContext(DbContextOptions<WatchListDbContext> options) : base(options) { }

    public DbSet<WatchedListing> WatchedListings => Set<WatchedListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WatchedListing>(entity =>
        {
            entity.ToTable("WatchedListings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PriceAtWatch).HasPrecision(18, 2);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 2);
            entity.Property(e => e.PriceChange).HasPrecision(18, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ListingId);
            entity.HasIndex(e => new { e.UserId, e.ListingId }).IsUnique();
        });
    }
}
