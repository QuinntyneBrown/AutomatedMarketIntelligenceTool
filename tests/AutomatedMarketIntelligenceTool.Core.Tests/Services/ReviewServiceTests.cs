using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class ReviewServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ReviewService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContextWithReview>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContextWithReview(options);
        _service = new ReviewService(_context, NullLogger<ReviewService>.Instance);
    }

    [Fact]
    public async Task CreateReviewItemAsync_WithValidData_ShouldCreateReviewItem()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        // Act
        var reviewItem = await _service.CreateReviewItemAsync(
            _testTenantId,
            listing1.ListingId,
            listing2.ListingId,
            75.5,
            MatchMethod.Fuzzy);

        // Assert
        Assert.NotNull(reviewItem);
        Assert.Equal(_testTenantId, reviewItem.TenantId);
        Assert.Equal(75.5m, reviewItem.ConfidenceScore);
        Assert.Equal(MatchMethod.Fuzzy, reviewItem.MatchMethod);
        Assert.Equal(ReviewItemStatus.Pending, reviewItem.Status);
    }

    [Fact]
    public async Task CreateReviewItemAsync_WithDuplicatePair_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateReviewItemAsync(
                _testTenantId, listing1.ListingId, listing2.ListingId, 80.0, MatchMethod.Image));
    }

    [Fact]
    public async Task GetPendingReviewsAsync_ShouldReturnOnlyPendingReviews()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");
        var listing3 = CreateAndSaveListing("EXT-003");

        var review1 = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);
        var review2 = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing3.ListingId, 80.0, MatchMethod.Image);

        // Resolve one review
        await _service.ResolveReviewAsync(_testTenantId, review1.ReviewItemId, ResolutionDecision.SameVehicle);

        // Act
        var pendingReviews = await _service.GetPendingReviewsAsync(_testTenantId);

        // Assert
        Assert.Single(pendingReviews);
        Assert.Equal(review2.ReviewItemId, pendingReviews[0].ReviewItemId);
    }

    [Fact]
    public async Task GetReviewsAsync_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        var review = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);
        await _service.ResolveReviewAsync(_testTenantId, review.ReviewItemId, ResolutionDecision.SameVehicle);

        // Act
        var result = await _service.GetReviewsAsync(_testTenantId, new ReviewFilterOptions
        {
            Status = ReviewItemStatus.Resolved
        });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(ReviewItemStatus.Resolved, result.Items[0].Status);
    }

    [Fact]
    public async Task GetReviewByIdAsync_WithValidId_ShouldReturnReviewItem()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        var createdReview = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act
        var review = await _service.GetReviewByIdAsync(_testTenantId, createdReview.ReviewItemId);

        // Assert
        Assert.NotNull(review);
        Assert.Equal(createdReview.ReviewItemId, review.ReviewItemId);
        Assert.Equal(listing1.Make, review.Listing1Make);
        Assert.Equal(listing2.Make, review.Listing2Make);
    }

    [Fact]
    public async Task GetReviewByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var review = await _service.GetReviewByIdAsync(_testTenantId, ReviewItemId.Create());

        // Assert
        Assert.Null(review);
    }

    [Fact]
    public async Task ResolveReviewAsync_WithSameVehicle_ShouldResolveSuccessfully()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        var review = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act
        var success = await _service.ResolveReviewAsync(
            _testTenantId,
            review.ReviewItemId,
            ResolutionDecision.SameVehicle,
            "TestUser",
            "Confirmed same vehicle");

        // Assert
        Assert.True(success);

        var resolved = await _service.GetReviewByIdAsync(_testTenantId, review.ReviewItemId);
        Assert.Equal(ReviewItemStatus.Resolved, resolved!.Status);
        Assert.Equal(ResolutionDecision.SameVehicle, resolved.Resolution);
        Assert.Equal("TestUser", resolved.ResolvedBy);
        Assert.Equal("Confirmed same vehicle", resolved.Notes);
    }

    [Fact]
    public async Task ResolveReviewAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var success = await _service.ResolveReviewAsync(
            _testTenantId,
            ReviewItemId.Create(),
            ResolutionDecision.SameVehicle);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task DismissReviewAsync_ShouldDismissSuccessfully()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        var review = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act
        var success = await _service.DismissReviewAsync(
            _testTenantId,
            review.ReviewItemId,
            "Not enough data");

        // Assert
        Assert.True(success);

        var dismissed = await _service.GetReviewByIdAsync(_testTenantId, review.ReviewItemId);
        Assert.Equal(ReviewItemStatus.Dismissed, dismissed!.Status);
        Assert.Equal("Not enough data", dismissed.Notes);
    }

    [Fact]
    public async Task DismissReviewAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var success = await _service.DismissReviewAsync(
            _testTenantId,
            ReviewItemId.Create());

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task ReviewExistsAsync_WithExistingPair_ShouldReturnTrue()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act
        var exists = await _service.ReviewExistsAsync(_testTenantId, listing1.ListingId, listing2.ListingId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ReviewExistsAsync_WithReversedPair_ShouldReturnTrue()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");

        await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);

        // Act - check with reversed order
        var exists = await _service.ReviewExistsAsync(_testTenantId, listing2.ListingId, listing1.ListingId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ReviewExistsAsync_WithNonExistingPair_ShouldReturnFalse()
    {
        // Act
        var exists = await _service.ReviewExistsAsync(
            _testTenantId,
            ListingId.Create(),
            ListingId.Create());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var listing1 = CreateAndSaveListing("EXT-001");
        var listing2 = CreateAndSaveListing("EXT-002");
        var listing3 = CreateAndSaveListing("EXT-003");
        var listing4 = CreateAndSaveListing("EXT-004");

        var review1 = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing2.ListingId, 75.0, MatchMethod.Fuzzy);
        var review2 = await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing3.ListingId, 80.0, MatchMethod.Image);
        await _service.CreateReviewItemAsync(
            _testTenantId, listing1.ListingId, listing4.ListingId, 65.0, MatchMethod.Fuzzy);

        await _service.ResolveReviewAsync(_testTenantId, review1.ReviewItemId, ResolutionDecision.SameVehicle);
        await _service.DismissReviewAsync(_testTenantId, review2.ReviewItemId);

        // Act
        var stats = await _service.GetStatsAsync(_testTenantId);

        // Assert
        Assert.Equal(3, stats.TotalCount);
        Assert.Equal(1, stats.PendingCount);
        Assert.Equal(1, stats.ResolvedCount);
        Assert.Equal(1, stats.DismissedCount);
        Assert.Equal(1, stats.SameVehicleCount);
        Assert.Equal(0, stats.DifferentVehicleCount);
    }

    private Listing CreateAndSaveListing(string externalId)
    {
        var listing = Listing.Create(
            _testTenantId,
            externalId,
            "TestSite",
            $"https://test.com/{externalId}",
            "Toyota",
            "Camry",
            2020,
            25000m,
            Condition.Used);

        _context.Listings.Add(listing);
        _context.SaveChangesAsync().Wait();
        return listing;
    }

    private class TestContextWithReview : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContextWithReview(DbContextOptions<TestContextWithReview> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
        public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();
        public DbSet<Core.Models.SearchProfileAggregate.SearchProfile> SearchProfiles => Set<Core.Models.SearchProfileAggregate.SearchProfile>();
        public DbSet<Core.Models.VehicleAggregate.Vehicle> Vehicles => Set<Core.Models.VehicleAggregate.Vehicle>();
        public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
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
                entity.Property(l => l.ListingId)
                    .HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(l => l.DomainEvents);
                entity.Ignore(l => l.Location);
                entity.Ignore(l => l.Dealer);
                entity.Ignore(l => l.DealerEntity);
                entity.Ignore(l => l.DealerId);
            });

            modelBuilder.Entity<ReviewItem>(entity =>
            {
                entity.HasKey(r => r.ReviewItemId);
                entity.Property(r => r.ReviewItemId)
                    .HasConversion(id => id.Value, value => new ReviewItemId(value));
                entity.Property(r => r.Listing1Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));
                entity.Property(r => r.Listing2Id)
                    .HasConversion(id => id.Value, value => new ListingId(value));
                entity.Ignore(r => r.DomainEvents);
            });

            modelBuilder.Entity<Core.Models.PriceHistoryAggregate.PriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.PriceHistoryId);
                entity.Property(ph => ph.PriceHistoryId)
                    .HasConversion(id => id.Value, value => new Core.Models.PriceHistoryAggregate.PriceHistoryId(value));
                entity.Property(ph => ph.ListingId)
                    .HasConversion(id => id.Value, value => new ListingId(value));
            });

            modelBuilder.Entity<Core.Models.SearchSessionAggregate.SearchSession>(entity =>
            {
                entity.HasKey(ss => ss.SearchSessionId);
                entity.Property(ss => ss.SearchSessionId)
                    .HasConversion(id => id.Value, value => new Core.Models.SearchSessionAggregate.SearchSessionId(value));
            });

            modelBuilder.Entity<Core.Models.SearchProfileAggregate.SearchProfile>(entity =>
            {
                entity.HasKey(sp => sp.SearchProfileId);
                entity.Property(sp => sp.SearchProfileId)
                    .HasConversion(
                        id => id.Value,
                        value => Core.Models.SearchProfileAggregate.SearchProfileId.From(value));
            });

            modelBuilder.Entity<Core.Models.VehicleAggregate.Vehicle>(entity =>
            {
                entity.HasKey(v => v.VehicleId);
                entity.Property(v => v.VehicleId)
                    .HasConversion(id => id.Value, value => new Core.Models.VehicleAggregate.VehicleId(value));
            });

            modelBuilder.Entity<Core.Models.WatchListAggregate.WatchedListing>(entity =>
            {
                entity.HasKey(w => w.WatchedListingId);
                entity.Property(w => w.WatchedListingId).HasConversion(id => id.Value, value => new Core.Models.WatchListAggregate.WatchedListingId(value));
                entity.Property(w => w.ListingId).HasConversion(id => id.Value, value => new ListingId(value));
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

            modelBuilder.Entity<Core.Models.DealerAggregate.Dealer>(entity =>
            {
                entity.HasKey(d => d.DealerId);
                entity.Property(d => d.DealerId).HasConversion(id => id.Value, value => new Core.Models.DealerAggregate.DealerId(value));
            });

            modelBuilder.Entity<Core.Models.ScraperHealthAggregate.ScraperHealthRecord>(entity =>
            {
                entity.HasKey(sh => sh.ScraperHealthRecordId);
                entity.Property(sh => sh.ScraperHealthRecordId).HasConversion(id => id.Value, value => new Core.Models.ScraperHealthAggregate.ScraperHealthRecordId(value));
            });
        }
    }
}
