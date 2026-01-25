using ScrapingOrchestration.Core.Enums;
using Shared.Contracts.Events;

namespace ScrapingOrchestration.Core.Events;

/// <summary>
/// Event raised when a listing is scraped.
/// </summary>
public sealed record ListingScrapedEvent : IntegrationEvent
{
    public Guid JobId { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public string SourceListingId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int? Mileage { get; init; }
    public string? VIN { get; init; }
    public string? DealerName { get; init; }
    public string? Location { get; init; }
    public string? ListingUrl { get; init; }
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public DateTimeOffset ScrapedAt { get; init; }
}
