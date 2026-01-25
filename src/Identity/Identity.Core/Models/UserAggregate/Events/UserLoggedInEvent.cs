using Shared.Contracts.Events;

namespace Identity.Core.Models.UserAggregate.Events;

public sealed record UserLoggedInEvent : IntegrationEvent
{
    public required string UserId { get; init; }
    public required DateTimeOffset LoggedInAt { get; init; }
}
