namespace Shared.Contracts.DTOs;

/// <summary>
/// Data transfer object for dealer information.
/// </summary>
public record DealerDto
{
    public Guid DealerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public int ActiveListingCount { get; init; }
    public int TotalListingCount { get; init; }
    public decimal? ReliabilityScore { get; init; }
    public DateTime FirstSeenDate { get; init; }
    public DateTime LastSeenDate { get; init; }
}
