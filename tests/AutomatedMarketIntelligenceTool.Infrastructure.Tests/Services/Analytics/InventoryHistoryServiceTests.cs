using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.InventorySnapshotAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Analytics;

public class InventoryHistoryServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<ILogger<InventoryHistoryService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public InventoryHistoryServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _loggerMock = new Mock<ILogger<InventoryHistoryService>>();
    }

    private InventoryHistoryService CreateService()
    {
        return new InventoryHistoryService(_contextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InventoryHistoryService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InventoryHistoryService(_contextMock.Object, null!));
    }
}

public class InventorySnapshotTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsBasicProperties()
    {
        var dealerId = DealerId.CreateNew();
        var snapshotDate = DateTime.UtcNow.Date;

        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, snapshotDate, SnapshotPeriodType.Daily);

        Assert.NotNull(snapshot);
        Assert.Equal(_tenantId, snapshot.TenantId);
        Assert.Equal(dealerId, snapshot.DealerId);
        Assert.Equal(snapshotDate, snapshot.SnapshotDate);
        Assert.Equal(SnapshotPeriodType.Daily, snapshot.PeriodType);
    }

    [Fact]
    public void SetInventoryCounts_SetsAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        snapshot.SetInventoryCounts(100, 10, 5, 3, 2, 1);

        Assert.Equal(100, snapshot.TotalListings);
        Assert.Equal(10, snapshot.NewListingsAdded);
        Assert.Equal(5, snapshot.ListingsRemoved);
        Assert.Equal(3, snapshot.ListingsSold);
        Assert.Equal(2, snapshot.ListingsExpired);
        Assert.Equal(1, snapshot.ListingsRelisted);
    }

    [Fact]
    public void SetInventoryValues_SetsAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        snapshot.SetInventoryValues(2500000m, 25000m, 24000m, 15000m, 50000m);

        Assert.Equal(2500000m, snapshot.TotalInventoryValue);
        Assert.Equal(25000m, snapshot.AverageListingPrice);
        Assert.Equal(24000m, snapshot.MedianListingPrice);
        Assert.Equal(15000m, snapshot.MinListingPrice);
        Assert.Equal(50000m, snapshot.MaxListingPrice);
    }

    [Fact]
    public void SetPriceChanges_SetsAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        snapshot.SetPriceChanges(5, 10, 5000m, -200m);

        Assert.Equal(5, snapshot.PriceIncreasesCount);
        Assert.Equal(10, snapshot.PriceDecreasesCount);
        Assert.Equal(5000m, snapshot.TotalPriceReduction);
        Assert.Equal(-200m, snapshot.AveragePriceChange);
    }

    [Fact]
    public void SetAgeDistribution_SetsAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        snapshot.SetAgeDistribution(40, 30, 20, 10, 45.5);

        Assert.Equal(40, snapshot.ListingsUnder30Days);
        Assert.Equal(30, snapshot.Listings30To60Days);
        Assert.Equal(20, snapshot.Listings60To90Days);
        Assert.Equal(10, snapshot.ListingsOver90Days);
        Assert.Equal(45.5, snapshot.AverageDaysOnMarket);
    }

    [Fact]
    public void SetDistributions_SetsAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        var byMake = new Dictionary<string, int> { { "Toyota", 50 }, { "Honda", 30 } };
        var byYear = new Dictionary<int, int> { { 2020, 30 }, { 2021, 40 }, { 2022, 30 } };
        var byCondition = new Dictionary<string, int> { { "New", 20 }, { "Used", 80 } };

        snapshot.SetDistributions(byMake, byYear, byCondition);

        Assert.Equal(2, snapshot.CountByMake.Count);
        Assert.Equal(50, snapshot.CountByMake["Toyota"]);
        Assert.Equal(3, snapshot.CountByYear.Count);
        Assert.Equal(30, snapshot.CountByYear[2020]);
        Assert.Equal(2, snapshot.CountByCondition.Count);
        Assert.Equal(80, snapshot.CountByCondition["Used"]);
    }

    [Fact]
    public void SetTrends_CalculatesFromPreviousSnapshot()
    {
        var dealerId = DealerId.CreateNew();
        var previousSnapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date.AddDays(-1), SnapshotPeriodType.Daily);
        previousSnapshot.SetInventoryCounts(100, 0, 0, 0, 0, 0);
        previousSnapshot.SetInventoryValues(2000000m, 20000m, 19000m, 10000m, 40000m);

        var currentSnapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);
        currentSnapshot.SetInventoryCounts(110, 15, 5, 0, 0, 0); // 10% increase
        currentSnapshot.SetInventoryValues(2200000m, 20000m, 19000m, 10000m, 40000m); // 10% value increase

        currentSnapshot.SetTrends(previousSnapshot);

        Assert.Equal(10, currentSnapshot.InventoryChangeFromPrevious);
        Assert.Equal(10.0, currentSnapshot.InventoryChangePercentFromPrevious);
        Assert.Equal(200000m, currentSnapshot.ValueChangeFromPrevious);
        Assert.Equal(10.0, currentSnapshot.ValueChangePercentFromPrevious);
    }

    [Fact]
    public void SetTrends_HandleNullPreviousSnapshot()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);

        snapshot.SetTrends(null);

        Assert.Null(snapshot.InventoryChangeFromPrevious);
        Assert.Null(snapshot.InventoryChangePercentFromPrevious);
        Assert.Null(snapshot.ValueChangeFromPrevious);
        Assert.Null(snapshot.ValueChangePercentFromPrevious);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectValues()
    {
        var dealerId = DealerId.CreateNew();
        var snapshotDate = DateTime.UtcNow.Date;
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, snapshotDate, SnapshotPeriodType.Weekly);
        snapshot.SetInventoryCounts(100, 10, 5, 3, 2, 1);
        snapshot.SetInventoryValues(2500000m, 25000m, 24000m, 15000m, 50000m);
        snapshot.SetAgeDistribution(40, 30, 20, 10, 45.5);

        var summary = snapshot.GetSummary();

        Assert.Equal(snapshotDate, summary.SnapshotDate);
        Assert.Equal(SnapshotPeriodType.Weekly, summary.PeriodType);
        Assert.Equal(100, summary.TotalListings);
        Assert.Equal(2500000m, summary.TotalInventoryValue);
        Assert.Equal(25000m, summary.AverageListingPrice);
        Assert.Equal(45.5, summary.AverageDaysOnMarket);
        Assert.Equal(10, summary.NewListingsAdded);
        Assert.Equal(5, summary.ListingsRemoved);
    }

    [Fact]
    public void GetSummary_CalculatesTurnoverRate()
    {
        var dealerId = DealerId.CreateNew();
        var snapshot = InventorySnapshot.Create(_tenantId, dealerId, DateTime.UtcNow.Date, SnapshotPeriodType.Daily);
        snapshot.SetInventoryCounts(100, 10, 5, 20, 5, 1); // 25 sold+expired out of 100

        var summary = snapshot.GetSummary();

        Assert.Equal(0.25, summary.InventoryTurnoverRate, 2);
    }
}

