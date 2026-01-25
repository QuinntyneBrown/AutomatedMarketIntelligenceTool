using Notification.Core.Enums;

namespace Notification.Core.Entities;

/// <summary>
/// Represents a log entry for a sent or attempted notification.
/// </summary>
public sealed class NotificationLog
{
    public Guid Id { get; init; }
    public Guid RecipientId { get; init; }
    public Guid TemplateId { get; init; }
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Recipient { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int RetryCount { get; init; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid recipientId,
        Guid templateId,
        string? recipient,
        string? subject,
        string? body,
        int retryCount = 0)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            TemplateId = templateId,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = retryCount
        };
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
