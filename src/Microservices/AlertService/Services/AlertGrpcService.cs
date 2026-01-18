using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Protos.Alert;
using AutomatedMarketIntelligenceTool.Protos.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProtoNotificationMethod = AutomatedMarketIntelligenceTool.Protos.Alert.NotificationMethod;
using CoreNotificationMethod = AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate.NotificationMethod;

namespace AlertService.Services;

public class AlertGrpcService : AutomatedMarketIntelligenceTool.Protos.Alert.AlertService.AlertServiceBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertGrpcService> _logger;

    public AlertGrpcService(
        IAlertService alertService,
        ILogger<AlertGrpcService> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    public override async Task<CreateAlertResponse> CreateAlert(CreateAlertRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating alert '{Name}' for tenant {TenantId}", request.Name, request.TenantId);

        var tenantId = Guid.Parse(request.TenantId);
        var criteria = MapToAlertCriteria(request.Criteria);
        var notificationMethod = MapToNotificationMethod(request.NotificationMethod);

        var alert = await _alertService.CreateAlertAsync(
            tenantId,
            request.Name,
            criteria,
            notificationMethod,
            request.NotificationTarget?.Value,
            context.CancellationToken);

        return new CreateAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Alert created successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            AlertId = alert.AlertId.Value.ToString(),
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(alert.CreatedAt, DateTimeKind.Utc))
        };
    }

    public override async Task<GetAlertResponse> GetAlert(GetAlertRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var alertId = AlertId.From(Guid.Parse(request.AlertId));

        var alert = await _alertService.GetAlertAsync(tenantId, alertId, context.CancellationToken);

        if (alert == null)
        {
            return new GetAlertResponse
            {
                Response = new ServiceResponse
                {
                    Success = false,
                    Message = "Alert not found"
                }
            };
        }

        return new GetAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Alert = MapToAlertInfo(alert)
        };
    }

    public override async Task<ListAlertsResponse> ListAlerts(ListAlertsRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var activeOnly = request.ActiveOnly?.Value;

        var alerts = await _alertService.GetAllAlertsAsync(tenantId, activeOnly, context.CancellationToken);

        var response = new ListAlertsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = 1,
                PageSize = alerts.Count,
                TotalCount = alerts.Count,
                TotalPages = 1
            }
        };

        foreach (var alert in alerts)
        {
            response.Alerts.Add(MapToAlertInfo(alert));
        }

        return response;
    }

    public override async Task<UpdateAlertResponse> UpdateAlert(UpdateAlertRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var alertId = AlertId.From(Guid.Parse(request.AlertId));
        var criteria = MapToAlertCriteria(request.Criteria);

        await _alertService.UpdateAlertAsync(tenantId, alertId, criteria, context.CancellationToken);

        var updatedAlert = await _alertService.GetAlertAsync(tenantId, alertId, context.CancellationToken);

        return new UpdateAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Alert updated successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Alert = updatedAlert != null ? MapToAlertInfo(updatedAlert) : null
        };
    }

    public override async Task<ActivateAlertResponse> ActivateAlert(ActivateAlertRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var alertId = AlertId.From(Guid.Parse(request.AlertId));

        await _alertService.ActivateAlertAsync(tenantId, alertId, context.CancellationToken);

        return new ActivateAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Alert activated",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override async Task<DeactivateAlertResponse> DeactivateAlert(DeactivateAlertRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var alertId = AlertId.From(Guid.Parse(request.AlertId));

        await _alertService.DeactivateAlertAsync(tenantId, alertId, context.CancellationToken);

        return new DeactivateAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Alert deactivated",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override async Task<DeleteAlertResponse> DeleteAlert(DeleteAlertRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var alertId = AlertId.From(Guid.Parse(request.AlertId));

        await _alertService.DeleteAlertAsync(tenantId, alertId, context.CancellationToken);

        return new DeleteAlertResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Alert deleted",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override async Task<CheckListingAgainstAlertsResponse> CheckListingAgainstAlerts(CheckListingAgainstAlertsRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);

        // Map proto listing to core listing for checking
        var listing = MapToCoreListing(request.Listing);
        var matchedAlerts = await _alertService.CheckAlertsAsync(tenantId, listing, context.CancellationToken);

        var response = new CheckListingAgainstAlertsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        foreach (var alert in matchedAlerts)
        {
            response.MatchedAlerts.Add(new MatchedAlert
            {
                AlertId = alert.AlertId.Value.ToString(),
                AlertName = alert.Name
            });
        }

        return response;
    }

    public override async Task StreamNotifications(StreamNotificationsRequest request, IServerStreamWriter<NotificationEvent> responseStream, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);

        _logger.LogInformation("Starting notification stream for tenant {TenantId}", tenantId);

        // This would typically connect to a message queue or event stream
        // For demo purposes, we'll simulate periodic checks
        while (!context.CancellationToken.IsCancellationRequested)
        {
            // In a real implementation, this would listen to an event stream
            await Task.Delay(5000, context.CancellationToken);

            // Placeholder for actual notification streaming
            // When a new notification is available, it would be sent here
        }
    }

    public override Task<AutomatedMarketIntelligenceTool.Protos.Alert.HealthCheckResponse> HealthCheck(
        AutomatedMarketIntelligenceTool.Protos.Alert.HealthCheckRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new AutomatedMarketIntelligenceTool.Protos.Alert.HealthCheckResponse
        {
            Healthy = true,
            Status = "Healthy",
            Components =
            {
                { "grpc", "Healthy" },
                { "database", "Healthy" },
                { "notifications", "Healthy" }
            }
        });
    }

    private static Core.Models.AlertAggregate.AlertCriteria MapToAlertCriteria(Protos.Alert.AlertCriteria proto)
    {
        return new Core.Models.AlertAggregate.AlertCriteria
        {
            Makes = proto.Makes.ToList(),
            Models = proto.Models.ToList(),
            YearMin = proto.YearMin?.Value,
            YearMax = proto.YearMax?.Value,
            PriceMin = proto.PriceMin != null ? (decimal)proto.PriceMin.Value : null,
            PriceMax = proto.PriceMax != null ? (decimal)proto.PriceMax.Value : null,
            MileageMax = proto.MileageMax?.Value,
            City = proto.City?.Value,
            Province = proto.Province?.Value,
            RadiusKm = proto.RadiusKm?.Value,
            Keywords = proto.Keywords.ToList(),
            NotifyOnPriceDrop = proto.NotifyOnPriceDrop,
            PriceDropThreshold = proto.PriceDropThreshold?.Value
        };
    }

    private static Protos.Alert.AlertCriteria MapToProtoCriteria(Core.Models.AlertAggregate.AlertCriteria criteria)
    {
        var proto = new Protos.Alert.AlertCriteria
        {
            NotifyOnPriceDrop = criteria.NotifyOnPriceDrop
        };

        proto.Makes.AddRange(criteria.Makes);
        proto.Models.AddRange(criteria.Models);
        proto.Keywords.AddRange(criteria.Keywords);

        if (criteria.YearMin.HasValue) proto.YearMin = criteria.YearMin.Value;
        if (criteria.YearMax.HasValue) proto.YearMax = criteria.YearMax.Value;
        if (criteria.PriceMin.HasValue) proto.PriceMin = (double)criteria.PriceMin.Value;
        if (criteria.PriceMax.HasValue) proto.PriceMax = (double)criteria.PriceMax.Value;
        if (criteria.MileageMax.HasValue) proto.MileageMax = criteria.MileageMax.Value;
        if (criteria.City != null) proto.City = criteria.City;
        if (criteria.Province != null) proto.Province = criteria.Province;
        if (criteria.RadiusKm.HasValue) proto.RadiusKm = criteria.RadiusKm.Value;
        if (criteria.PriceDropThreshold.HasValue) proto.PriceDropThreshold = criteria.PriceDropThreshold.Value;

        return proto;
    }

    private static CoreNotificationMethod MapToNotificationMethod(ProtoNotificationMethod method) => method switch
    {
        ProtoNotificationMethod.Console => CoreNotificationMethod.Console,
        ProtoNotificationMethod.Email => CoreNotificationMethod.Email,
        ProtoNotificationMethod.Sms => CoreNotificationMethod.Sms,
        ProtoNotificationMethod.Webhook => CoreNotificationMethod.Webhook,
        ProtoNotificationMethod.Push => CoreNotificationMethod.Push,
        _ => CoreNotificationMethod.Console
    };

    private static ProtoNotificationMethod MapToProtoNotificationMethod(CoreNotificationMethod method) => method switch
    {
        CoreNotificationMethod.Console => ProtoNotificationMethod.Console,
        CoreNotificationMethod.Email => ProtoNotificationMethod.Email,
        CoreNotificationMethod.Sms => ProtoNotificationMethod.Sms,
        CoreNotificationMethod.Webhook => ProtoNotificationMethod.Webhook,
        CoreNotificationMethod.Push => ProtoNotificationMethod.Push,
        _ => ProtoNotificationMethod.NotificationMethodUnspecified
    };

    private static AlertInfo MapToAlertInfo(Alert alert)
    {
        var info = new AlertInfo
        {
            AlertId = alert.AlertId.Value.ToString(),
            TenantId = alert.TenantId.ToString(),
            Name = alert.Name,
            Criteria = MapToProtoCriteria(alert.Criteria),
            NotificationMethod = MapToProtoNotificationMethod(alert.NotificationMethod),
            IsActive = alert.IsActive,
            MatchCount = alert.MatchCount,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(alert.CreatedAt, DateTimeKind.Utc))
        };

        if (alert.NotificationTarget != null)
            info.NotificationTarget = alert.NotificationTarget;
        if (alert.LastTriggered.HasValue)
            info.LastTriggered = Timestamp.FromDateTime(DateTime.SpecifyKind(alert.LastTriggered.Value, DateTimeKind.Utc));
        if (alert.UpdatedAt.HasValue)
            info.UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(alert.UpdatedAt.Value, DateTimeKind.Utc));

        return info;
    }

    private static Core.Models.ListingAggregate.Listing MapToCoreListing(Protos.Common.Listing proto)
    {
        // This creates a minimal listing object for alert checking
        return Core.Models.ListingAggregate.Listing.Create(
            Guid.Parse(proto.TenantId),
            proto.ExternalId,
            proto.SourceSite,
            proto.ListingUrl,
            proto.Make,
            proto.Model,
            proto.Year,
            (decimal)proto.Price,
            MapToCondition(proto.Condition),
            proto.Trim?.Value,
            proto.Mileage?.Value,
            proto.Vin?.Value,
            proto.City?.Value,
            proto.Province?.Value,
            proto.PostalCode?.Value,
            proto.Currency);
    }

    private static Core.Models.ListingAggregate.Enums.Condition MapToCondition(Protos.Common.Condition condition) => condition switch
    {
        Protos.Common.Condition.New => Core.Models.ListingAggregate.Enums.Condition.New,
        Protos.Common.Condition.Used => Core.Models.ListingAggregate.Enums.Condition.Used,
        Protos.Common.Condition.CertifiedPreOwned => Core.Models.ListingAggregate.Enums.Condition.CertifiedPreOwned,
        _ => Core.Models.ListingAggregate.Enums.Condition.Used
    };
}
