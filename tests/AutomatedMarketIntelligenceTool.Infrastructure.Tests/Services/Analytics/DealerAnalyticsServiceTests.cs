using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerMetricsAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Analytics;

public class DealerAnalyticsServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<ILogger<DealerAnalyticsService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DealerAnalyticsServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _loggerMock = new Mock<ILogger<DealerAnalyticsService>>();
    }

    private DealerAnalyticsService CreateService()
    {
        return new DealerAnalyticsService(_contextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DealerAnalyticsService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DealerAnalyticsService(_contextMock.Object, null!));
    }

    [Fact]
    public async Task GetOrCreateDealerMetricsAsync_CreatesMetricsWhenNotFound()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var metricsDbSet = CreateMockDbSet(new List<DealerMetrics>());
        _contextMock.Setup(c => c.DealerMetrics).Returns(metricsDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = CreateService();

        // Act
        var result = await service.GetOrCreateDealerMetricsAsync(_tenantId, dealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal(dealerId, result.DealerId);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDealerMetricsAsync_ReturnsExistingMetrics()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var existingMetrics = DealerMetrics.Create(_tenantId, dealerId);
        var metricsList = new List<DealerMetrics> { existingMetrics };
        var metricsDbSet = CreateMockDbSet(metricsList);
        _contextMock.Setup(c => c.DealerMetrics).Returns(metricsDbSet.Object);

        var service = CreateService();

        // Act
        var result = await service.GetOrCreateDealerMetricsAsync(_tenantId, dealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMetrics.DealerMetricsId, result.DealerMetricsId);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDealerMetricsAsync_ReturnsNullWhenNotFound()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var metricsDbSet = CreateMockDbSet(new List<DealerMetrics>());
        _contextMock.Setup(c => c.DealerMetrics).Returns(metricsDbSet.Object);

        var service = CreateService();

        // Act
        var result = await service.GetDealerMetricsAsync(_tenantId, dealerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDealerMetricsAsync_ReturnsMetricsWhenFound()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);
        var metricsList = new List<DealerMetrics> { metrics };
        var metricsDbSet = CreateMockDbSet(metricsList);
        _contextMock.Setup(c => c.DealerMetrics).Returns(metricsDbSet.Object);

        var service = CreateService();

        // Act
        var result = await service.GetDealerMetricsAsync(_tenantId, dealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dealerId, result.DealerId);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return mockSet;
    }
}

public class DealerMetricsTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsCorrectDefaults()
    {
        var dealerId = DealerId.CreateNew();

        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        Assert.NotNull(metrics);
        Assert.Equal(_tenantId, metrics.TenantId);
        Assert.Equal(dealerId, metrics.DealerId);
        Assert.Equal(50m, metrics.ReliabilityScore); // Default neutral score
        Assert.Equal(0, metrics.TotalListingsHistorical);
        Assert.Equal(0, metrics.ActiveListingsCount);
        Assert.False(metrics.IsFrequentRelister);
    }

    [Fact]
    public void UpdateMetrics_UpdatesAllValues()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        var updateData = new DealerMetricsUpdateData
        {
            TotalListingsHistorical = 100,
            ActiveListingsCount = 50,
            SoldListingsCount = 30,
            ExpiredListingsCount = 20,
            AverageDaysOnMarket = 45.5,
            MedianDaysOnMarket = 40.0,
            MinDaysOnMarket = 5,
            MaxDaysOnMarket = 120,
            AverageListingPrice = 25000m,
            AveragePriceReduction = 500m,
            AveragePriceReductionPercent = 2.0,
            PriceReductionCount = 25,
            RelistingCount = 10,
            RelistingRate = 0.10,
            VinProvidedRate = 0.95,
            ImageProvidedRate = 0.90,
            DescriptionQualityScore = 75.0
        };

        metrics.UpdateMetrics(updateData);

        Assert.Equal(100, metrics.TotalListingsHistorical);
        Assert.Equal(50, metrics.ActiveListingsCount);
        Assert.Equal(30, metrics.SoldListingsCount);
        Assert.Equal(20, metrics.ExpiredListingsCount);
        Assert.Equal(45.5, metrics.AverageDaysOnMarket);
        Assert.Equal(40.0, metrics.MedianDaysOnMarket);
        Assert.Equal(5, metrics.MinDaysOnMarket);
        Assert.Equal(120, metrics.MaxDaysOnMarket);
        Assert.Equal(25000m, metrics.AverageListingPrice);
        Assert.Equal(500m, metrics.AveragePriceReduction);
        Assert.Equal(2.0, metrics.AveragePriceReductionPercent);
        Assert.Equal(25, metrics.PriceReductionCount);
        Assert.Equal(10, metrics.RelistingCount);
        Assert.Equal(0.10, metrics.RelistingRate);
        Assert.Equal(0.95, metrics.VinProvidedRate);
        Assert.Equal(0.90, metrics.ImageProvidedRate);
        Assert.Equal(75.0, metrics.DescriptionQualityScore);
    }

    [Fact]
    public void UpdateMetrics_RecalculatesReliabilityScore()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        var updateData = new DealerMetricsUpdateData
        {
            TotalListingsHistorical = 100,
            ActiveListingsCount = 50,
            AverageDaysOnMarket = 30, // Good - fast selling
            VinProvidedRate = 1.0, // 100% VIN
            ImageProvidedRate = 1.0, // 100% images
            DescriptionQualityScore = 100.0,
            RelistingCount = 0,
            RelistingRate = 0,
            AveragePriceReductionPercent = 5.0
        };

        metrics.UpdateMetrics(updateData);

        // High reliability score expected for good metrics
        Assert.True(metrics.ReliabilityScore > 70);
    }

    [Fact]
    public void UpdateMetrics_SetsFrequentRelisterFlag()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        var updateData = new DealerMetricsUpdateData
        {
            TotalListingsHistorical = 50,
            RelistingCount = 15, // More than threshold
            RelistingRate = 0.30 // 30% - above 20% threshold
        };

        metrics.UpdateMetrics(updateData);

        Assert.True(metrics.IsFrequentRelister);
    }

    [Fact]
    public void IncrementActiveListings_UpdatesCounts()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        metrics.IncrementActiveListings();

        Assert.Equal(1, metrics.ActiveListingsCount);
        Assert.Equal(1, metrics.TotalListingsHistorical);
    }

    [Fact]
    public void DecrementActiveListings_DoesNotGoBelowZero()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        metrics.DecrementActiveListings();

        Assert.Equal(0, metrics.ActiveListingsCount);
    }

    [Fact]
    public void IncrementSoldListings_UpdatesCountsAndDecrementsActive()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);
        metrics.IncrementActiveListings();

        metrics.IncrementSoldListings();

        Assert.Equal(1, metrics.SoldListingsCount);
        Assert.Equal(0, metrics.ActiveListingsCount);
    }

    [Fact]
    public void IncrementExpiredListings_UpdatesCountsAndDecrementsActive()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);
        metrics.IncrementActiveListings();

        metrics.IncrementExpiredListings();

        Assert.Equal(1, metrics.ExpiredListingsCount);
        Assert.Equal(0, metrics.ActiveListingsCount);
    }

    [Fact]
    public void IncrementRelistingCount_UpdatesCountAndRate()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        // Add some listings first
        for (int i = 0; i < 10; i++)
        {
            metrics.IncrementActiveListings();
        }

        metrics.IncrementRelistingCount();
        metrics.IncrementRelistingCount();

        Assert.Equal(2, metrics.RelistingCount);
        Assert.Equal(0.2, metrics.RelistingRate, 2);
    }

    [Fact]
    public void ScheduleNextAnalysis_SetsCorrectDate()
    {
        var dealerId = DealerId.CreateNew();
        var metrics = DealerMetrics.Create(_tenantId, dealerId);

        metrics.ScheduleNextAnalysis(TimeSpan.FromDays(7));

        Assert.NotNull(metrics.NextScheduledAnalysis);
        Assert.True(metrics.NextScheduledAnalysis > DateTime.UtcNow);
        Assert.True(metrics.NextScheduledAnalysis < DateTime.UtcNow.AddDays(8));
    }
}

