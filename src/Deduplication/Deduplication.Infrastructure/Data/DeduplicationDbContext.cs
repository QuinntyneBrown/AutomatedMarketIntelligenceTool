using Deduplication.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Deduplication.Infrastructure.Data;

public class DeduplicationDbContext : DbContext
{
    public DeduplicationDbContext(DbContextOptions<DeduplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeduplicationConfig> Configs => Set<DeduplicationConfig>();
    public DbSet<DuplicateMatch> DuplicateMatches => Set<DuplicateMatch>();
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeduplicationDbContext).Assembly);
    }
}
