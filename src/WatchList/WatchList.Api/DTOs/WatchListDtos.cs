namespace WatchList.Api.DTOs;

public sealed record WatchedListingResponse(
    Guid Id,
    Guid UserId,
    Guid ListingId,
    string? Notes,
    decimal PriceAtWatch,
    decimal CurrentPrice,
    decimal PriceChange,
    DateTimeOffset AddedAt,
    DateTimeOffset LastCheckedAt);

public sealed record AddToWatchListRequest(
    Guid UserId,
    Guid ListingId,
    decimal CurrentPrice,
    string? Notes);

public sealed record UpdateNotesRequest(
    string? Notes);

public sealed record PriceChangeResponse(
    Guid Id,
    Guid ListingId,
    decimal PriceAtWatch,
    decimal CurrentPrice,
    decimal PriceChange,
    decimal PercentChange,
    DateTimeOffset AddedAt);
