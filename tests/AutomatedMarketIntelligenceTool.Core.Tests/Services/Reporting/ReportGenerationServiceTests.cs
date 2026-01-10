using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Reporting;

public class ReportGenerationServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly Mock<IReportGenerator> _mockHtmlGenerator;
    private readonly Mock<IReportGenerator> _mockPdfGenerator;
    private readonly ReportGenerationService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ReportGenerationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        
        _mockHtmlGenerator = new Mock<IReportGenerator>();
        _mockHtmlGenerator.Setup(g => g.SupportedFormat).Returns(ReportFormat.Html);
        
        _mockPdfGenerator = new Mock<IReportGenerator>();
        _mockPdfGenerator.Setup(g => g.SupportedFormat).Returns(ReportFormat.Pdf);

        var generators = new List<IReportGenerator> { _mockHtmlGenerator.Object, _mockPdfGenerator.Object };
        _service = new ReportGenerationService(_context, generators, NullLogger<ReportGenerationService>.Instance);
    }

    [Fact]
    public async Task GenerateReportAsync_WithHtmlFormat_ShouldCallHtmlGenerator()
    {
        // Arrange
        var outputPath = "/tmp/report.html";
        _mockHtmlGenerator
            .Setup(g => g.GenerateReportAsync(It.IsAny<ReportData>(), outputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputPath);

        var reportData = new ReportData
        {
            Title = "Test Report",
            Listings = new List<Listing>()
        };

        // Create a mock file
        Directory.CreateDirectory("/tmp");
        File.WriteAllText(outputPath, "test");

        // Act
        var report = await _service.GenerateReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Html,
            reportData,
            outputPath);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(ReportStatus.Complete, report.Status);
        Assert.Equal(outputPath, report.FilePath);
        _mockHtmlGenerator.Verify(g => g.GenerateReportAsync(reportData, outputPath, It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    [Fact]
    public async Task GenerateReportAsync_WithPdfFormat_ShouldCallPdfGenerator()
    {
        // Arrange
        var outputPath = "/tmp/report.pdf";
        _mockPdfGenerator
            .Setup(g => g.GenerateReportAsync(It.IsAny<ReportData>(), outputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputPath);

        var reportData = new ReportData
        {
            Title = "Test Report",
            Listings = new List<Listing>()
        };

        // Create a mock file
        Directory.CreateDirectory("/tmp");
        File.WriteAllText(outputPath, "test");

        // Act
        var report = await _service.GenerateReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            reportData,
            outputPath);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(ReportStatus.Complete, report.Status);
        _mockPdfGenerator.Verify(g => g.GenerateReportAsync(reportData, outputPath, It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    [Fact]
    public async Task GenerateReportAsync_WithUnsupportedFormat_ShouldThrowException()
    {
        // Arrange
        var reportData = new ReportData { Title = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GenerateReportAsync(
                _testTenantId,
                "Test",
                ReportFormat.Excel, // Not registered
                reportData,
                "/tmp/report.xlsx"));
    }

    [Fact]
    public async Task GenerateReportAsync_WhenGeneratorThrows_ShouldMarkReportAsFailed()
    {
        // Arrange
        var outputPath = "/tmp/report-error.html";
        _mockHtmlGenerator
            .Setup(g => g.GenerateReportAsync(It.IsAny<ReportData>(), outputPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk full"));

        var reportData = new ReportData { Title = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(async () =>
            await _service.GenerateReportAsync(
                _testTenantId,
                "Test",
                ReportFormat.Html,
                reportData,
                outputPath));

        // Verify report was marked as failed
        var reports = await _context.Reports.ToListAsync();
        Assert.Single(reports);
        Assert.Equal(ReportStatus.Failed, reports[0].Status);
        Assert.Contains("Disk full", reports[0].ErrorMessage);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldSaveReportToDatabase()
    {
        // Arrange
        var outputPath = "/tmp/report-db.html";
        _mockHtmlGenerator
            .Setup(g => g.GenerateReportAsync(It.IsAny<ReportData>(), outputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputPath);

        var reportData = new ReportData { Title = "Test" };
        
        // Create a mock file
        Directory.CreateDirectory("/tmp");
        File.WriteAllText(outputPath, "test content");

        // Act
        var report = await _service.GenerateReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Html,
            reportData,
            outputPath);

        // Assert
        var savedReport = await _context.Reports.FirstOrDefaultAsync(r => r.ReportId == report.ReportId);
        Assert.NotNull(savedReport);
        Assert.Equal("Test Report", savedReport.Name);
        Assert.Equal(ReportFormat.Html, savedReport.Format);

        // Cleanup
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    [Fact]
    public async Task GetReportPathAsync_WithValidReportId_ShouldReturnPath()
    {
        // Arrange
        var outputPath = "/tmp/test-path.html";
        _mockHtmlGenerator
            .Setup(g => g.GenerateReportAsync(It.IsAny<ReportData>(), outputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputPath);

        var reportData = new ReportData { Title = "Test" };
        
        // Create a mock file
        Directory.CreateDirectory("/tmp");
        File.WriteAllText(outputPath, "test");

        var report = await _service.GenerateReportAsync(
            _testTenantId,
            "Test",
            ReportFormat.Html,
            reportData,
            outputPath);

        // Act
        var path = await _service.GetReportPathAsync(report.ReportId);

        // Assert
        Assert.Equal(outputPath, path);

        // Cleanup
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    [Fact]
    public async Task GetReportPathAsync_WithInvalidReportId_ShouldThrowException()
    {
        // Arrange
        var invalidId = ReportId.CreateNew();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GetReportPathAsync(invalidId));
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
        public DbSet<Core.Models.CacheAggregate.ResponseCacheEntry> ResponseCacheEntries => Set<Core.Models.CacheAggregate.ResponseCacheEntry>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Core.Models.DeduplicationAuditAggregate.AuditEntry> AuditEntries => Set<Core.Models.DeduplicationAuditAggregate.AuditEntry>();
        public DbSet<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig> DeduplicationConfigs => Set<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>();
        public DbSet<Core.Models.CustomMarketAggregate.CustomMarket> CustomMarkets => Set<Core.Models.CustomMarketAggregate.CustomMarket>();
        public DbSet<Core.Models.ScheduledReportAggregate.ScheduledReport> ScheduledReports => Set<Core.Models.ScheduledReportAggregate.ScheduledReport>();
        public DbSet<Core.Models.ResourceThrottleAggregate.ResourceThrottle> ResourceThrottles => Set<Core.Models.ResourceThrottleAggregate.ResourceThrottle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ignore strongly-typed ID value objects
            modelBuilder.Ignore<ListingId>();
            modelBuilder.Ignore<ReportId>();
            modelBuilder.Ignore<Core.Models.VehicleAggregate.VehicleId>();
            modelBuilder.Ignore<Core.Models.AlertAggregate.AlertId>();

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.ReportId);
                entity.Property(r => r.ReportId).HasConversion(id => id.Value, value => new ReportId(value));
            });

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(l => l.ListingId);
                entity.Property(l => l.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(l => l.DomainEvents);
                entity.Ignore(l => l.Location);
                entity.Ignore(l => l.Dealer);
                entity.Ignore(l => l.DealerId);
            });

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId).HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
            });

            modelBuilder.Entity<Core.Models.CustomMarketAggregate.CustomMarket>(entity =>
            {
                entity.HasKey(cm => cm.CustomMarketId);
                entity.Property(cm => cm.CustomMarketId).HasConversion(id => id.Value, value => new Core.Models.CustomMarketAggregate.CustomMarketId(value));
            });

            modelBuilder.Entity<Core.Models.ScheduledReportAggregate.ScheduledReport>(entity =>
            {
                entity.HasKey(sr => sr.ScheduledReportId);
                entity.Property(sr => sr.ScheduledReportId).HasConversion(id => id.Value, value => new Core.Models.ScheduledReportAggregate.ScheduledReportId(value));
            });

            modelBuilder.Entity<Core.Models.ResourceThrottleAggregate.ResourceThrottle>(entity =>
            {
                entity.HasKey(rt => rt.ResourceThrottleId);
                entity.Property(rt => rt.ResourceThrottleId).HasConversion(id => id.Value, value => new Core.Models.ResourceThrottleAggregate.ResourceThrottleId(value));
            });

            modelBuilder.Entity<Core.Models.DeduplicationAuditAggregate.AuditEntry>(entity =>
            {
                entity.HasKey(ae => ae.AuditEntryId);
                entity.Property(ae => ae.AuditEntryId).HasConversion(id => id.Value, value => new Core.Models.DeduplicationAuditAggregate.AuditEntryId(value));
            });

            modelBuilder.Entity<Core.Models.DeduplicationConfigAggregate.DeduplicationConfig>(entity =>
            {
                entity.HasKey(dc => dc.ConfigId);
                entity.Property(dc => dc.ConfigId).HasConversion(id => id.Value, value => new Core.Models.DeduplicationConfigAggregate.DeduplicationConfigId(value));
            });

            modelBuilder.Entity<Core.Models.AlertAggregate.Alert>(entity =>
            {
                entity.HasKey(a => a.AlertId);
                entity.Property(a => a.AlertId).HasConversion(id => id.Value, value => new Core.Models.AlertAggregate.AlertId(value));
            });

            modelBuilder.Entity<Core.Models.AlertAggregate.AlertNotification>(entity =>
            {
                entity.HasKey(an => an.NotificationId);
                entity.Property(an => an.AlertId).HasConversion(id => id.Value, value => new Core.Models.AlertAggregate.AlertId(value));
                entity.Property(an => an.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.CacheAggregate.ResponseCacheEntry>(entity =>
            {
                entity.HasKey(rce => rce.CacheEntryId);
                entity.Property(rce => rce.CacheEntryId).HasConversion(id => id.Value, value => Core.Models.CacheAggregate.ResponseCacheEntryId.FromGuid(value));
            });

            modelBuilder.Entity<Core.Models.DealerAggregate.Dealer>(entity =>
            {
                entity.HasKey(d => d.DealerId);
                entity.Property(d => d.DealerId).HasConversion(id => id.Value, value => new Core.Models.DealerAggregate.DealerId(value));
            });

            modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                entity.Property(ph => ph.PriceHistoryId).HasConversion(id => id.Value, value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));
                entity.Property(ph => ph.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.ReviewQueueAggregate.ReviewItem>(entity =>
            {
                entity.HasKey(ri => ri.ReviewItemId);
                entity.Property(ri => ri.ReviewItemId).HasConversion(id => id.Value, value => new Core.Models.ReviewQueueAggregate.ReviewItemId(value));
                entity.Property(ri => ri.Listing1Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Property(ri => ri.Listing2Id).HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(ri => ri.DomainEvents);
            });
        }
    }
}
