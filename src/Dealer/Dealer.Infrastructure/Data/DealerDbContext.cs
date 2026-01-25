using Microsoft.EntityFrameworkCore;

namespace Dealer.Infrastructure.Data;

public class DealerDbContext : DbContext
{
    public DealerDbContext(DbContextOptions<DealerDbContext> options) : base(options) { }

    public DbSet<Core.Entities.Dealer> Dealers => Set<Core.Entities.Dealer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Core.Entities.Dealer>(entity =>
        {
            entity.ToTable("Dealers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Province).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.ReliabilityScore).HasPrecision(5, 2);

            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.HasIndex(e => e.Province);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.ReliabilityScore);
        });
    }
}
