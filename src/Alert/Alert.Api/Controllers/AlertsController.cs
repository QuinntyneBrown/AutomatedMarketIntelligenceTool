using Alert.Api.DTOs;
using Alert.Core.Entities;
using Alert.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetByIdAsync(id, cancellationToken);
        if (alert == null) return NotFound();
        return Ok(MapToResponse(alert));
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<AlertResponse>>> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(alerts.Select(MapToResponse));
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<AlertResponse>>> GetActive(CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetActiveAlertsAsync(cancellationToken);
        return Ok(alerts.Select(MapToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<AlertResponse>> Create([FromBody] CreateAlertRequest request, CancellationToken cancellationToken)
    {
        var data = new CreateAlertData
        {
            Name = request.Name,
            UserId = request.UserId,
            Criteria = MapToCriteria(request.Criteria),
            NotificationMethod = request.NotificationMethod,
            Email = request.Email,
            WebhookUrl = request.WebhookUrl
        };

        var alert = await _alertService.CreateAsync(data, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, MapToResponse(alert));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlertResponse>> Update(Guid id, [FromBody] UpdateAlertRequest request, CancellationToken cancellationToken)
    {
        var data = new UpdateAlertData
        {
            Name = request.Name,
            Criteria = request.Criteria != null ? MapToCriteria(request.Criteria) : null,
            NotificationMethod = request.NotificationMethod,
            Email = request.Email,
            WebhookUrl = request.WebhookUrl
        };

        var alert = await _alertService.UpdateAsync(id, data, cancellationToken);
        return Ok(MapToResponse(alert));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _alertService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<AlertResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertService.ActivateAsync(id, cancellationToken);
        return Ok(MapToResponse(alert));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<AlertResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertService.DeactivateAsync(id, cancellationToken);
        return Ok(MapToResponse(alert));
    }

    [HttpPost("check")]
    public async Task<ActionResult<IEnumerable<AlertNotificationResponse>>> CheckAlerts([FromBody] CheckAlertsRequest request, CancellationToken cancellationToken)
    {
        var notifications = await _alertService.CheckAlertsAsync(
            request.VehicleId,
            request.Make,
            request.Model,
            request.Year,
            request.Price,
            request.Mileage,
            cancellationToken);

        return Ok(notifications.Select(MapToNotificationResponse));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<AlertHistoryResponse>> GetAlertHistory(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var alert = await _alertService.GetByIdAsync(id, cancellationToken);
        if (alert == null) return NotFound();

        var notifications = await _alertService.GetAlertHistoryAsync(id, skip, take, cancellationToken);

        return Ok(new AlertHistoryResponse(
            alert.Id,
            alert.Name,
            notifications.Select(MapToNotificationResponse).ToList(),
            alert.TriggerCount));
    }

    private static AlertResponse MapToResponse(Core.Entities.Alert alert)
    {
        return new AlertResponse(
            alert.Id,
            alert.Name,
            alert.UserId,
            new AlertCriteriaResponse(
                alert.Criteria.Make,
                alert.Criteria.Model,
                alert.Criteria.YearFrom,
                alert.Criteria.YearTo,
                alert.Criteria.MinPrice,
                alert.Criteria.MaxPrice,
                alert.Criteria.MaxMileage,
                alert.Criteria.Trim,
                alert.Criteria.BodyStyle,
                alert.Criteria.Transmission,
                alert.Criteria.FuelType,
                alert.Criteria.ExteriorColor),
            alert.NotificationMethod,
            alert.Email,
            alert.WebhookUrl,
            alert.IsActive,
            alert.CreatedAt,
            alert.UpdatedAt,
            alert.LastTriggeredAt,
            alert.TriggerCount);
    }

    private static AlertNotificationResponse MapToNotificationResponse(AlertNotification notification)
    {
        return new AlertNotificationResponse(
            notification.Id,
            notification.AlertId,
            notification.VehicleId,
            notification.MatchedPrice,
            notification.Message,
            notification.WasSent,
            notification.CreatedAt,
            notification.SentAt);
    }

    private static AlertCriteria MapToCriteria(AlertCriteriaRequest request)
    {
        return new AlertCriteria
        {
            Make = request.Make,
            Model = request.Model,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            MaxMileage = request.MaxMileage,
            Trim = request.Trim,
            BodyStyle = request.BodyStyle,
            Transmission = request.Transmission,
            FuelType = request.FuelType,
            ExteriorColor = request.ExteriorColor
        };
    }
}
