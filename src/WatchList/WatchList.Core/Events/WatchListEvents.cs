using Shared.Contracts.Events;

namespace WatchList.Core.Events;

public sealed record ListingWatchedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public Guid ListingId { get; init; }
    public decimal PriceAtWatch { get; init; }
}

public sealed record ListingUnwatchedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public Guid ListingId { get; init; }
}

public sealed record WatchedListingPriceChangedEvent : IntegrationEvent
{
    public Guid WatchedListingId { get; init; }
    public Guid UserId { get; init; }
    public Guid ListingId { get; init; }
    public decimal PreviousPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal PriceChange { get; init; }
}
