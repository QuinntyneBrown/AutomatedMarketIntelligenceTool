namespace AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;

/// <summary>
/// Represents a vehicle entity that links multiple listings of the same vehicle across sources.
/// </summary>
public class Vehicle
{
    public VehicleId VehicleId { get; private set; }
    public Guid TenantId { get; private set; }
    public string? PrimaryVin { get; private set; }
    public string Make { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public string? Trim { get; private set; }
    public decimal? BestPrice { get; private set; }
    public decimal? AveragePrice { get; private set; }
    public int ListingCount { get; private set; }
    public DateTime FirstSeenDate { get; private set; }
    public DateTime LastSeenDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Vehicle()
    {
        VehicleId = VehicleId.Create();
        Make = string.Empty;
        Model = string.Empty;
    }

    public static Vehicle Create(
        Guid tenantId,
        string make,
        string model,
        int year,
        string? trim = null,
        string? primaryVin = null)
    {
        var vehicle = new Vehicle
        {
            VehicleId = VehicleId.Create(),
            TenantId = tenantId,
            Make = make,
            Model = model,
            Year = year,
            Trim = trim,
            PrimaryVin = primaryVin,
            ListingCount = 0,
            FirstSeenDate = DateTime.UtcNow,
            LastSeenDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        return vehicle;
    }

    public void UpdatePrices(decimal? bestPrice, decimal? averagePrice)
    {
        BestPrice = bestPrice;
        AveragePrice = averagePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateListingCount(int count)
    {
        ListingCount = count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastSeenDate()
    {
        LastSeenDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimaryVin(string vin)
    {
        PrimaryVin = vin;
        UpdatedAt = DateTime.UtcNow;
    }
}
