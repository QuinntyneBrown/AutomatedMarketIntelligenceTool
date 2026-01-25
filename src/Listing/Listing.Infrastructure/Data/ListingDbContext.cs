using Listing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Listing.Infrastructure.Data;

/// <summary>
/// Database context for the Listing service.
/// </summary>
public sealed class ListingDbContext : DbContext
{
    public ListingDbContext(DbContextOptions<ListingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Core.Entities.Listing> Listings => Set<Core.Entities.Listing>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ListingDbContext).Assembly);
    }
}
