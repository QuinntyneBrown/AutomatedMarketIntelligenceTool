using Microsoft.EntityFrameworkCore;
using Configuration.Core.Entities;

namespace Configuration.Infrastructure.Data;

public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : base(options) { }

    public DbSet<ServiceConfiguration> ServiceConfigurations => Set<ServiceConfiguration>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<ResourceThrottle> ResourceThrottles => Set<ResourceThrottle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceConfiguration>(entity =>
        {
            entity.ToTable("ServiceConfigurations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.ServiceName, e.Key }).IsUnique();
            entity.HasIndex(e => e.ServiceName);
        });

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ResourceThrottle>(entity =>
        {
            entity.ToTable("ResourceThrottles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MaxConcurrent).IsRequired();
            entity.Property(e => e.RateLimitPerMinute).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired();

            entity.HasIndex(e => e.ResourceName).IsUnique();
        });
    }
}
