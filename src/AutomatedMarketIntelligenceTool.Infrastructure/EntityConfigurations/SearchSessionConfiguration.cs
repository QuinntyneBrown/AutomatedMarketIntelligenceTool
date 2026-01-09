using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatedMarketIntelligenceTool.Infrastructure.EntityConfigurations;

public class SearchSessionConfiguration : IEntityTypeConfiguration<SearchSession>
{
    public void Configure(EntityTypeBuilder<SearchSession> builder)
    {
        builder.ToTable("SearchSessions");

        builder.HasKey(ss => ss.SearchSessionId);

        builder.Property(ss => ss.SearchSessionId)
            .HasConversion(
                id => id.Value,
                value => new SearchSessionId(value))
            .IsRequired();

        builder.Property(ss => ss.TenantId)
            .IsRequired();

        builder.Property(ss => ss.StartTime)
            .IsRequired();

        builder.Property(ss => ss.EndTime);

        builder.Property(ss => ss.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ss => ss.SearchParameters)
            .IsRequired();

        builder.Property(ss => ss.TotalListingsFound)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ss => ss.NewListingsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ss => ss.PriceChangesCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ss => ss.ErrorMessage);

        builder.Property(ss => ss.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ss => ss.TenantId)
            .HasDatabaseName("IX_SearchSession_TenantId");

        builder.HasIndex(ss => ss.StartTime)
            .HasDatabaseName("IX_SearchSession_StartTime");
    }
}
