using Shared.Contracts.Events;

namespace Identity.Core.Models.UserAggregate.Events;

public sealed record UserEmailVerifiedEvent : IntegrationEvent
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset VerifiedAt { get; init; }
}
