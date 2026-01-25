using Alert.Core.Enums;

namespace Alert.Api.DTOs;

public sealed record AlertResponse(
    Guid Id,
    string Name,
    Guid UserId,
    AlertCriteriaResponse Criteria,
    NotificationMethod NotificationMethod,
    string? Email,
    string? WebhookUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastTriggeredAt,
    int TriggerCount);

public sealed record AlertCriteriaResponse(
    string? Make,
    string? Model,
    int? YearFrom,
    int? YearTo,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MaxMileage,
    string? Trim,
    string? BodyStyle,
    string? Transmission,
    string? FuelType,
    string? ExteriorColor);

public sealed record AlertNotificationResponse(
    Guid Id,
    Guid AlertId,
    Guid VehicleId,
    decimal MatchedPrice,
    string Message,
    bool WasSent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

public sealed record CreateAlertRequest(
    string Name,
    Guid UserId,
    AlertCriteriaRequest Criteria,
    NotificationMethod NotificationMethod,
    string? Email,
    string? WebhookUrl);

public sealed record AlertCriteriaRequest(
    string? Make,
    string? Model,
    int? YearFrom,
    int? YearTo,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MaxMileage,
    string? Trim,
    string? BodyStyle,
    string? Transmission,
    string? FuelType,
    string? ExteriorColor);

public sealed record UpdateAlertRequest(
    string? Name,
    AlertCriteriaRequest? Criteria,
    NotificationMethod? NotificationMethod,
    string? Email,
    string? WebhookUrl);

public sealed record CheckAlertsRequest(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    decimal Price,
    int? Mileage);

public sealed record AlertHistoryResponse(
    Guid AlertId,
    string AlertName,
    IReadOnlyList<AlertNotificationResponse> Notifications,
    int TotalTriggerCount);
