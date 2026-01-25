namespace Notification.Core.Enums;

/// <summary>
/// Status of a notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is queued and pending delivery.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification was successfully sent.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Notification failed to send.
    /// </summary>
    Failed = 2
}
