using Geographic.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Geographic.Infrastructure.Data;

/// <summary>
/// Database context for the Geographic service.
/// </summary>
public sealed class GeographicDbContext : DbContext
{
    public GeographicDbContext(DbContextOptions<GeographicDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomMarket> CustomMarkets => Set<CustomMarket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomMarket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CenterLatitude).IsRequired();
            entity.Property(e => e.CenterLongitude).IsRequired();
            entity.Property(e => e.RadiusKm).IsRequired();
            entity.Property(e => e.PostalCodes).HasMaxLength(4000);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Name);
        });
    }
}
