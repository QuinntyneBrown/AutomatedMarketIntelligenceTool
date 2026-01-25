using Notification.Core.Enums;

namespace Notification.Api.DTOs;

/// <summary>
/// Request to send a notification using a template.
/// </summary>
public sealed record SendNotificationRequest
{
    public required Guid RecipientId { get; init; }
    public required string Recipient { get; init; }
    public required Guid TemplateId { get; init; }
    public Dictionary<string, string>? Placeholders { get; init; }
}

/// <summary>
/// Request to send a direct notification without a template.
/// </summary>
public sealed record SendDirectNotificationRequest
{
    public required Guid RecipientId { get; init; }
    public required string Recipient { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
}

/// <summary>
/// Request to create a notification template.
/// </summary>
public sealed record CreateTemplateRequest
{
    public required string Name { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required string Type { get; init; }
}

/// <summary>
/// Request to update a notification template.
/// </summary>
public sealed record UpdateTemplateRequest
{
    public required string Name { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required string Type { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Response for a notification template.
/// </summary>
public sealed record TemplateResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required string Type { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Request to create a webhook.
/// </summary>
public sealed record CreateWebhookRequest
{
    public required string Url { get; init; }
    public required string Secret { get; init; }
    public required string Events { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Request to update a webhook.
/// </summary>
public sealed record UpdateWebhookRequest
{
    public required string Url { get; init; }
    public required string Events { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Name { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Response for a webhook.
/// </summary>
public sealed record WebhookResponse
{
    public Guid Id { get; init; }
    public required string Url { get; init; }
    public required string Events { get; init; }
    public bool IsActive { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int FailureCount { get; init; }
    public DateTimeOffset? LastTriggeredAt { get; init; }
}

/// <summary>
/// Response for a notification log entry.
/// </summary>
public sealed record NotificationLogResponse
{
    public Guid Id { get; init; }
    public Guid RecipientId { get; init; }
    public Guid TemplateId { get; init; }
    public NotificationStatus Status { get; init; }
    public DateTimeOffset? SentAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Recipient { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int RetryCount { get; init; }
}

/// <summary>
/// Request to trigger webhooks for an event.
/// </summary>
public sealed record TriggerWebhooksRequest
{
    public required string EventType { get; init; }
    public required object Payload { get; init; }
}
