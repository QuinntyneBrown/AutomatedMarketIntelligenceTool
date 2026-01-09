using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using System.Text.Json;

namespace AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;

public class Alert
{
    public AlertId AlertId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string CriteriaJson { get; private set; } = null!;
    public NotificationMethod NotificationMethod { get; private set; }
    public string? NotificationTarget { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastTriggeredAt { get; private set; }
    public int TriggerCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Alert() { }

    public static Alert Create(
        Guid tenantId,
        string name,
        AlertCriteria criteria,
        NotificationMethod notificationMethod = NotificationMethod.Console,
        string? notificationTarget = null)
    {
        var criteriaJson = JsonSerializer.Serialize(criteria);
        
        return new Alert
        {
            AlertId = AlertId.CreateNew(),
            TenantId = tenantId,
            Name = name,
            CriteriaJson = criteriaJson,
            NotificationMethod = notificationMethod,
            NotificationTarget = notificationTarget,
            IsActive = true,
            TriggerCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public AlertCriteria GetCriteria()
    {
        return JsonSerializer.Deserialize<AlertCriteria>(CriteriaJson) ?? new AlertCriteria();
    }

    public void UpdateCriteria(AlertCriteria criteria)
    {
        CriteriaJson = JsonSerializer.Serialize(criteria);
    }

    public void Activate() => IsActive = true;
    
    public void Deactivate() => IsActive = false;

    public void RecordTrigger()
    {
        LastTriggeredAt = DateTime.UtcNow;
        TriggerCount++;
    }

    public bool Matches(Listing listing)
    {
        if (!IsActive) return false;

        var criteria = GetCriteria();

        if (criteria.Make != null && !string.Equals(listing.Make, criteria.Make, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.Model != null && !string.Equals(listing.Model, criteria.Model, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.YearMin.HasValue && listing.Year < criteria.YearMin)
            return false;

        if (criteria.YearMax.HasValue && listing.Year > criteria.YearMax)
            return false;

        if (criteria.PriceMin.HasValue && listing.Price < criteria.PriceMin)
            return false;

        if (criteria.PriceMax.HasValue && listing.Price > criteria.PriceMax)
            return false;

        if (criteria.MileageMax.HasValue && listing.Mileage > criteria.MileageMax)
            return false;

        if (criteria.Location != null && !listing.Location.Contains(criteria.Location, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.Dealer != null && !listing.Dealer.Contains(criteria.Dealer, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
