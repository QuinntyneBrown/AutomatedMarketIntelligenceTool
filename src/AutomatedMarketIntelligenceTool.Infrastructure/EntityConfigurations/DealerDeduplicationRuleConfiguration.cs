using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class DealerDeduplicationRuleConfiguration : IEntityTypeConfiguration<DealerDeduplicationRule>
{
    public void Configure(EntityTypeBuilder<DealerDeduplicationRule> builder)
    {
        builder.ToTable("DealerDeduplicationRules");

        builder.HasKey(r => r.DealerDeduplicationRuleId);

        builder.Property(r => r.DealerDeduplicationRuleId)
            .HasConversion(
                id => id.Value,
                value => new DealerDeduplicationRuleId(value))
            .IsRequired();

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.DealerId)
            .HasConversion(
                id => id.Value,
                value => new DealerId(value))
            .IsRequired();

        // Rule identification
        builder.Property(r => r.RuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        // Matching thresholds
        builder.Property(r => r.AutoMatchThreshold);
        builder.Property(r => r.ReviewThreshold);

        // Field weights
        builder.Property(r => r.MakeModelWeight);
        builder.Property(r => r.YearWeight);
        builder.Property(r => r.MileageWeight);
        builder.Property(r => r.PriceWeight);
        builder.Property(r => r.LocationWeight);
        builder.Property(r => r.ImageWeight);

        // Tolerance settings
        builder.Property(r => r.MileageTolerance);
        builder.Property(r => r.PriceTolerance)
            .HasColumnType("decimal(18,2)");
        builder.Property(r => r.YearTolerance);

        // Feature flags
        builder.Property(r => r.EnableVinMatching);
        builder.Property(r => r.EnableFuzzyMatching);
        builder.Property(r => r.EnableImageMatching);
        builder.Property(r => r.StrictModeEnabled);

        // Conditions
        builder.Property(r => r.Condition)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.MinPrice)
            .HasColumnType("decimal(18,2)");
        builder.Property(r => r.MaxPrice)
            .HasColumnType("decimal(18,2)");
        builder.Property(r => r.MinYear);
        builder.Property(r => r.MaxYear);
        builder.Property(r => r.MakeFilter)
            .HasMaxLength(50);
        builder.Property(r => r.ModelFilter)
            .HasMaxLength(100);

        // Audit
        builder.Property(r => r.CreatedAt)
            .IsRequired();
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);
        builder.Property(r => r.UpdatedBy)
            .HasMaxLength(100);
        builder.Property(r => r.TimesApplied)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(r => r.LastAppliedAt);

        // Indexes
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_DealerDedupRule_TenantId");

        builder.HasIndex(r => r.DealerId)
            .HasDatabaseName("IX_DealerDedupRule_DealerId");

        builder.HasIndex(r => new { r.TenantId, r.DealerId, r.IsActive })
            .HasDatabaseName("IX_DealerDedupRule_TenantDealerActive");

        builder.HasIndex(r => new { r.TenantId, r.Priority })
            .HasDatabaseName("IX_DealerDedupRule_TenantPriority");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_DealerDedupRule_IsActive");
    }
}
