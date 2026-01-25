namespace Identity.Core.Models.ApiKeyAggregate.Events;

/// <summary>
/// Event raised when a new API key is generated.
/// </summary>
public sealed record ApiKeyGeneratedEvent(
    Guid ApiKeyId,
    Guid UserId,
    string Name,
    DateTimeOffset CreatedAt);
