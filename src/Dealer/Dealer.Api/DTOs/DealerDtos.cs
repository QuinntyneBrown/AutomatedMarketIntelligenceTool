namespace Dealer.Api.DTOs;

public sealed record DealerResponse(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Website,
    string? Phone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode,
    decimal ReliabilityScore,
    int ListingCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateDealerRequest(
    string Name,
    string? Website,
    string? Phone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record UpdateDealerRequest(
    string? Name,
    string? Website,
    string? Phone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode);

public sealed record UpdateReliabilityScoreRequest(
    decimal Score,
    string? Reason);

public sealed record DealerSearchRequest(
    string? Name,
    string? City,
    string? Province,
    decimal? MinReliabilityScore,
    int? MinListingCount,
    int Skip = 0,
    int Take = 50);

public sealed record DealerReliabilityResponse(
    Guid DealerId,
    string Name,
    decimal ReliabilityScore,
    int ListingCount,
    string? City,
    string? Province);

public sealed record DealerInventoryResponse(
    Guid DealerId,
    string Name,
    int ListingCount,
    string? City,
    string? Province);
