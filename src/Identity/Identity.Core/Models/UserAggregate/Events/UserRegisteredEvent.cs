using Shared.Contracts.Events;

namespace Identity.Core.Models.UserAggregate.Events;

public sealed record UserRegisteredEvent : IntegrationEvent
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
}
