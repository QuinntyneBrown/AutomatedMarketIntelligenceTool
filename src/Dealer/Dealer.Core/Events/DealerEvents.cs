using Shared.Contracts.Events;

namespace Dealer.Core.Events;

public sealed record DealerCreatedEvent : IntegrationEvent
{
    public Guid DealerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Province { get; init; }
}

public sealed record DealerUpdatedEvent : IntegrationEvent
{
    public Guid DealerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public int ListingCount { get; init; }
}

public sealed record ReliabilityScoreChangedEvent : IntegrationEvent
{
    public Guid DealerId { get; init; }
    public decimal PreviousScore { get; init; }
    public decimal NewScore { get; init; }
    public string? Reason { get; init; }
}
