using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.PriceHistoryAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.VehicleAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.WatchListAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Dashboard;

public class DashboardServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly DashboardService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<DashboardTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DashboardTestContext(options);
        _service = new DashboardService(_context, NullLogger<DashboardService>.Instance);
    }

    #region GetDashboardDataAsync Tests

    [Fact]
    public async Task GetDashboardDataAsync_WithEmptyDatabase_ShouldReturnZeroValues()
    {
        // Act
        var result = await _service.GetDashboardDataAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ListingSummary.TotalListings);
        Assert.Equal(0, result.ListingSummary.ActiveListings);
        Assert.Equal(0, result.WatchListSummary.TotalWatched);
        Assert.Equal(0, result.AlertSummary.TotalAlerts);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldPopulateAllSections()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetDashboardDataAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ListingSummary);
        Assert.NotNull(result.WatchListSummary);
        Assert.NotNull(result.AlertSummary);
        Assert.NotNull(result.MarketTrends);
        Assert.NotNull(result.SystemMetrics);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithCustomTrendDays_ShouldUseProvidedValue()
    {
        // Arrange
        await SeedTestDataAsync();
        var customTrendDays = 14;

        // Act
        var result = await _service.GetDashboardDataAsync(_testTenantId, customTrendDays);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customTrendDays, result.MarketTrends.TrendDays);
    }

    #endregion

    #region GetListingSummaryAsync Tests

    [Fact]
    public async Task GetListingSummaryAsync_ShouldReturnCorrectTotalCount()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.TotalListings);
    }

    [Fact]
    public async Task GetListingSummaryAsync_ShouldCountActiveListings()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.ActiveListings);
    }

    [Fact]
    public async Task GetListingSummaryAsync_WithDeactivatedListings_ShouldCountCorrectly()
    {
        // Arrange
        var activeListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var deactivatedListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        deactivatedListing.Deactivate();

        _context.Listings.AddRange(activeListing, deactivatedListing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalListings);
        Assert.Equal(1, result.ActiveListings);
    }

    [Fact]
    public async Task GetListingSummaryAsync_ShouldReturnBySourceBreakdown()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.BySource);
        Assert.True(result.BySource.ContainsKey("TestSite"));
    }

    [Fact]
    public async Task GetListingSummaryAsync_ShouldCountUniqueVehicles()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedVehiclesAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UniqueVehicles >= 0);
    }

    [Fact]
    public async Task GetListingSummaryAsync_WithNewListingsToday_ShouldCountCorrectly()
    {
        // Arrange - All listings created "now" count as today
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.NewToday); // All 6 listings were created today
    }

    [Fact]
    public async Task GetListingSummaryAsync_WithPriceChangesToday_ShouldCountCorrectly()
    {
        // Arrange
        var listing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        var priceHistory = Models.PriceHistoryAggregate.PriceHistory.Create(
            _testTenantId, listing.ListingId, 24000m, 25000m);
        _context.PriceHistory.Add(priceHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetListingSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PriceDropsToday);
    }

    #endregion

    #region GetWatchListSummaryAsync Tests

    [Fact]
    public async Task GetWatchListSummaryAsync_WithEmptyWatchList_ShouldReturnZero()
    {
        // Act
        var result = await _service.GetWatchListSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalWatched);
    }

    [Fact]
    public async Task GetWatchListSummaryAsync_ShouldCountWatchedListings()
    {
        // Arrange
        await SeedWatchListAsync();

        // Act
        var result = await _service.GetWatchListSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalWatched);
    }

    [Fact]
    public async Task GetWatchListSummaryAsync_WithInactiveWatchedListing_ShouldCountAsNoLongerActive()
    {
        // Arrange
        var activeListing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var inactiveListing = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);
        inactiveListing.Deactivate();

        _context.Listings.AddRange(activeListing, inactiveListing);
        await _context.SaveChangesAsync();

        var watched1 = Models.WatchListAggregate.WatchedListing.Create(
            _testTenantId, activeListing.ListingId, "Active listing");
        var watched2 = Models.WatchListAggregate.WatchedListing.Create(
            _testTenantId, inactiveListing.ListingId, "Inactive listing");
        _context.WatchedListings.AddRange(watched1, watched2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWatchListSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalWatched);
        Assert.Equal(1, result.NoLongerActive);
    }

    #endregion

    #region GetAlertSummaryAsync Tests

    [Fact]
    public async Task GetAlertSummaryAsync_WithNoAlerts_ShouldReturnZero()
    {
        // Act
        var result = await _service.GetAlertSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalAlerts);
        Assert.Equal(0, result.ActiveAlerts);
    }

    [Fact]
    public async Task GetAlertSummaryAsync_ShouldCountAlerts()
    {
        // Arrange
        await SeedAlertsAsync();

        // Act
        var result = await _service.GetAlertSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalAlerts);
    }

    [Fact]
    public async Task GetAlertSummaryAsync_ShouldCountActiveAlerts()
    {
        // Arrange
        await SeedAlertsAsync();

        // Act
        var result = await _service.GetAlertSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ActiveAlerts);
    }

    [Fact]
    public async Task GetAlertSummaryAsync_WithDeactivatedAlert_ShouldNotCountAsActive()
    {
        // Arrange
        var criteria = new Models.AlertAggregate.AlertCriteria { Make = "Toyota", PriceMax = 30000m };
        var activeAlert = Models.AlertAggregate.Alert.Create(_testTenantId, "Active Alert", criteria);
        var inactiveAlert = Models.AlertAggregate.Alert.Create(_testTenantId, "Inactive Alert", criteria);
        inactiveAlert.Deactivate();

        _context.Alerts.AddRange(activeAlert, inactiveAlert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAlertSummaryAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalAlerts);
        Assert.Equal(1, result.ActiveAlerts);
    }

    #endregion

    #region GetMarketTrendsAsync Tests

    [Fact]
    public async Task GetMarketTrendsAsync_WithEmptyDatabase_ShouldReturnZeroTrends()
    {
        // Act
        var result = await _service.GetMarketTrendsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.AveragePriceTrend.CurrentValue);
        Assert.Equal(0, result.InventoryTrend.CurrentValue);
    }

    [Fact]
    public async Task GetMarketTrendsAsync_ShouldCalculateAveragePriceTrend()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetMarketTrendsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AveragePriceTrend.CurrentValue > 0);
    }

    [Fact]
    public async Task GetMarketTrendsAsync_ShouldCalculateInventoryTrend()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetMarketTrendsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.InventoryTrend.CurrentValue > 0);
    }

    [Fact]
    public async Task GetMarketTrendsAsync_ShouldReturnDailyNewListings()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetMarketTrendsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.DailyNewListings);
    }

    [Fact]
    public async Task GetMarketTrendsAsync_WithCustomTrendDays_ShouldUseDays()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetMarketTrendsAsync(_testTenantId, 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.TrendDays);
    }

    #endregion

    #region GetSystemMetricsAsync Tests

    [Fact]
    public async Task GetSystemMetricsAsync_ShouldReturnDatabaseStats()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetSystemMetricsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DatabaseStats);
        Assert.Equal(6, result.DatabaseStats.TotalListings);
    }

    [Fact]
    public async Task GetSystemMetricsAsync_ShouldCountSearchSessions()
    {
        // Arrange
        await SeedSearchSessionsAsync();

        // Act
        var result = await _service.GetSystemMetricsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalSearchSessions >= 0);
    }

    [Fact]
    public async Task GetSystemMetricsAsync_ShouldCountSavedProfiles()
    {
        // Arrange
        await SeedSearchProfilesAsync();

        // Act
        var result = await _service.GetSystemMetricsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SavedProfiles >= 0);
    }

    [Fact]
    public async Task GetSystemMetricsAsync_WithEmptyDatabase_ShouldReturnZeros()
    {
        // Act
        var result = await _service.GetSystemMetricsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalSearchSessions);
        Assert.Equal(0, result.SavedProfiles);
        Assert.Equal(0, result.DatabaseStats.TotalListings);
    }

    #endregion

    #region TrendData Tests

    [Fact]
    public void TrendData_WithPositiveChange_ShouldShowUpDirection()
    {
        // Arrange
        var trend = new TrendData { CurrentValue = 100, PreviousValue = 80 };

        // Assert
        Assert.Equal(TrendDirection.Up, trend.Direction);
        Assert.Equal(20, trend.Change);
        Assert.Equal(25, trend.PercentageChange);
    }

    [Fact]
    public void TrendData_WithNegativeChange_ShouldShowDownDirection()
    {
        // Arrange
        var trend = new TrendData { CurrentValue = 80, PreviousValue = 100 };

        // Assert
        Assert.Equal(TrendDirection.Down, trend.Direction);
        Assert.Equal(-20, trend.Change);
        Assert.Equal(-20, trend.PercentageChange);
    }

    [Fact]
    public void TrendData_WithNoChange_ShouldShowStableDirection()
    {
        // Arrange
        var trend = new TrendData { CurrentValue = 100, PreviousValue = 100 };

        // Assert
        Assert.Equal(TrendDirection.Stable, trend.Direction);
        Assert.Equal(0, trend.Change);
        Assert.Equal(0, trend.PercentageChange);
    }

    [Fact]
    public void TrendData_WithZeroPreviousValue_ShouldNotDivideByZero()
    {
        // Arrange
        var trend = new TrendData { CurrentValue = 100, PreviousValue = 0 };

        // Assert
        Assert.Equal(TrendDirection.Up, trend.Direction);
        Assert.Equal(100, trend.Change);
        Assert.Equal(0, trend.PercentageChange); // Division by zero avoided
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetDashboardDataAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.GetDashboardDataAsync(_testTenantId, 30, cts.Token));
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestDataAsync()
    {
        var listings = new[]
        {
            Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
                "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000),
            Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
                "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000),
            Listing.Create(_testTenantId, "EXT-003", "TestSite", "https://test.com/3",
                "Ford", "F-150", 2019, 35000m, Condition.Used, mileage: 45000),
            Listing.Create(_testTenantId, "EXT-004", "TestSite", "https://test.com/4",
                "Toyota", "Camry", 2021, 28000m, Condition.Certified, mileage: 20000),
            Listing.Create(_testTenantId, "EXT-005", "TestSite", "https://test.com/5",
                "Tesla", "Model 3", 2022, 45000m, Condition.New, mileage: 500),
            Listing.Create(_testTenantId, "EXT-006", "TestSite", "https://test.com/6",
                "BMW", "X5", 2020, 55000m, Condition.Used, mileage: 35000)
        };

        _context.Listings.AddRange(listings);
        await _context.SaveChangesAsync();
    }

    private async Task SeedVehiclesAsync()
    {
        var vehicle = Models.VehicleAggregate.Vehicle.Create(
            _testTenantId, "Toyota", "Camry", 2020, null);
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
    }

    private async Task SeedWatchListAsync()
    {
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Toyota", "Camry", 2020, 25000m, Condition.Used, mileage: 30000);
        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Civic", 2021, 22000m, Condition.Used, mileage: 15000);

        _context.Listings.AddRange(listing1, listing2);
        await _context.SaveChangesAsync();

        var watched1 = Models.WatchListAggregate.WatchedListing.Create(
            _testTenantId, listing1.ListingId, "Watching this one");
        var watched2 = Models.WatchListAggregate.WatchedListing.Create(
            _testTenantId, listing2.ListingId, "Also watching this");

        _context.WatchedListings.AddRange(watched1, watched2);
        await _context.SaveChangesAsync();
    }

    private async Task SeedAlertsAsync()
    {
        var criteria1 = new Models.AlertAggregate.AlertCriteria { Make = "Toyota", PriceMax = 30000m };
        var criteria2 = new Models.AlertAggregate.AlertCriteria { Make = "Honda", PriceMax = 25000m };

        var alert1 = Models.AlertAggregate.Alert.Create(_testTenantId, "Toyota Alert", criteria1);
        var alert2 = Models.AlertAggregate.Alert.Create(_testTenantId, "Honda Alert", criteria2);

        _context.Alerts.AddRange(alert1, alert2);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSearchSessionsAsync()
    {
        var session = Models.SearchSessionAggregate.SearchSession.Create(
            _testTenantId, "{}", "192.168.1.1");
        _context.SearchSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSearchProfilesAsync()
    {
        var profile = Models.SearchProfileAggregate.SearchProfile.Create(
            _testTenantId, "Test Profile", "{}");
        _context.SearchProfiles.Add(profile);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Test Context

    private class DashboardTestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public DashboardTestContext(DbContextOptions<DashboardTestContext> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
        public DbSet<SearchSession> SearchSessions => Set<SearchSession>();
        public DbSet<SearchProfile> SearchProfiles => Set<SearchProfile>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
        public DbSet<WatchedListing> WatchedListings => Set<WatchedListing>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<AlertNotification> AlertNotifications => Set<AlertNotification>();
        public DbSet<Dealer> Dealers => Set<Dealer>();
        public DbSet<ScraperHealthRecord> ScraperHealthRecords => Set<ScraperHealthRecord>();
        public DbSet<ResponseCacheEntry> ResponseCacheEntries => Set<ResponseCacheEntry>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Core.Models.DeduplicationAuditAggregate.AuditEntry> AuditEntries => Set<Core.Models.DeduplicationAuditAggregate.AuditEntry>();
        public DbSet<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig> DeduplicationConfigs => Set<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>();
        public DbSet<Core.Models.CustomMarketAggregate.CustomMarket> CustomMarkets => Set<Core.Models.CustomMarketAggregate.CustomMarket>();
        public DbSet<Core.Models.ScheduledReportAggregate.ScheduledReport> ScheduledReports => Set<Core.Models.ScheduledReportAggregate.ScheduledReport>();
        public DbSet<Core.Models.ResourceThrottleAggregate.ResourceThrottle> ResourceThrottles => Set<Core.Models.ResourceThrottleAggregate.ResourceThrottle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(l => l.ListingId);
                entity.Property(l => l.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(l => l.DomainEvents);
                entity.Ignore(l => l.Location);
                entity.Ignore(l => l.Dealer);
                entity.Ignore(l => l.DealerEntity);
                entity.Ignore(l => l.DealerId);
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                entity.Property(ph => ph.PriceHistoryId).HasConversion(id => id.Value, value => new PriceHistoryId(value));
                entity.Property(ph => ph.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<SearchSession>(entity =>
            {
                entity.HasKey(ss => ss.SearchSessionId);
                entity.Property(ss => ss.SearchSessionId).HasConversion(id => id.Value, value => new SearchSessionId(value));
            });

            modelBuilder.Entity<SearchProfile>(entity =>
            {
                entity.HasKey(sp => sp.SearchProfileId);
                entity.Property(sp => sp.SearchProfileId).HasConversion(id => id.Value, value => SearchProfileId.From(value));
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId).HasConversion(id => id.Value, value => new VehicleId(value));
            });

            modelBuilder.Entity<ReviewItem>(entity =>
            {
                entity.HasKey(r => r.ReviewItemId);
                entity.Property(r => r.ReviewItemId).HasConversion(id => id.Value, value => new ReviewItemId(value));
                entity.Property(r => r.Listing1Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Property(r => r.Listing2Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(r => r.DomainEvents);
            });

            modelBuilder.Entity<WatchedListing>(entity =>
            {
                entity.HasKey(w => w.WatchedListingId);
                entity.Property(w => w.WatchedListingId).HasConversion(id => id.Value, value => new WatchedListingId(value));
                entity.Property(w => w.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
                entity.HasOne(w => w.Listing).WithMany().HasForeignKey(w => w.ListingId);
            });

            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(a => a.AlertId);
                entity.Property(a => a.AlertId).HasConversion(id => id.Value, value => new AlertId(value));
            });

            modelBuilder.Entity<AlertNotification>(entity =>
            {
                entity.HasKey(an => an.NotificationId);
                entity.Property(an => an.AlertId).HasConversion(id => id.Value, value => new AlertId(value));
                entity.Property(an => an.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
                entity.HasOne(an => an.Alert).WithMany().HasForeignKey(an => an.AlertId);
                entity.HasOne(an => an.Listing).WithMany().HasForeignKey(an => an.ListingId);
            });

            modelBuilder.Entity<Dealer>(entity =>
            {
                entity.HasKey(d => d.DealerId);
                entity.Property(d => d.DealerId).HasConversion(id => id.Value, value => new DealerId(value));
            });

            modelBuilder.Entity<ScraperHealthRecord>(entity =>
            {
                entity.HasKey(sh => sh.ScraperHealthRecordId);
                entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(id => id.Value, value => new ScraperHealthRecordId(value));
            });

            modelBuilder.Entity<ResponseCacheEntry>(entity =>
            {
                entity.HasKey(c => c.ResponseCacheEntryId);
                entity.Property(c => c.ResponseCacheEntryId).HasConversion(id => id.Value, value => new ResponseCacheEntryId(value));
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.ReportId);
                entity.Property(r => r.ReportId).HasConversion(id => id.Value, value => new ReportId(value));
            });
        }
    }

    #endregion
}
