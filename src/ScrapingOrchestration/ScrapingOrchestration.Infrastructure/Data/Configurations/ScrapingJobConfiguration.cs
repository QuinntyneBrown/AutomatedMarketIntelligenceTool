using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrapingOrchestration.Core.Entities;
using System.Text.Json;

namespace ScrapingOrchestration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for ScrapingJob.
/// </summary>
public sealed class ScrapingJobConfiguration : IEntityTypeConfiguration<ScrapingJob>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<ScrapingJob> builder)
    {
        builder.ToTable("ScrapingJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.WorkerId)
            .HasMaxLength(100);

        builder.Property(x => x.Parameters)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Core.ValueObjects.SearchParameters>(v, JsonOptions)!
            )
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.CreatedAt);
    }
}
