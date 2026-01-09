using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

public class AlertNotification
{
    public Guid NotificationId { get; private set; }
    public Guid TenantId { get; private set; }
    public AlertId AlertId { get; private set; } = null!;
    public Alert Alert { get; private set; } = null!;
    public ListingId ListingId { get; private set; } = null!;
    public Listing Listing { get; private set; } = null!;
    public DateTime SentAt { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AlertNotification() { }

    public static AlertNotification Create(
        Guid tenantId,
        AlertId alertId,
        ListingId listingId,
        NotificationStatus status = NotificationStatus.Sent)
    {
        return new AlertNotification
        {
            NotificationId = Guid.NewGuid(),
            TenantId = tenantId,
            AlertId = alertId,
            ListingId = listingId,
            SentAt = DateTime.UtcNow,
            Status = status
        };
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
