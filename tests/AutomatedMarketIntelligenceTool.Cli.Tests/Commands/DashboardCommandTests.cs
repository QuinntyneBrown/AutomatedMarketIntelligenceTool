using AutomatedMarketIntelligenceTool.Cli.Commands;
using AutomatedMarketIntelligenceTool.Core.Services.Dashboard;
using FluentAssertions;
using Moq;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Commands;

public class DashboardCommandTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly Mock<SignalHandler> _signalHandlerMock;
    private readonly DashboardCommand _command;

    public DashboardCommandTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _signalHandlerMock = new Mock<SignalHandler>();

        // Setup default mock behavior
        _dashboardServiceMock
            .Setup(x => x.GetDashboardDataAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDashboardData());

        _command = new DashboardCommand(
            _dashboardServiceMock.Object,
            _signalHandlerMock.Object,
            null);
    }

    #region Settings Tests

    [Fact]
    public void Settings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new DashboardCommand.Settings();

        // Assert
        settings.Watch.Should().BeFalse();
        settings.RefreshInterval.Should().Be(30);
        settings.Compact.Should().BeFalse();
        settings.TrendDays.Should().Be(30);
        settings.ShowHealth.Should().BeTrue();
    }

    [Fact]
    public void Settings_CanSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new DashboardCommand.Settings();

        // Act
        settings.TenantId = tenantId;
        settings.Watch = true;
        settings.RefreshInterval = 10;
        settings.Compact = true;
        settings.TrendDays = 14;
        settings.ShowHealth = false;

        // Assert
        settings.TenantId.Should().Be(tenantId);
        settings.Watch.Should().BeTrue();
        settings.RefreshInterval.Should().Be(10);
        settings.Compact.Should().BeTrue();
        settings.TrendDays.Should().Be(14);
        settings.ShowHealth.Should().BeFalse();
    }

    [Fact]
    public void Settings_InheritsFromCommandSettings()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Assert
        settings.Should().BeAssignableTo<CommandSettings>();
    }

    [Fact]
    public void Settings_WatchMode_CanBeEnabled()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Act
        settings.Watch = true;

        // Assert
        settings.Watch.Should().BeTrue();
    }

    [Fact]
    public void Settings_RefreshInterval_CanBeCustomized()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Act
        settings.RefreshInterval = 5;

        // Assert
        settings.RefreshInterval.Should().Be(5);
    }

    [Fact]
    public void Settings_CompactMode_CanBeEnabled()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Act
        settings.Compact = true;

        // Assert
        settings.Compact.Should().BeTrue();
    }

    [Fact]
    public void Settings_TrendDays_CanBeCustomized()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Act
        settings.TrendDays = 7;

        // Assert
        settings.TrendDays.Should().Be(7);
    }

    [Fact]
    public void Settings_ShowHealth_CanBeDisabled()
    {
        // Arrange
        var settings = new DashboardCommand.Settings();

        // Act
        settings.ShowHealth = false;

        // Assert
        settings.ShowHealth.Should().BeFalse();
    }

    #endregion

    #region Model Tests

    [Fact]
    public void DashboardData_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var data = new DashboardData();

        // Assert
        data.ListingSummary.Should().NotBeNull();
        data.WatchListSummary.Should().NotBeNull();
        data.AlertSummary.Should().NotBeNull();
        data.MarketTrends.Should().NotBeNull();
        data.SystemMetrics.Should().NotBeNull();
    }

    [Fact]
    public void ListingSummary_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var summary = new ListingSummary();

        // Assert
        summary.TotalListings.Should().Be(0);
        summary.ActiveListings.Should().Be(0);
        summary.NewToday.Should().Be(0);
        summary.NewThisWeek.Should().Be(0);
        summary.PriceDropsToday.Should().Be(0);
        summary.PriceIncreasesToday.Should().Be(0);
        summary.DeactivatedToday.Should().Be(0);
        summary.UniqueVehicles.Should().Be(0);
        summary.BySource.Should().NotBeNull();
        summary.BySource.Should().BeEmpty();
    }

    [Fact]
    public void WatchListSummary_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var summary = new WatchListSummary();

        // Assert
        summary.TotalWatched.Should().Be(0);
        summary.WithPriceChanges.Should().Be(0);
        summary.NoLongerActive.Should().Be(0);
        summary.RecentChanges.Should().NotBeNull();
        summary.RecentChanges.Should().BeEmpty();
    }

    [Fact]
    public void AlertSummary_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var summary = new AlertSummary();

        // Assert
        summary.TotalAlerts.Should().Be(0);
        summary.ActiveAlerts.Should().Be(0);
        summary.TriggeredToday.Should().Be(0);
        summary.TriggeredThisWeek.Should().Be(0);
        summary.RecentNotifications.Should().NotBeNull();
        summary.RecentNotifications.Should().BeEmpty();
    }

    [Fact]
    public void MarketTrends_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var trends = new MarketTrends();

        // Assert
        trends.TrendDays.Should().Be(30);
        trends.AveragePriceTrend.Should().NotBeNull();
        trends.InventoryTrend.Should().NotBeNull();
        trends.NewListingsRateTrend.Should().NotBeNull();
        trends.DaysOnMarketTrend.Should().NotBeNull();
        trends.DailyNewListings.Should().NotBeNull();
        trends.DailyNewListings.Should().BeEmpty();
        trends.DailyPriceChanges.Should().NotBeNull();
        trends.DailyPriceChanges.Should().BeEmpty();
    }

    [Fact]
    public void SystemMetrics_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var metrics = new SystemMetrics();

        // Assert
        metrics.TotalSearchSessions.Should().Be(0);
        metrics.SearchesLast24Hours.Should().Be(0);
        metrics.SavedProfiles.Should().Be(0);
        metrics.DatabaseStats.Should().NotBeNull();
        metrics.ScraperHealth.Should().NotBeNull();
        metrics.ScraperHealth.Should().BeEmpty();
    }

    [Fact]
    public void TrendData_ShouldCalculateChange()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 100,
            PreviousValue = 80
        };

        // Assert
        trend.Change.Should().Be(20);
    }

    [Fact]
    public void TrendData_ShouldCalculatePercentageChange()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 100,
            PreviousValue = 80
        };

        // Assert
        trend.PercentageChange.Should().Be(25);
    }

    [Fact]
    public void TrendData_ShouldDetermineUpDirection()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 100,
            PreviousValue = 80
        };

        // Assert
        trend.Direction.Should().Be(TrendDirection.Up);
    }

    [Fact]
    public void TrendData_ShouldDetermineDownDirection()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 80,
            PreviousValue = 100
        };

        // Assert
        trend.Direction.Should().Be(TrendDirection.Down);
    }

    [Fact]
    public void TrendData_ShouldDetermineStableDirection()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 100,
            PreviousValue = 100
        };

        // Assert
        trend.Direction.Should().Be(TrendDirection.Stable);
    }

    [Fact]
    public void TrendData_WithZeroPreviousValue_ShouldNotThrow()
    {
        // Arrange
        var trend = new TrendData
        {
            CurrentValue = 100,
            PreviousValue = 0
        };

        // Act & Assert - Should not throw
        var percentageChange = trend.PercentageChange;
        percentageChange.Should().Be(0);
    }

    [Fact]
    public void TrendDirection_ShouldHaveCorrectValues()
    {
        // Assert
        TrendDirection.Up.Should().Be(TrendDirection.Up);
        TrendDirection.Down.Should().Be(TrendDirection.Down);
        TrendDirection.Stable.Should().Be(TrendDirection.Stable);
    }

    [Fact]
    public void DailyMetric_ShouldHaveCorrectProperties()
    {
        // Arrange
        var metric = new DailyMetric
        {
            Date = DateTime.Today,
            Count = 10,
            AverageValue = 25000m
        };

        // Assert
        metric.Date.Should().Be(DateTime.Today);
        metric.Count.Should().Be(10);
        metric.AverageValue.Should().Be(25000m);
    }

    [Fact]
    public void WatchedListingChange_ShouldHaveCorrectProperties()
    {
        // Arrange
        var change = new WatchedListingChange
        {
            ListingId = Guid.NewGuid(),
            Title = "2020 Toyota Camry",
            ChangeType = "Price Drop",
            Details = "$25000 → $24000",
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        change.ListingId.Should().NotBeEmpty();
        change.Title.Should().Be("2020 Toyota Camry");
        change.ChangeType.Should().Be("Price Drop");
        change.Details.Should().Be("$25000 → $24000");
        change.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecentAlertNotification_ShouldHaveCorrectProperties()
    {
        // Arrange
        var notification = new RecentAlertNotification
        {
            AlertId = Guid.NewGuid(),
            AlertName = "Toyota Alert",
            ListingId = Guid.NewGuid(),
            ListingTitle = "2021 Toyota Camry",
            TriggeredAt = DateTime.UtcNow
        };

        // Assert
        notification.AlertId.Should().NotBeEmpty();
        notification.AlertName.Should().Be("Toyota Alert");
        notification.ListingId.Should().NotBeEmpty();
        notification.ListingTitle.Should().Be("2021 Toyota Camry");
        notification.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DatabaseStats_ShouldHaveCorrectProperties()
    {
        // Arrange
        var stats = new DatabaseStats
        {
            TotalListings = 1000,
            TotalPriceHistoryRecords = 5000,
            TotalVehicles = 500,
            TotalSearchSessions = 100
        };

        // Assert
        stats.TotalListings.Should().Be(1000);
        stats.TotalPriceHistoryRecords.Should().Be(5000);
        stats.TotalVehicles.Should().Be(500);
        stats.TotalSearchSessions.Should().Be(100);
    }

    [Fact]
    public void ScraperStatus_ShouldHaveCorrectProperties()
    {
        // Arrange
        var status = new ScraperStatus
        {
            SiteName = "Autotrader",
            Status = "Healthy",
            SuccessRate = 95.5,
            ListingsFound = 150,
            LastRun = DateTime.UtcNow
        };

        // Assert
        status.SiteName.Should().Be("Autotrader");
        status.Status.Should().Be("Healthy");
        status.SuccessRate.Should().Be(95.5);
        status.ListingsFound.Should().Be(150);
        status.LastRun.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Helper Methods

    private static DashboardData CreateTestDashboardData()
    {
        return new DashboardData
        {
            ListingSummary = new ListingSummary
            {
                TotalListings = 100,
                ActiveListings = 90,
                NewToday = 5,
                NewThisWeek = 20,
                PriceDropsToday = 3,
                PriceIncreasesToday = 1,
                DeactivatedToday = 2,
                UniqueVehicles = 80,
                BySource = new Dictionary<string, int>
                {
                    { "Autotrader", 50 },
                    { "Kijiji", 30 },
                    { "CarGurus", 20 }
                }
            },
            WatchListSummary = new WatchListSummary
            {
                TotalWatched = 10,
                WithPriceChanges = 2,
                NoLongerActive = 1
            },
            AlertSummary = new AlertSummary
            {
                TotalAlerts = 5,
                ActiveAlerts = 4,
                TriggeredToday = 2,
                TriggeredThisWeek = 5
            },
            MarketTrends = new MarketTrends
            {
                TrendDays = 30,
                AveragePriceTrend = new TrendData
                {
                    CurrentValue = 28000,
                    PreviousValue = 27000
                },
                InventoryTrend = new TrendData
                {
                    CurrentValue = 90,
                    PreviousValue = 85
                }
            },
            SystemMetrics = new SystemMetrics
            {
                TotalSearchSessions = 50,
                SearchesLast24Hours = 5,
                SavedProfiles = 10
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    #endregion
}
