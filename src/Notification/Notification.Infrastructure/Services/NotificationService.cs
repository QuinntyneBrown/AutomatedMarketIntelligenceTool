using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Core.Entities;
using Notification.Core.Enums;
using Notification.Core.Events;
using Notification.Core.Interfaces;
using Notification.Infrastructure.Data;
using Shared.Messaging;

namespace Notification.Infrastructure.Services;

/// <summary>
/// Implementation of the notification service.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly NotificationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IEventPublisher eventPublisher,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<NotificationLog> SendAsync(
        Guid recipientId,
        string recipient,
        Guid templateId,
        Dictionary<string, string>? placeholders = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.Templates.FindAsync([templateId], cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template with ID {templateId} not found");
        }

        var subject = ApplyPlaceholders(template.Subject, placeholders);
        var body = ApplyPlaceholders(template.Body, placeholders);

        return await SendDirectAsync(recipientId, recipient, subject, body, templateId, cancellationToken);
    }

    public async Task<NotificationLog> SendDirectAsync(
        Guid recipientId,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        return await SendDirectAsync(recipientId, recipient, subject, body, Guid.Empty, cancellationToken);
    }

    private async Task<NotificationLog> SendDirectAsync(
        Guid recipientId,
        string recipient,
        string subject,
        string body,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var log = NotificationLog.Create(recipientId, templateId, recipient, subject, body);

        try
        {
            // In a real implementation, this would send via email, SMS, push notification, etc.
            // For now, we simulate a successful send
            _logger.LogInformation("Sending notification to {Recipient}: {Subject}", recipient, subject);

            log.MarkAsSent();

            _dbContext.Logs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new NotificationSentEvent
            {
                NotificationId = log.Id,
                RecipientId = recipientId,
                TemplateId = templateId,
                Recipient = recipient,
                SentAt = log.SentAt!.Value
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Recipient}", recipient);

            log.MarkAsFailed(ex.Message);

            _dbContext.Logs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new NotificationFailedEvent
            {
                NotificationId = log.Id,
                RecipientId = recipientId,
                TemplateId = templateId,
                Recipient = recipient,
                ErrorMessage = ex.Message,
                RetryCount = log.RetryCount
            }, cancellationToken);
        }

        return log;
    }

    public async Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Templates
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Templates.FindAsync([templateId], cancellationToken);
    }

    public async Task<NotificationTemplate> CreateTemplateAsync(
        string name,
        string subject,
        string body,
        string type,
        CancellationToken cancellationToken = default)
    {
        var template = NotificationTemplate.Create(name, subject, body, type);

        _dbContext.Templates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created notification template {TemplateName} with ID {TemplateId}", name, template.Id);

        return template;
    }

    public async Task<NotificationTemplate?> UpdateTemplateAsync(
        Guid templateId,
        string name,
        string subject,
        string body,
        string type,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.Templates.FindAsync([templateId], cancellationToken);
        if (template == null)
        {
            return null;
        }

        template.Update(name, subject, body, type, isActive);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notification template {TemplateId}", templateId);

        return template;
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.Templates.FindAsync([templateId], cancellationToken);
        if (template == null)
        {
            return false;
        }

        _dbContext.Templates.Remove(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted notification template {TemplateId}", templateId);

        return true;
    }

    public async Task<Webhook> CreateWebhookAsync(
        string url,
        string secret,
        string events,
        string? name = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var webhook = Webhook.Create(url, secret, events, name, description);

        _dbContext.Webhooks.Add(webhook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created webhook {WebhookId} for URL {Url}", webhook.Id, url);

        return webhook;
    }

    public async Task<IEnumerable<Webhook>> GetWebhooksAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Webhooks
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Webhook?> GetWebhookByIdAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Webhooks.FindAsync([webhookId], cancellationToken);
    }

    public async Task<Webhook?> UpdateWebhookAsync(
        Guid webhookId,
        string url,
        string events,
        bool isActive,
        string? name = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var webhook = await _dbContext.Webhooks.FindAsync([webhookId], cancellationToken);
        if (webhook == null)
        {
            return null;
        }

        webhook.Update(url, events, isActive, name, description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated webhook {WebhookId}", webhookId);

        return webhook;
    }

    public async Task<bool> DeleteWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        var webhook = await _dbContext.Webhooks.FindAsync([webhookId], cancellationToken);
        if (webhook == null)
        {
            return false;
        }

        _dbContext.Webhooks.Remove(webhook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted webhook {WebhookId}", webhookId);

        return true;
    }

    public async Task TriggerWebhooksAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var webhooks = await _dbContext.Webhooks
            .Where(w => w.IsActive && w.Events.Contains(eventType))
            .ToListAsync(cancellationToken);

        foreach (var webhook in webhooks)
        {
            await TriggerWebhookAsync(webhook, eventType, payload, cancellationToken);
        }
    }

    private async Task TriggerWebhookAsync(Webhook webhook, string eventType, object payload, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Webhooks");
        var success = false;
        string? errorMessage = null;

        try
        {
            var json = JsonSerializer.Serialize(new
            {
                eventType,
                timestamp = DateTimeOffset.UtcNow,
                payload
            });

            var signature = ComputeSignature(json, webhook.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", eventType);

            var response = await client.SendAsync(request, cancellationToken);
            success = response.IsSuccessStatusCode;

            if (!success)
            {
                errorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                webhook.RecordFailure();
            }
            else
            {
                webhook.RecordSuccess();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger webhook {WebhookId} for event {EventType}", webhook.Id, eventType);
            errorMessage = ex.Message;

            webhook.RecordFailure();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _eventPublisher.PublishAsync(new WebhookTriggeredEvent
        {
            WebhookId = webhook.Id,
            Url = webhook.Url,
            EventType = eventType,
            Success = success,
            ErrorMessage = errorMessage
        }, cancellationToken);
    }

    public async Task<IEnumerable<NotificationLog>> GetLogsAsync(
        Guid? recipientId = null,
        NotificationStatus? status = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Logs.AsQueryable();

        if (recipientId.HasValue)
        {
            query = query.Where(l => l.RecipientId == recipientId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        query = query.OrderByDescending(l => l.CreatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<NotificationLog?> RetryNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.Logs.FindAsync([notificationId], cancellationToken);
        if (log == null || log.Status != NotificationStatus.Failed)
        {
            return null;
        }

        // Create a new log entry for the retry
        var newLog = NotificationLog.Create(
            log.RecipientId,
            log.TemplateId,
            log.Recipient,
            log.Subject,
            log.Body,
            log.RetryCount + 1);

        try
        {
            _logger.LogInformation("Retrying notification {NotificationId} to {Recipient}", notificationId, log.Recipient);

            newLog.MarkAsSent();

            _dbContext.Logs.Add(newLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new NotificationSentEvent
            {
                NotificationId = newLog.Id,
                RecipientId = newLog.RecipientId,
                TemplateId = newLog.TemplateId,
                Recipient = newLog.Recipient,
                SentAt = newLog.SentAt!.Value
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry notification {NotificationId}", notificationId);

            newLog.MarkAsFailed(ex.Message);

            _dbContext.Logs.Add(newLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(new NotificationFailedEvent
            {
                NotificationId = newLog.Id,
                RecipientId = newLog.RecipientId,
                TemplateId = newLog.TemplateId,
                Recipient = newLog.Recipient,
                ErrorMessage = ex.Message,
                RetryCount = newLog.RetryCount
            }, cancellationToken);
        }

        return newLog;
    }

    private static string ApplyPlaceholders(string template, Dictionary<string, string>? placeholders)
    {
        if (placeholders == null || placeholders.Count == 0)
        {
            return template;
        }

        var result = template;
        foreach (var (key, value) in placeholders)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }

        return result;
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
