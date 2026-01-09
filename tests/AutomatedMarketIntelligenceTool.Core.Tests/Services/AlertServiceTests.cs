using AutomatedMarketIntelligenceTool.Core.Models.AlertAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class AlertServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly AlertService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public AlertServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new AlertService(_context, NullLogger<AlertService>.Instance);
    }

    [Fact]
    public async Task CreateAlertAsync_WithValidData_ShouldCreateAlert()
    {
        // Arrange
        var criteria = new AlertCriteria { Make = "Toyota", PriceMax = 30000 };

        // Act
        var alert = await _service.CreateAlertAsync(_testTenantId, "Test Alert", criteria);

        // Assert
        Assert.NotNull(alert);
        Assert.Equal("Test Alert", alert.Name);
        Assert.True(alert.IsActive);
    }

    [Fact]
    public async Task GetAllAlertsAsync_ShouldReturnAllAlerts()
    {
        // Arrange
        var criteria1 = new AlertCriteria { Make = "Toyota" };
        var criteria2 = new AlertCriteria { Make = "Honda" };
        await _service.CreateAlertAsync(_testTenantId, "Alert 1", criteria1);
        await _service.CreateAlertAsync(_testTenantId, "Alert 2", criteria2);

        // Act
        var alerts = await _service.GetAllAlertsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, alerts.Count);
    }

    [Fact]
    public async Task DeactivateAlertAsync_ShouldDeactivateAlert()
    {
        // Arrange
        var criteria = new AlertCriteria { Make = "Toyota" };
        var alert = await _service.CreateAlertAsync(_testTenantId, "Test Alert", criteria);

        // Act
        await _service.DeactivateAlertAsync(_testTenantId, alert.AlertId);

        // Assert
        var deactivated = await _service.GetAlertAsync(_testTenantId, alert.AlertId);
        Assert.False(deactivated.IsActive);
    }

    [Fact]
    public async Task CheckAlertsAsync_WithMatchingListing_ShouldReturnAlert()
    {
        // Arrange
        var criteria = new AlertCriteria { Make = "Toyota", PriceMax = 30000 };
        await _service.CreateAlertAsync(_testTenantId, "Toyota Alert", criteria);
        
        var listing = Listing.Create(_testTenantId, "EXT-001", "test-site",
            "https://example.com", "Toyota", "Camry", 2022, 25000m, Condition.Used);

        // Act
        var matchingAlerts = await _service.CheckAlertsAsync(_testTenantId, listing);

        // Assert
        Assert.Single(matchingAlerts);
    }

    [Fact]
    public async Task CheckAlertsAsync_WithNonMatchingListing_ShouldReturnEmpty()
    {
        // Arrange
        var criteria = new AlertCriteria { Make = "Toyota", PriceMax = 20000 };
        await _service.CreateAlertAsync(_testTenantId, "Toyota Alert", criteria);
        
        var listing = Listing.Create(_testTenantId, "EXT-001", "test-site",
            "https://example.com", "Honda", "Civic", 2022, 25000m, Condition.Used);

        // Act
        var matchingAlerts = await _service.CheckAlertsAsync(_testTenantId, listing);

        // Assert
        Assert.Empty(matchingAlerts);
    }

    private class TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
        public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();
        public DbSet<Core.Models.SearchProfileAggregate.SearchProfile> SearchProfiles => Set<Core.Models.SearchProfileAggregate.SearchProfile>();
        public DbSet<Core.Models.VehicleAggregate.Vehicle> Vehicles => Set<Core.Models.VehicleAggregate.Vehicle>();
        public DbSet<Core.Models.ReviewQueueAggregate.ReviewItem> ReviewItems => Set<Core.Models.ReviewQueueAggregate.ReviewItem>();
        public DbSet<Core.Models.WatchListAggregate.WatchedListing> WatchedListings => Set<Core.Models.WatchListAggregate.WatchedListing>();
        public DbSet<Core.Models.AlertAggregate.Alert> Alerts => Set<Core.Models.AlertAggregate.Alert>();
        public DbSet<Core.Models.AlertAggregate.AlertNotification> AlertNotifications => Set<Core.Models.AlertAggregate.AlertNotification>();
        public DbSet<Core.Models.DealerAggregate.Dealer> Dealers => Set<Core.Models.DealerAggregate.Dealer>();
        public DbSet<Core.Models.ScraperHealthAggregate.ScraperHealthRecord> ScraperHealthRecords => Set<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>();

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
                entity.Ignore(l => l.DealerId);
            });
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(a => a.AlertId);
                entity.Property(a => a.AlertId).HasConversion(id => id.Value, value => new AlertId(value));
            });

            modelBuilder.Entity<Core.Models.WatchListAggregate.WatchedListing>(entity =>
            {
                entity.HasKey(w => w.WatchedListingId);
                entity.Property(w => w.WatchedListingId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.WatchListAggregate.WatchedListingId(value));
                entity.Property(w => w.ListingId).HasConversion(
                    id => id.Value,
                    value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.AlertAggregate.AlertNotification>(entity =>
            {
                entity.HasKey(an => an.NotificationId);
                entity.Property(an => an.AlertId).HasConversion(
                    id => id.Value,
                    value => new AlertId(value));
                entity.Property(an => an.ListingId).HasConversion(
                    id => id.Value,
                    value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.DealerAggregate.Dealer>(entity =>
            {
                entity.HasKey(d => d.DealerId);
                entity.Property(d => d.DealerId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.DealerAggregate.DealerId(value));
            });

            modelBuilder.Entity<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>(entity =>
            {
                entity.HasKey(sh => sh.ScraperHealthRecordId);
                entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(
                    id => id.Value,
                    value => new Core.Models.ScraperHealthAggregate.ScraperHealthRecordId(value));
            });

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId).HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
            });
        }
    }
}