public class SnapshotPeriodTypeTests
{
    [Theory]
    [InlineData(SnapshotPeriodType.Daily)]
    [InlineData(SnapshotPeriodType.Weekly)]
    [InlineData(SnapshotPeriodType.Monthly)]
    [InlineData(SnapshotPeriodType.Quarterly)]
    [InlineData(SnapshotPeriodType.Custom)]
    public void AllPeriodTypesExist(SnapshotPeriodType periodType)
    {
        Assert.True(Enum.IsDefined(typeof(SnapshotPeriodType), periodType));
    }
}

public class InventorySnapshotSummaryTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var summary = new InventorySnapshotSummary();

        Assert.Equal(default, summary.SnapshotDate);
        Assert.Equal(default, summary.PeriodType);
        Assert.Equal(0, summary.TotalListings);
        Assert.Equal(0m, summary.TotalInventoryValue);
        Assert.Equal(0m, summary.AverageListingPrice);
        Assert.Equal(0, summary.AverageDaysOnMarket);
        Assert.Equal(0, summary.NewListingsAdded);
        Assert.Equal(0, summary.ListingsRemoved);
        Assert.Equal(0, summary.InventoryTurnoverRate);
    }
}

public class SnapshotBatchResultTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var result = new SnapshotBatchResult();

        Assert.Equal(0, result.TotalDealers);
        Assert.Equal(0, result.SnapshotsCreated);
        Assert.Equal(0, result.Failures);
        Assert.Equal(0, result.ProcessingTimeMs);
        Assert.Empty(result.Errors);
    }
}

