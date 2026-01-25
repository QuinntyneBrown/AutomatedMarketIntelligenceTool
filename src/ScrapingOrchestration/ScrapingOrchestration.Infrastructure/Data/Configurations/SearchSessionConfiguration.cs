using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScrapingOrchestration.Core.Entities;
using ScrapingOrchestration.Core.Enums;
using System.Text.Json;

namespace ScrapingOrchestration.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for SearchSession.
/// </summary>
public sealed class SearchSessionConfiguration : IEntityTypeConfiguration<SearchSession>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<SearchSession> builder)
    {
        builder.ToTable("SearchSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.Parameters)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Core.ValueObjects.SearchParameters>(v, JsonOptions)!
            )
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Sources)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ScrapingSource>>(v, JsonOptions)!
            )
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
