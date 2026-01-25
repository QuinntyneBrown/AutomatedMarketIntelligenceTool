using Shared.Contracts.Events;

namespace Notification.Core.Events;

/// <summary>
/// Event raised when a notification has been successfully sent.
/// </summary>
public sealed record NotificationSentEvent : IntegrationEvent
{
    public Guid NotificationId { get; init; }
    public Guid RecipientId { get; init; }
    public Guid TemplateId { get; init; }
    public string? Recipient { get; init; }
    public DateTimeOffset SentAt { get; init; }
}

/// <summary>
/// Event raised when a notification has failed to send.
/// </summary>
public sealed record NotificationFailedEvent : IntegrationEvent
{
    public Guid NotificationId { get; init; }
    public Guid RecipientId { get; init; }
    public Guid TemplateId { get; init; }
    public string? Recipient { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
}

/// <summary>
/// Event raised when a webhook has been triggered.
/// </summary>
public sealed record WebhookTriggeredEvent : IntegrationEvent
{
    public Guid WebhookId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
