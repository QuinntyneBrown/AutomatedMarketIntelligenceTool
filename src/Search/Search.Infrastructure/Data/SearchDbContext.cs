using Microsoft.EntityFrameworkCore;
using Search.Core.Entities;

namespace Search.Infrastructure.Data;

public class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options) { }

    public DbSet<SearchProfile> SearchProfiles => Set<SearchProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SearchProfile>(entity =>
        {
            entity.ToTable("SearchProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Name);

            entity.OwnsOne(e => e.Criteria, criteria =>
            {
                criteria.Property(c => c.Make).HasMaxLength(100).HasColumnName("Criteria_Make");
                criteria.Property(c => c.Model).HasMaxLength(100).HasColumnName("Criteria_Model");
                criteria.Property(c => c.YearFrom).HasColumnName("Criteria_YearFrom");
                criteria.Property(c => c.YearTo).HasColumnName("Criteria_YearTo");
                criteria.Property(c => c.PriceFrom).HasPrecision(18, 2).HasColumnName("Criteria_PriceFrom");
                criteria.Property(c => c.PriceTo).HasPrecision(18, 2).HasColumnName("Criteria_PriceTo");
                criteria.Property(c => c.MileageFrom).HasColumnName("Criteria_MileageFrom");
                criteria.Property(c => c.MileageTo).HasColumnName("Criteria_MileageTo");
                criteria.Property(c => c.BodyStyle).HasMaxLength(50).HasColumnName("Criteria_BodyStyle");
                criteria.Property(c => c.Transmission).HasMaxLength(50).HasColumnName("Criteria_Transmission");
                criteria.Property(c => c.Drivetrain).HasMaxLength(50).HasColumnName("Criteria_Drivetrain");
                criteria.Property(c => c.FuelType).HasMaxLength(50).HasColumnName("Criteria_FuelType");
                criteria.Property(c => c.ExteriorColor).HasMaxLength(50).HasColumnName("Criteria_ExteriorColor");
                criteria.Property(c => c.Province).HasMaxLength(50).HasColumnName("Criteria_Province");
                criteria.Property(c => c.City).HasMaxLength(100).HasColumnName("Criteria_City");
                criteria.Property(c => c.RadiusKm).HasColumnName("Criteria_RadiusKm");
                criteria.Property(c => c.Keywords).HasMaxLength(500).HasColumnName("Criteria_Keywords");
                criteria.Property(c => c.CertifiedPreOwned).HasColumnName("Criteria_CertifiedPreOwned");
                criteria.Property(c => c.DealerOnly).HasColumnName("Criteria_DealerOnly");
                criteria.Property(c => c.PrivateSellerOnly).HasColumnName("Criteria_PrivateSellerOnly");
            });
        });
    }
}
