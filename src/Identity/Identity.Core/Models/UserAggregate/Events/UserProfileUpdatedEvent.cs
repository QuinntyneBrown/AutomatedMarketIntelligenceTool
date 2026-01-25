using Shared.Contracts.Events;

namespace Identity.Core.Models.UserAggregate.Events;

public sealed record UserProfileUpdatedEvent : IntegrationEvent
{
    public required string UserId { get; init; }
    public string? BusinessName { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
