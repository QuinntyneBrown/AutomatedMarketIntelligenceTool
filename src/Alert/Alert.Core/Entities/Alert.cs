using Alert.Core.Enums;

namespace Alert.Core.Entities;

/// <summary>
/// An alert configuration for monitoring vehicle listings.
/// </summary>
public sealed class Alert
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public AlertCriteria Criteria { get; private set; } = null!;
    public NotificationMethod NotificationMethod { get; private set; }
    public string? WebhookUrl { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastTriggeredAt { get; private set; }
    public int TriggerCount { get; private set; }

    private readonly List<AlertNotification> _notifications = [];
    public IReadOnlyList<AlertNotification> Notifications => _notifications.AsReadOnly();

    private Alert() { }

    public static Alert Create(
        string name,
        Guid userId,
        AlertCriteria criteria,
        NotificationMethod notificationMethod,
        string? email = null,
        string? webhookUrl = null)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Criteria = criteria,
            NotificationMethod = notificationMethod,
            Email = email,
            WebhookUrl = webhookUrl,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string? name = null,
        AlertCriteria? criteria = null,
        NotificationMethod? notificationMethod = null,
        string? email = null,
        string? webhookUrl = null)
    {
        if (name != null) Name = name;
        if (criteria != null) Criteria = criteria;
        if (notificationMethod.HasValue) NotificationMethod = notificationMethod.Value;
        if (email != null) Email = email;
        if (webhookUrl != null) WebhookUrl = webhookUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public AlertNotification RecordTrigger(Guid vehicleId, decimal price, string message)
    {
        var notification = AlertNotification.Create(Id, vehicleId, price, message);
        _notifications.Add(notification);
        LastTriggeredAt = DateTimeOffset.UtcNow;
        TriggerCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
        return notification;
    }

    public bool MatchesVehicle(string make, string model, int year, decimal price, int? mileage = null)
    {
        if (!IsActive) return false;

        if (!string.IsNullOrWhiteSpace(Criteria.Make) &&
            !make.Equals(Criteria.Make, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(Criteria.Model) &&
            !model.Contains(Criteria.Model, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Criteria.YearFrom.HasValue && year < Criteria.YearFrom.Value)
            return false;

        if (Criteria.YearTo.HasValue && year > Criteria.YearTo.Value)
            return false;

        if (Criteria.MaxPrice.HasValue && price > Criteria.MaxPrice.Value)
            return false;

        if (Criteria.MinPrice.HasValue && price < Criteria.MinPrice.Value)
            return false;

        if (mileage.HasValue)
        {
            if (Criteria.MaxMileage.HasValue && mileage > Criteria.MaxMileage.Value)
                return false;
        }

        return true;
    }
}

/// <summary>
/// Criteria for matching vehicles against an alert.
/// </summary>
public sealed record AlertCriteria
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? Trim { get; init; }
    public string? BodyStyle { get; init; }
    public string? Transmission { get; init; }
    public string? FuelType { get; init; }
    public string? ExteriorColor { get; init; }
}

/// <summary>
/// A notification record for when an alert is triggered.
/// </summary>
public sealed class AlertNotification
{
    public Guid Id { get; init; }
    public Guid AlertId { get; init; }
    public Guid VehicleId { get; init; }
    public decimal MatchedPrice { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool WasSent { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SentAt { get; private set; }

    private AlertNotification() { }

    public static AlertNotification Create(Guid alertId, Guid vehicleId, decimal price, string message)
    {
        return new AlertNotification
        {
            Id = Guid.NewGuid(),
            AlertId = alertId,
            VehicleId = vehicleId,
            MatchedPrice = price,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsSent()
    {
        WasSent = true;
        SentAt = DateTimeOffset.UtcNow;
    }
}
