using Notification.Core.Entities;
using Notification.Core.Enums;

namespace Notification.Core.Interfaces;

/// <summary>
/// Service for sending and managing notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a recipient using the specified template.
    /// </summary>
    Task<NotificationLog> SendAsync(
        Guid recipientId,
        string recipient,
        Guid templateId,
        Dictionary<string, string>? placeholders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a recipient without a template.
    /// </summary>
    Task<NotificationLog> SendDirectAsync(
        Guid recipientId,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notification templates.
    /// </summary>
    Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification template by ID.
    /// </summary>
    Task<NotificationTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    Task<NotificationTemplate> CreateTemplateAsync(
        string name,
        string subject,
        string body,
        string type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification template.
    /// </summary>
    Task<NotificationTemplate?> UpdateTemplateAsync(
        Guid templateId,
        string name,
        string subject,
        string body,
        string type,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    Task<Webhook> CreateWebhookAsync(
        string url,
        string secret,
        string events,
        string? name = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all webhooks.
    /// </summary>
    Task<IEnumerable<Webhook>> GetWebhooksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a webhook by ID.
    /// </summary>
    Task<Webhook?> GetWebhookByIdAsync(Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a webhook.
    /// </summary>
    Task<Webhook?> UpdateWebhookAsync(
        Guid webhookId,
        string url,
        string events,
        bool isActive,
        string? name = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers webhooks for a specific event.
    /// </summary>
    Task TriggerWebhooksAsync(string eventType, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification logs with optional filtering.
    /// </summary>
    Task<IEnumerable<NotificationLog>> GetLogsAsync(
        Guid? recipientId = null,
        NotificationStatus? status = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed notification.
    /// </summary>
    Task<NotificationLog?> RetryNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
