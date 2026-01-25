using Alert.Core.Entities;
using Alert.Core.Events;
using Alert.Core.Interfaces;
using Alert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace Alert.Infrastructure.Services;

public sealed class AlertService : IAlertService
{
    private readonly AlertDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public AlertService(AlertDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<Core.Entities.Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Alerts
            .Include(a => a.Notifications)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Core.Entities.Alert>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Alerts
            .Include(a => a.Notifications)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Core.Entities.Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Alerts
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Core.Entities.Alert> CreateAsync(CreateAlertData data, CancellationToken cancellationToken = default)
    {
        var alert = Core.Entities.Alert.Create(
            data.Name,
            data.UserId,
            data.Criteria,
            data.NotificationMethod,
            data.Email,
            data.WebhookUrl);

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AlertCreatedEvent
        {
            AlertId = alert.Id,
            UserId = alert.UserId,
            Name = alert.Name,
            Make = alert.Criteria.Make,
            Model = alert.Criteria.Model,
            YearFrom = alert.Criteria.YearFrom,
            YearTo = alert.Criteria.YearTo,
            MaxPrice = alert.Criteria.MaxPrice,
            NotificationMethod = alert.NotificationMethod
        }, cancellationToken);

        return alert;
    }

    public async Task<Core.Entities.Alert> UpdateAsync(Guid id, UpdateAlertData data, CancellationToken cancellationToken = default)
    {
        var alert = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Alert {id} not found");

        alert.Update(
            data.Name,
            data.Criteria,
            data.NotificationMethod,
            data.Email,
            data.WebhookUrl);

        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AlertUpdatedEvent
        {
            AlertId = alert.Id,
            UserId = alert.UserId,
            IsActive = alert.IsActive
        }, cancellationToken);

        return alert;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alert = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Alert {id} not found");

        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AlertDeletedEvent
        {
            AlertId = alert.Id,
            UserId = alert.UserId
        }, cancellationToken);
    }

    public async Task<Core.Entities.Alert> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alert = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Alert {id} not found");

        alert.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AlertUpdatedEvent
        {
            AlertId = alert.Id,
            UserId = alert.UserId,
            IsActive = true
        }, cancellationToken);

        return alert;
    }

    public async Task<Core.Entities.Alert> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alert = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Alert {id} not found");

        alert.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AlertUpdatedEvent
        {
            AlertId = alert.Id,
            UserId = alert.UserId,
            IsActive = false
        }, cancellationToken);

        return alert;
    }

    public async Task<IReadOnlyList<AlertNotification>> CheckAlertsAsync(
        Guid vehicleId,
        string make,
        string model,
        int year,
        decimal price,
        int? mileage = null,
        CancellationToken cancellationToken = default)
    {
        var activeAlerts = await GetActiveAlertsAsync(cancellationToken);
        var triggeredNotifications = new List<AlertNotification>();

        foreach (var alert in activeAlerts)
        {
            if (alert.MatchesVehicle(make, model, year, price, mileage))
            {
                var message = $"Found matching vehicle: {year} {make} {model} at ${price:N2}";
                var notification = alert.RecordTrigger(vehicleId, price, message);
                triggeredNotifications.Add(notification);

                await _eventPublisher.PublishAsync(new AlertTriggeredEvent
                {
                    AlertId = alert.Id,
                    NotificationId = notification.Id,
                    UserId = alert.UserId,
                    VehicleId = vehicleId,
                    MatchedPrice = price,
                    Message = message,
                    NotificationMethod = alert.NotificationMethod,
                    Email = alert.Email,
                    WebhookUrl = alert.WebhookUrl
                }, cancellationToken);
            }
        }

        if (triggeredNotifications.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return triggeredNotifications;
    }

    public async Task<IReadOnlyList<AlertNotification>> GetAlertHistoryAsync(
        Guid alertId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var alert = await _context.Alerts
            .Include(a => a.Notifications)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken)
            ?? throw new InvalidOperationException($"Alert {alertId} not found");

        return alert.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
    }
}
