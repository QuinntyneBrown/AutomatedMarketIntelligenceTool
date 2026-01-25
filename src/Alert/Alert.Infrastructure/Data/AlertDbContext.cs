using Alert.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alert.Infrastructure.Data;

public class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options) { }

    public DbSet<Core.Entities.Alert> Alerts => Set<Core.Entities.Alert>();
    public DbSet<AlertNotification> AlertNotifications => Set<AlertNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Core.Entities.Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.WebhookUrl).HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);

            // Configure AlertCriteria as owned type
            entity.OwnsOne(e => e.Criteria, criteria =>
            {
                criteria.Property(c => c.Make).HasMaxLength(100).HasColumnName("Criteria_Make");
                criteria.Property(c => c.Model).HasMaxLength(100).HasColumnName("Criteria_Model");
                criteria.Property(c => c.YearFrom).HasColumnName("Criteria_YearFrom");
                criteria.Property(c => c.YearTo).HasColumnName("Criteria_YearTo");
                criteria.Property(c => c.MinPrice).HasPrecision(18, 2).HasColumnName("Criteria_MinPrice");
                criteria.Property(c => c.MaxPrice).HasPrecision(18, 2).HasColumnName("Criteria_MaxPrice");
                criteria.Property(c => c.MaxMileage).HasColumnName("Criteria_MaxMileage");
                criteria.Property(c => c.Trim).HasMaxLength(100).HasColumnName("Criteria_Trim");
                criteria.Property(c => c.BodyStyle).HasMaxLength(50).HasColumnName("Criteria_BodyStyle");
                criteria.Property(c => c.Transmission).HasMaxLength(50).HasColumnName("Criteria_Transmission");
                criteria.Property(c => c.FuelType).HasMaxLength(50).HasColumnName("Criteria_FuelType");
                criteria.Property(c => c.ExteriorColor).HasMaxLength(50).HasColumnName("Criteria_ExteriorColor");
            });

            // Configure notifications as owned collection
            entity.OwnsMany(e => e.Notifications, notification =>
            {
                notification.ToTable("AlertNotifications");
                notification.WithOwner().HasForeignKey("AlertId");
                notification.HasKey(n => n.Id);
                notification.Property(n => n.MatchedPrice).HasPrecision(18, 2);
                notification.Property(n => n.Message).HasMaxLength(1000);
                notification.HasIndex(n => n.AlertId);
                notification.HasIndex(n => n.VehicleId);
                notification.HasIndex(n => n.CreatedAt);
            });
        });
    }
}
