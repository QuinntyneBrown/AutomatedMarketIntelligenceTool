using Deduplication.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deduplication.Infrastructure.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Operation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.ListingId);
        builder.HasIndex(x => x.DuplicateMatchId);
        builder.HasIndex(x => x.ReviewItemId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Operation);
    }
}
