using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IAlertService
{
    Task<Alert> CreateAlertAsync(
        Guid tenantId,
        string name,
        AlertCriteria criteria,
        NotificationMethod notificationMethod = NotificationMethod.Console,
        string? notificationTarget = null,
        CancellationToken cancellationToken = default);

    Task<Alert> GetAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default);

    Task<List<Alert>> GetAllAlertsAsync(
        Guid tenantId,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task UpdateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        AlertCriteria criteria,
        CancellationToken cancellationToken = default);

    Task ActivateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default);

    Task DeactivateAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default);

    Task DeleteAlertAsync(
        Guid tenantId,
        AlertId alertId,
        CancellationToken cancellationToken = default);

    Task<List<Alert>> CheckAlertsAsync(
        Guid tenantId,
        Listing listing,
        CancellationToken cancellationToken = default);

    Task SendNotificationAsync(
        Alert alert,
        Listing listing,
        CancellationToken cancellationToken = default);
}
