using Image.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Image.Infrastructure.Data;

/// <summary>
/// Database context for the Image service.
/// </summary>
public sealed class ImageDbContext : DbContext
{
    public ImageDbContext(DbContextOptions<ImageDbContext> options) : base(options)
    {
    }

    public DbSet<ImageRecord> Images => Set<ImageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImageDbContext).Assembly);
    }
}