public class DealerMetricsUpdateDataTests
{
    [Fact]
    public void DefaultValues_AreZero()
    {
        var data = new DealerMetricsUpdateData();

        Assert.Equal(0, data.TotalListingsHistorical);
        Assert.Equal(0, data.ActiveListingsCount);
        Assert.Equal(0, data.SoldListingsCount);
        Assert.Equal(0, data.ExpiredListingsCount);
        Assert.Equal(0, data.AverageDaysOnMarket);
        Assert.Equal(0m, data.AverageListingPrice);
        Assert.Equal(0, data.RelistingCount);
        Assert.Equal(0, data.RelistingRate);
    }
}

public class DealerAnalysisBatchResultTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var result = new DealerAnalysisBatchResult();

        Assert.Equal(0, result.TotalDealersAnalyzed);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(0, result.ProcessingTimeMs);
        Assert.Empty(result.Errors);
    }
}

public class DealerMarketStatisticsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var stats = new DealerMarketStatistics();

        Assert.Equal(0, stats.TotalDealers);
        Assert.Equal(0, stats.ActiveDealers);
        Assert.Equal(0m, stats.AverageReliabilityScore);
        Assert.Equal(0m, stats.MedianReliabilityScore);
        Assert.Equal(0, stats.FrequentRelisterCount);
        Assert.Equal(0, stats.AverageDaysOnMarket);
        Assert.Equal(0, stats.TotalActiveListings);
        Assert.Empty(stats.DealersByState);
        Assert.Empty(stats.DealersByCity);
    }
}

public class DealerWithMetricsTests
{
    [Fact]
    public void CanCreateWithDealerOnly()
    {
        var dealer = Dealer.Create(Guid.NewGuid(), "Test Dealer");

        var dwm = new DealerWithMetrics
        {
            Dealer = dealer,
            Metrics = null
        };

        Assert.NotNull(dwm.Dealer);
        Assert.Null(dwm.Metrics);
    }

    [Fact]
    public void CanCreateWithDealerAndMetrics()
    {
        var tenantId = Guid.NewGuid();
        var dealer = Dealer.Create(tenantId, "Test Dealer");
        var metrics = DealerMetrics.Create(tenantId, dealer.DealerId);

        var dwm = new DealerWithMetrics
        {
            Dealer = dealer,
            Metrics = metrics
        };

        Assert.NotNull(dwm.Dealer);
        Assert.NotNull(dwm.Metrics);
    }
}
