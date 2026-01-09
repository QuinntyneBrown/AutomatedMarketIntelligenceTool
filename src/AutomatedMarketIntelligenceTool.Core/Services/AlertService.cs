using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class AlertService : IAlertService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<AlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Alert> CreateAlertAsync(
        Guid tenantId,
        string name,
        AlertCriteria criteria,
        NotificationMethod notificationMethod = NotificationMethod.Console,
        string? notificationTarget = null,
        CancellationToken cancellationToken = default)
    {
        var alert = Alert.Create(tenantId, name, criteria, notificationMethod, notificationTarget);
        
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created alert {AlertName} for tenant {TenantId}", name, tenantId);
        
        return alert;
    }

    public async Task<Alert> GetAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AlertId == alertId, cancellationToken);

        if (alert == null)
        {
            throw new ArgumentException($"Alert with ID {alertId.Value} not found", nameof(alertId));
        }

        return alert;
    }

    public async Task<List<Alert>> GetAllAlertsAsync(
        Guid tenantId,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Alerts.Where(a => a.TenantId == tenantId);

        if (activeOnly.HasValue)
        {
            query = query.Where(a => a.IsActive == activeOnly.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        AlertCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(tenantId, alertId, cancellationToken);
        
        alert.UpdateCriteria(criteria);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated alert {AlertId}", alertId.Value);
    }

    public async Task ActivateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(tenantId, alertId, cancellationToken);
        
        alert.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated alert {AlertId}", alertId.Value);
    }

    public async Task DeactivateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(tenantId, alertId, cancellationToken);
        
        alert.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated alert {AlertId}", alertId.Value);
    }

    public async Task DeleteAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(tenantId, alertId, cancellationToken);
        
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted alert {AlertId}", alertId.Value);
    }

    public async Task<List<Alert>> CheckAlertsAsync(
        Guid tenantId,
        Listing listing,
        CancellationToken cancellationToken = default)
    {
        var alerts = await GetAllAlertsAsync(tenantId, activeOnly: true, cancellationToken);
        
        var matchingAlerts = alerts.Where(a => a.Matches(listing)).ToList();

        if (matchingAlerts.Any())
        {
            _logger.LogInformation("Found {Count} matching alerts for listing {ListingId}", 
                matchingAlerts.Count, listing.ListingId.Value);
        }

        return matchingAlerts;
    }

    public async Task SendNotificationAsync(
        Alert alert,
        Listing listing,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create notification record
            var notification = AlertNotification.Create(
                alert.TenantId,
                alert.AlertId,
                listing.ListingId,
                NotificationStatus.Pending);

            _context.AlertNotifications.Add(notification);

            // Send notification based on method
            switch (alert.NotificationMethod)
            {
                case NotificationMethod.Console:
                    SendConsoleNotification(alert, listing);
                    notification.MarkAsSent();
                    break;

                case NotificationMethod.Email:
                    // TODO: Implement email notification
                    _logger.LogWarning("Email notifications not yet implemented");
                    notification.MarkAsFailed("Email notifications not implemented");
                    break;

                case NotificationMethod.Webhook:
                    // TODO: Implement webhook notification
                    _logger.LogWarning("Webhook notifications not yet implemented");
                    notification.MarkAsFailed("Webhook notifications not implemented");
                    break;
            }

            // Record trigger
            alert.RecordTrigger();
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Sent notification for alert {AlertId} and listing {ListingId}", 
                alert.AlertId.Value, listing.ListingId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for alert {AlertId}", alert.AlertId.Value);
            throw;
        }
    }

    private void SendConsoleNotification(Alert alert, Listing listing)
    {
        var message = $@"
╔══════════════════════════════════════════════════════════════╗
║                    🔔 ALERT TRIGGERED                        ║
╠══════════════════════════════════════════════════════════════╣
║ Alert: {alert.Name,-52} ║
║ Match: {listing.Year} {listing.Make} {listing.Model,-39} ║
║ Price: ${listing.Price:N2,-50} ║
║ URL:   {listing.ListingUrl,-52} ║
╚══════════════════════════════════════════════════════════════╝
";
        Console.WriteLine(message);
    }
}
