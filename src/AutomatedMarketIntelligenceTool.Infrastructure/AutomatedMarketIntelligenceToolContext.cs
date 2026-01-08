using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Infrastructure;

public class AutomatedMarketIntelligenceToolContext : DbContext, IAutomatedMarketIntelligenceToolContext
{
    private readonly Guid _tenantId;

    public DbSet<Listing> Listings => Set<Listing>();

    public AutomatedMarketIntelligenceToolContext(DbContextOptions<AutomatedMarketIntelligenceToolContext> options)
        : base(options)
    {
        // TODO: Inject ITenantContext to get TenantId
        // For now, using a default value
        _tenantId = Guid.Empty;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomatedMarketIntelligenceToolContext).Assembly);

        // Apply global query filter for multi-tenancy
        modelBuilder.Entity<Listing>().HasQueryFilter(l => l.TenantId == _tenantId);
    }
}
