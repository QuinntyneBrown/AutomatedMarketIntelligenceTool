using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;

/// <summary>
/// Represents a point-in-time snapshot of a dealer's inventory.
/// Used for tracking inventory changes over time and analyzing
/// dealer behavior patterns.
/// </summary>
public class InventorySnapshot
{
    public InventorySnapshotId InventorySnapshotId { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public DealerId DealerId { get; private set; } = null!;

    // Snapshot timing
    public DateTime SnapshotDate { get; private set; }
    public SnapshotPeriodType PeriodType { get; private set; }

    // Inventory counts
    public int TotalListings { get; private set; }
    public int NewListingsAdded { get; private set; }
    public int ListingsRemoved { get; private set; }
    public int ListingsSold { get; private set; }
    public int ListingsExpired { get; private set; }
    public int ListingsRelisted { get; private set; }

    // Inventory value
    public decimal TotalInventoryValue { get; private set; }
    public decimal AverageListingPrice { get; private set; }
    public decimal MedianListingPrice { get; private set; }
    public decimal MinListingPrice { get; private set; }
    public decimal MaxListingPrice { get; private set; }

    // Price changes during period
    public int PriceIncreasesCount { get; private set; }
    public int PriceDecreasesCount { get; private set; }
    public decimal TotalPriceReduction { get; private set; }
    public decimal AveragePriceChange { get; private set; }

    // Age distribution
    public int ListingsUnder30Days { get; private set; }
    public int Listings30To60Days { get; private set; }
    public int Listings60To90Days { get; private set; }
    public int ListingsOver90Days { get; private set; }
    public double AverageDaysOnMarket { get; private set; }

    // Vehicle type distribution
    public Dictionary<string, int> CountByMake { get; private set; } = new();
    public Dictionary<int, int> CountByYear { get; private set; } = new();
    public Dictionary<string, int> CountByCondition { get; private set; } = new();

    // Trends (compared to previous period)
    public int? InventoryChangeFromPrevious { get; private set; }
    public double? InventoryChangePercentFromPrevious { get; private set; }
    public decimal? ValueChangeFromPrevious { get; private set; }
    public double? ValueChangePercentFromPrevious { get; private set; }

    // Metadata
    public DateTime CreatedAt { get; private set; }

    private InventorySnapshot() { }

    public static InventorySnapshot Create(
        Guid tenantId,
        DealerId dealerId,
        DateTime snapshotDate,
        SnapshotPeriodType periodType)
    {
        return new InventorySnapshot
        {
            InventorySnapshotId = InventorySnapshotId.CreateNew(),
            TenantId = tenantId,
            DealerId = dealerId,
            SnapshotDate = snapshotDate,
            PeriodType = periodType,
            CreatedAt = DateTime.UtcNow,
            CountByMake = new Dictionary<string, int>(),
            CountByYear = new Dictionary<int, int>(),
            CountByCondition = new Dictionary<string, int>()
        };
    }

    /// <summary>
    /// Updates the snapshot with inventory count data.
    /// </summary>
    public void SetInventoryCounts(
        int totalListings,
        int newListingsAdded,
        int listingsRemoved,
        int listingsSold,
        int listingsExpired,
        int listingsRelisted)
    {
        TotalListings = totalListings;
        NewListingsAdded = newListingsAdded;
        ListingsRemoved = listingsRemoved;
        ListingsSold = listingsSold;
        ListingsExpired = listingsExpired;
        ListingsRelisted = listingsRelisted;
    }

    /// <summary>
    /// Updates the snapshot with inventory value data.
    /// </summary>
    public void SetInventoryValues(
        decimal totalValue,
        decimal averagePrice,
        decimal medianPrice,
        decimal minPrice,
        decimal maxPrice)
    {
        TotalInventoryValue = totalValue;
        AverageListingPrice = averagePrice;
        MedianListingPrice = medianPrice;
        MinListingPrice = minPrice;
        MaxListingPrice = maxPrice;
    }

    /// <summary>
    /// Updates the snapshot with price change data.
    /// </summary>
    public void SetPriceChanges(
        int increases,
        int decreases,
        decimal totalReduction,
        decimal averageChange)
    {
        PriceIncreasesCount = increases;
        PriceDecreasesCount = decreases;
        TotalPriceReduction = totalReduction;
        AveragePriceChange = averageChange;
    }

    /// <summary>
    /// Updates the snapshot with age distribution data.
    /// </summary>
    public void SetAgeDistribution(
        int under30Days,
        int days30To60,
        int days60To90,
        int over90Days,
        double averageDaysOnMarket)
    {
        ListingsUnder30Days = under30Days;
        Listings30To60Days = days30To60;
        Listings60To90Days = days60To90;
        ListingsOver90Days = over90Days;
        AverageDaysOnMarket = averageDaysOnMarket;
    }

    /// <summary>
    /// Updates the snapshot with vehicle type distribution data.
    /// </summary>
    public void SetDistributions(
        Dictionary<string, int> byMake,
        Dictionary<int, int> byYear,
        Dictionary<string, int> byCondition)
    {
        CountByMake = byMake ?? new Dictionary<string, int>();
        CountByYear = byYear ?? new Dictionary<int, int>();
        CountByCondition = byCondition ?? new Dictionary<string, int>();
    }

    /// <summary>
    /// Sets trend data compared to the previous snapshot.
    /// </summary>
    public void SetTrends(InventorySnapshot? previousSnapshot)
    {
        if (previousSnapshot == null)
        {
            InventoryChangeFromPrevious = null;
            InventoryChangePercentFromPrevious = null;
            ValueChangeFromPrevious = null;
            ValueChangePercentFromPrevious = null;
            return;
        }

        InventoryChangeFromPrevious = TotalListings - previousSnapshot.TotalListings;
        if (previousSnapshot.TotalListings > 0)
        {
            InventoryChangePercentFromPrevious =
                (double)InventoryChangeFromPrevious.Value / previousSnapshot.TotalListings * 100;
        }

        ValueChangeFromPrevious = TotalInventoryValue - previousSnapshot.TotalInventoryValue;
        if (previousSnapshot.TotalInventoryValue > 0)
        {
            ValueChangePercentFromPrevious =
                (double)(ValueChangeFromPrevious.Value / previousSnapshot.TotalInventoryValue) * 100;
        }
    }

    /// <summary>
    /// Gets a summary of the inventory state.
    /// </summary>
    public InventorySnapshotSummary GetSummary()
    {
        return new InventorySnapshotSummary
        {
            SnapshotDate = SnapshotDate,
            PeriodType = PeriodType,
            TotalListings = TotalListings,
            TotalInventoryValue = TotalInventoryValue,
            AverageListingPrice = AverageListingPrice,
            AverageDaysOnMarket = AverageDaysOnMarket,
            NewListingsAdded = NewListingsAdded,
            ListingsRemoved = ListingsRemoved,
            InventoryTurnoverRate = TotalListings > 0
                ? (double)(ListingsSold + ListingsExpired) / TotalListings
                : 0
        };
    }
}

/// <summary>
/// Time period type for inventory snapshots.
/// </summary>
public enum SnapshotPeriodType
{
    /// <summary>Daily snapshot</summary>
    Daily,

    /// <summary>Weekly snapshot (typically Sunday)</summary>
    Weekly,

    /// <summary>Monthly snapshot (first of month)</summary>
    Monthly,

    /// <summary>Quarterly snapshot</summary>
    Quarterly,

    /// <summary>Custom/ad-hoc snapshot</summary>
    Custom
}

/// <summary>
/// Summary data from an inventory snapshot.
/// </summary>
public class InventorySnapshotSummary
{
    public DateTime SnapshotDate { get; init; }
    public SnapshotPeriodType PeriodType { get; init; }
    public int TotalListings { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public decimal AverageListingPrice { get; init; }
    public double AverageDaysOnMarket { get; init; }
    public int NewListingsAdded { get; init; }
    public int ListingsRemoved { get; init; }
    public double InventoryTurnoverRate { get; init; }
}
