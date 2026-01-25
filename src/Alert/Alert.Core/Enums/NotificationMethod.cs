namespace Alert.Core.Enums;

/// <summary>
/// Methods for sending alert notifications.
/// </summary>
public enum NotificationMethod
{
    /// <summary>
    /// Send notification via email.
    /// </summary>
    Email = 0,

    /// <summary>
    /// Send push notification to mobile device.
    /// </summary>
    Push = 1,

    /// <summary>
    /// Send notification via webhook.
    /// </summary>
    Webhook = 2
}
