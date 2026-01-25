using Microsoft.AspNetCore.Mvc;
using Notification.Api.DTOs;
using Notification.Core.Enums;
using Notification.Core.Interfaces;

namespace Notification.Api.Controllers;

/// <summary>
/// API controller for notification operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification using a template.
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationLogResponse>> Send(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = await _notificationService.SendAsync(
                request.RecipientId,
                request.Recipient,
                request.TemplateId,
                request.Placeholders,
                cancellationToken);

            return Ok(MapToLogResponse(log));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Sends a direct notification without a template.
    /// </summary>
    [HttpPost("send-direct")]
    [ProducesResponseType(typeof(NotificationLogResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationLogResponse>> SendDirect(
        [FromBody] SendDirectNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var log = await _notificationService.SendDirectAsync(
            request.RecipientId,
            request.Recipient,
            request.Subject,
            request.Body,
            cancellationToken);

        return Ok(MapToLogResponse(log));
    }

    /// <summary>
    /// Retries a failed notification.
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(NotificationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationLogResponse>> Retry(Guid id, CancellationToken cancellationToken)
    {
        var log = await _notificationService.RetryNotificationAsync(id, cancellationToken);
        if (log == null)
        {
            return NotFound();
        }

        return Ok(MapToLogResponse(log));
    }

    /// <summary>
    /// Gets notification logs.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(IEnumerable<NotificationLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationLogResponse>>> GetLogs(
        [FromQuery] Guid? recipientId,
        [FromQuery] NotificationStatus? status,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var logs = await _notificationService.GetLogsAsync(recipientId, status, limit, cancellationToken);
        return Ok(logs.Select(MapToLogResponse));
    }

    // Template endpoints

    /// <summary>
    /// Gets all notification templates.
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TemplateResponse>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await _notificationService.GetTemplatesAsync(cancellationToken);
        return Ok(templates.Select(MapToTemplateResponse));
    }

    /// <summary>
    /// Gets a notification template by ID.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateResponse>> GetTemplate(Guid id, CancellationToken cancellationToken)
    {
        var template = await _notificationService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return NotFound();
        }

        return Ok(MapToTemplateResponse(template));
    }

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TemplateResponse>> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _notificationService.CreateTemplateAsync(
            request.Name,
            request.Subject,
            request.Body,
            request.Type,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetTemplate),
            new { id = template.Id },
            MapToTemplateResponse(template));
    }

    /// <summary>
    /// Updates a notification template.
    /// </summary>
    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateResponse>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _notificationService.UpdateTemplateAsync(
            id,
            request.Name,
            request.Subject,
            request.Body,
            request.Type,
            request.IsActive,
            cancellationToken);

        if (template == null)
        {
            return NotFound();
        }

        return Ok(MapToTemplateResponse(template));
    }

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    [HttpDelete("templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _notificationService.DeleteTemplateAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Webhook endpoints

    /// <summary>
    /// Gets all webhooks.
    /// </summary>
    [HttpGet("webhooks")]
    [ProducesResponseType(typeof(IEnumerable<WebhookResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WebhookResponse>>> GetWebhooks(CancellationToken cancellationToken)
    {
        var webhooks = await _notificationService.GetWebhooksAsync(cancellationToken);
        return Ok(webhooks.Select(MapToWebhookResponse));
    }

    /// <summary>
    /// Gets a webhook by ID.
    /// </summary>
    [HttpGet("webhooks/{id:guid}")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookResponse>> GetWebhook(Guid id, CancellationToken cancellationToken)
    {
        var webhook = await _notificationService.GetWebhookByIdAsync(id, cancellationToken);
        if (webhook == null)
        {
            return NotFound();
        }

        return Ok(MapToWebhookResponse(webhook));
    }

    /// <summary>
    /// Creates a webhook.
    /// </summary>
    [HttpPost("webhooks")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<WebhookResponse>> CreateWebhook(
        [FromBody] CreateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var webhook = await _notificationService.CreateWebhookAsync(
            request.Url,
            request.Secret,
            request.Events,
            request.Name,
            request.Description,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetWebhook),
            new { id = webhook.Id },
            MapToWebhookResponse(webhook));
    }

    /// <summary>
    /// Updates a webhook.
    /// </summary>
    [HttpPut("webhooks/{id:guid}")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WebhookResponse>> UpdateWebhook(
        Guid id,
        [FromBody] UpdateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var webhook = await _notificationService.UpdateWebhookAsync(
            id,
            request.Url,
            request.Events,
            request.IsActive,
            request.Name,
            request.Description,
            cancellationToken);

        if (webhook == null)
        {
            return NotFound();
        }

        return Ok(MapToWebhookResponse(webhook));
    }

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    [HttpDelete("webhooks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _notificationService.DeleteWebhookAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Triggers webhooks for a specific event type.
    /// </summary>
    [HttpPost("webhooks/trigger")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> TriggerWebhooks(
        [FromBody] TriggerWebhooksRequest request,
        CancellationToken cancellationToken)
    {
        await _notificationService.TriggerWebhooksAsync(request.EventType, request.Payload, cancellationToken);
        return Accepted();
    }

    private static NotificationLogResponse MapToLogResponse(Core.Entities.NotificationLog log)
    {
        return new NotificationLogResponse
        {
            Id = log.Id,
            RecipientId = log.RecipientId,
            TemplateId = log.TemplateId,
            Status = log.Status,
            SentAt = log.SentAt,
            ErrorMessage = log.ErrorMessage,
            Recipient = log.Recipient,
            Subject = log.Subject,
            CreatedAt = log.CreatedAt,
            RetryCount = log.RetryCount
        };
    }

    private static TemplateResponse MapToTemplateResponse(Core.Entities.NotificationTemplate template)
    {
        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            Type = template.Type,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            IsActive = template.IsActive
        };
    }

    private static WebhookResponse MapToWebhookResponse(Core.Entities.Webhook webhook)
    {
        return new WebhookResponse
        {
            Id = webhook.Id,
            Url = webhook.Url,
            Events = webhook.Events,
            IsActive = webhook.IsActive,
            Name = webhook.Name,
            Description = webhook.Description,
            CreatedAt = webhook.CreatedAt,
            UpdatedAt = webhook.UpdatedAt,
            FailureCount = webhook.FailureCount,
            LastTriggeredAt = webhook.LastTriggeredAt
        };
    }
}
