using Alert.Core.Enums;
using Shared.Contracts.Events;

namespace Alert.Core.Events;

/// <summary>
/// Event raised when a new alert is created.
/// </summary>
public sealed record AlertCreatedEvent : IntegrationEvent
{
    public Guid AlertId { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MaxPrice { get; init; }
    public NotificationMethod NotificationMethod { get; init; }
}

/// <summary>
/// Event raised when an alert is triggered by a matching vehicle.
/// </summary>
public sealed record AlertTriggeredEvent : IntegrationEvent
{
    public Guid AlertId { get; init; }
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
    public Guid VehicleId { get; init; }
    public decimal MatchedPrice { get; init; }
    public string Message { get; init; } = string.Empty;
    public NotificationMethod NotificationMethod { get; init; }
    public string? Email { get; init; }
    public string? WebhookUrl { get; init; }
}

/// <summary>
/// Event raised when an alert is updated.
/// </summary>
public sealed record AlertUpdatedEvent : IntegrationEvent
{
    public Guid AlertId { get; init; }
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Event raised when an alert is deleted.
/// </summary>
public sealed record AlertDeletedEvent : IntegrationEvent
{
    public Guid AlertId { get; init; }
    public Guid UserId { get; init; }
}
