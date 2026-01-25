namespace Shared.Contracts.DTOs;

/// <summary>
/// Data transfer object for aggregated vehicle information.
/// </summary>
public record VehicleDto
{
    public Guid VehicleId { get; init; }
    public string? PrimaryVin { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Trim { get; init; }
    public decimal? BestPrice { get; init; }
    public decimal? AveragePrice { get; init; }
    public int ListingCount { get; init; }
    public DateTime FirstSeenDate { get; init; }
    public DateTime LastSeenDate { get; init; }
    public List<Guid> LinkedListingIds { get; init; } = new();
}
