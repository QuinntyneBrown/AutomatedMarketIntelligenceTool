using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class SearchProfileConfiguration : IEntityTypeConfiguration<SearchProfile>
{
    public void Configure(EntityTypeBuilder<SearchProfile> builder)
    {
        builder.ToTable("SearchProfiles");

        builder.HasKey(sp => sp.SearchProfileId);

        builder.Property(sp => sp.SearchProfileId)
            .HasConversion(
                id => id.Value,
                value => SearchProfileId.From(value))
            .IsRequired();

        builder.Property(sp => sp.TenantId)
            .IsRequired();

        builder.Property(sp => sp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .HasMaxLength(500);

        builder.Property(sp => sp.SearchParametersJson)
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt);

        builder.Property(sp => sp.LastUsedAt);

        // Indexes
        builder.HasIndex(sp => sp.TenantId);
        builder.HasIndex(sp => new { sp.TenantId, sp.Name })
            .IsUnique();
    }
}
