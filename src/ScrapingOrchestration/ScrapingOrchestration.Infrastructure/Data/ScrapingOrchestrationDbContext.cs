using Microsoft.EntityFrameworkCore;
using ScrapingOrchestration.Core.Entities;

namespace ScrapingOrchestration.Infrastructure.Data;

/// <summary>
/// Database context for the Scraping Orchestration service.
/// </summary>
public sealed class ScrapingOrchestrationDbContext : DbContext
{
    public ScrapingOrchestrationDbContext(DbContextOptions<ScrapingOrchestrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SearchSession> Sessions => Set<SearchSession>();
    public DbSet<ScrapingJob> Jobs => Set<ScrapingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScrapingOrchestrationDbContext).Assembly);
    }
}
