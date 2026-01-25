using Alert.Core.Entities;
using Alert.Core.Enums;

namespace Alert.Core.Interfaces;

public interface IAlertService
{
    /// <summary>
    /// Gets an alert by its identifier.
    /// </summary>
    Task<Entities.Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all alerts for a specific user.
    /// </summary>
    Task<IReadOnlyList<Entities.Alert>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active alerts.
    /// </summary>
    Task<IReadOnlyList<Entities.Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new alert.
    /// </summary>
    Task<Entities.Alert> CreateAsync(CreateAlertData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing alert.
    /// </summary>
    Task<Entities.Alert> UpdateAsync(Guid id, UpdateAlertData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an alert.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an alert.
    /// </summary>
    Task<Entities.Alert> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an alert.
    /// </summary>
    Task<Entities.Alert> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks all active alerts against a vehicle and triggers matching alerts.
    /// </summary>
    Task<IReadOnlyList<AlertNotification>> CheckAlertsAsync(
        Guid vehicleId,
        string make,
        string model,
        int year,
        decimal price,
        int? mileage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the notification history for an alert.
    /// </summary>
    Task<IReadOnlyList<AlertNotification>> GetAlertHistoryAsync(
        Guid alertId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}

public sealed record CreateAlertData
{
    public string Name { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public AlertCriteria Criteria { get; init; } = null!;
    public NotificationMethod NotificationMethod { get; init; }
    public string? Email { get; init; }
    public string? WebhookUrl { get; init; }
}

public sealed record UpdateAlertData
{
    public string? Name { get; init; }
    public AlertCriteria? Criteria { get; init; }
    public NotificationMethod? NotificationMethod { get; init; }
    public string? Email { get; init; }
    public string? WebhookUrl { get; init; }
}