public class InventoryTrendDataTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var trend = new InventoryTrendData();

        Assert.Null(trend.DealerId);
        Assert.Equal(default, trend.PeriodStart);
        Assert.Equal(default, trend.PeriodEnd);
        Assert.Empty(trend.DataPoints);
        Assert.Equal(default, trend.InventoryTrend);
        Assert.Equal(default, trend.ValueTrend);
        Assert.Equal(0, trend.InventoryChangePercent);
        Assert.Equal(0, trend.ValueChangePercent);
        Assert.Equal(0, trend.AverageTurnoverRate);
    }
}

public class InventoryDataPointTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var dataPoint = new InventoryDataPoint();

        Assert.Equal(default, dataPoint.Date);
        Assert.Equal(0, dataPoint.TotalListings);
        Assert.Equal(0m, dataPoint.TotalValue);
        Assert.Equal(0m, dataPoint.AveragePrice);
        Assert.Equal(0, dataPoint.NewListings);
        Assert.Equal(0, dataPoint.RemovedListings);
    }
}

public class TrendDirectionTests
{
    [Theory]
    [InlineData(TrendDirection.Increasing)]
    [InlineData(TrendDirection.Stable)]
    [InlineData(TrendDirection.Decreasing)]
    public void AllDirectionsExist(TrendDirection direction)
    {
        Assert.True(Enum.IsDefined(typeof(TrendDirection), direction));
    }
}

public class InventoryComparisonTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var comparison = new InventoryComparison();

        Assert.Null(comparison.DealerId);
        Assert.Equal(default, comparison.Date1);
        Assert.Equal(default, comparison.Date2);
        Assert.Equal(0, comparison.ListingsDate1);
        Assert.Equal(0, comparison.ListingsDate2);
        Assert.Equal(0, comparison.ListingChange);
        Assert.Equal(0, comparison.ListingChangePercent);
        Assert.Equal(0m, comparison.ValueDate1);
        Assert.Equal(0m, comparison.ValueDate2);
        Assert.Equal(0m, comparison.ValueChange);
        Assert.Equal(0, comparison.ValueChangePercent);
        Assert.Equal(0, comparison.ListingsAdded);
        Assert.Equal(0, comparison.ListingsRemoved);
    }
}

public class MarketInventoryStatisticsTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var stats = new MarketInventoryStatistics();

        Assert.Equal(default, stats.AsOfDate);
        Assert.Equal(0, stats.TotalDealers);
        Assert.Equal(0, stats.TotalListings);
        Assert.Equal(0m, stats.TotalInventoryValue);
        Assert.Equal(0m, stats.AverageListingPrice);
        Assert.Equal(0, stats.AverageDaysOnMarket);
        Assert.Equal(0, stats.NewListingsLast7Days);
        Assert.Equal(0, stats.RemovedListingsLast7Days);
        Assert.Empty(stats.ListingsByMake);
        Assert.Empty(stats.ListingsByYear);
    }
}

public class DealerInventoryChangeTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var change = new DealerInventoryChange();

        Assert.Null(change.DealerId);
        Assert.Null(change.DealerName);
        Assert.Equal(0, change.PreviousListingCount);
        Assert.Equal(0, change.CurrentListingCount);
        Assert.Equal(0, change.ChangeAmount);
        Assert.Equal(0, change.ChangePercent);
        Assert.Equal(default, change.Type);
    }
}

public class ChangeTypeTests
{
    [Theory]
    [InlineData(ChangeType.SignificantIncrease)]
    [InlineData(ChangeType.SignificantDecrease)]
    [InlineData(ChangeType.MassAddition)]
    [InlineData(ChangeType.MassRemoval)]
    public void AllChangeTypesExist(ChangeType changeType)
    {
        Assert.True(Enum.IsDefined(typeof(ChangeType), changeType));
    }
}
