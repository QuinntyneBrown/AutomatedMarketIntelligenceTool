using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("DeduplicationAudit");

        builder.HasKey(a => a.AuditEntryId);

        builder.Property(a => a.AuditEntryId)
            .HasConversion(
                id => id.Value,
                value => new AuditEntryId(value))
            .IsRequired();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.Listing1Id)
            .IsRequired();

        builder.Property(a => a.Listing2Id);

        builder.Property(a => a.Decision)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Reason)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ConfidenceScore)
            .HasPrecision(5, 2);

        builder.Property(a => a.WasAutomatic)
            .IsRequired();

        builder.Property(a => a.ManualOverride)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.OverrideReason)
            .HasMaxLength(500);

        builder.Property(a => a.OriginalAuditEntryId);

        builder.Property(a => a.IsFalsePositive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsFalseNegative)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.FuzzyMatchDetailsJson)
            .HasColumnName("FuzzyMatchDetails");

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100);

        // Indexes for common queries
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_Audit_TenantId");

        builder.HasIndex(a => a.Decision)
            .HasDatabaseName("IX_Audit_Decision");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_Audit_CreatedAt");

        builder.HasIndex(a => a.Listing1Id)
            .HasDatabaseName("IX_Audit_Listing1Id");

        builder.HasIndex(a => new { a.TenantId, a.CreatedAt })
            .HasDatabaseName("IX_Audit_TenantId_CreatedAt");

        // Index for false positive/negative tracking
        builder.HasIndex(a => new { a.TenantId, a.IsFalsePositive, a.IsFalseNegative })
            .HasDatabaseName("IX_Audit_FalsePositiveNegative");
    }
}
