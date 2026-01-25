using Shared.Contracts.Events;

namespace VehicleAggregation.Core.Events;

public sealed record VehicleCreatedEvent : IntegrationEvent
{
    public Guid VehicleId { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? VIN { get; init; }
}

public sealed record VehicleUpdatedEvent : IntegrationEvent
{
    public Guid VehicleId { get; init; }
    public int ListingCount { get; init; }
    public decimal? BestPrice { get; init; }
}

public sealed record BestPriceChangedEvent : IntegrationEvent
{
    public Guid VehicleId { get; init; }
    public decimal? PreviousBestPrice { get; init; }
    public decimal NewBestPrice { get; init; }
    public Guid BestPriceListingId { get; init; }
}

public sealed record ListingLinkedToVehicleEvent : IntegrationEvent
{
    public Guid VehicleId { get; init; }
    public Guid ListingId { get; init; }
    public decimal Price { get; init; }
}
