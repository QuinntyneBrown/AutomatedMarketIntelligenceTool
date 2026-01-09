using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Analytics;

public class DealerAnalyticsServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly DealerAnalyticsService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DealerAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new DealerAnalyticsService(_context, NullLogger<DealerAnalyticsService>.Instance);
    }

    [Fact]
    public async Task AnalyzeDealerAsync_WithNoListings_ReturnsEmptyAnalytics()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer", "City", "CA");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dealer.DealerId, result.DealerId);
        Assert.Equal(dealer.Name, result.DealerName);
        Assert.Equal(0, result.TotalListingsHistorical);
        Assert.Equal(0, result.ActiveListings);
        Assert.Equal(0, result.RelistedCount);
        Assert.False(result.FrequentRelisterFlag);
    }

    [Fact]
    public async Task AnalyzeDealerAsync_WithActiveListings_CalculatesCorrectMetrics()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Premium Motors", "Los Angeles", "CA");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Add 5 active listings
        for (int i = 0; i < 5; i++)
        {
            var listing = Listing.Create(
                _testTenantId,
                $"EXT-{i}",
                "TestSite",
                $"https://test.com/{i}",
                "Toyota",
                "Camry",
                2020,
                25000m,
                Condition.Used);
            listing.SetDealer(dealer.DealerId);
            _context.Listings.Add(listing);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalListingsHistorical);
        Assert.Equal(5, result.ActiveListings);
        Assert.Equal(0, result.RelistedCount);
        Assert.True(result.ReliabilityScore > 0);
    }

    [Fact]
    public async Task AnalyzeDealerAsync_WithRelistedVehicles_FlagsFrequentRelister()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Relister Auto");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Create a listing that was relisted 3 times
        var listing = Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Honda",
            "Civic",
            2019,
            20000m,
            Condition.Used);
        listing.SetDealer(dealer.DealerId);
        
        // Simulate multiple relistings
        for (int i = 0; i < 3; i++)
        {
            listing.MarkAsRelisted(19500m - (i * 500));
        }
        
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.True(result.FrequentRelisterFlag);
        Assert.Equal(1, result.RelistedCount);
        Assert.True(result.ReliabilityScore < 100); // Should have penalty
    }

    [Fact]
    public async Task AnalyzeDealerAsync_CalculatesAvgDaysOnMarket()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Quick Sell Motors");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Add listings with deactivation dates
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Ford", "F-150", 2020, 30000m, Condition.Used);
        listing1.SetDealer(dealer.DealerId);
        listing1.Deactivate();
        _context.Listings.Add(listing1);

        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Ford", "Mustang", 2021, 35000m, Condition.Used);
        listing2.SetDealer(dealer.DealerId);
        listing2.Deactivate();
        _context.Listings.Add(listing2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AvgDaysOnMarket >= 0);
    }

    [Fact]
    public async Task AnalyzeAllDealersAsync_ProcessesAllDealers()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Dealer One");
        var dealer2 = Dealer.Create(_testTenantId, "Dealer Two");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.AnalyzeAllDealersAsync(_testTenantId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.DealerName == "Dealer One");
        Assert.Contains(results, r => r.DealerName == "Dealer Two");
    }

    [Fact]
    public async Task GetDealerAnalyticsAsync_ReturnsNullForNonAnalyzedDealer()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "New Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDealerAnalyticsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDealerAnalyticsAsync_ReturnsCachedAnalytics()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Analyzed Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Analyze first
        await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Act
        var result = await _service.GetDealerAnalyticsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dealer.DealerId, result.DealerId);
        Assert.NotNull(result.LastAnalyzedAt);
    }

    [Fact]
    public async Task GetInventoryHistoryAsync_ReturnsHistoricalSnapshots()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "History Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Add listings with different dates
        var fromDate = DateTime.UtcNow.AddMonths(-2);
        for (int i = 0; i < 3; i++)
        {
            var listing = Listing.Create(
                _testTenantId,
                $"EXT-{i}",
                "TestSite",
                $"https://test.com/{i}",
                "Toyota",
                "Corolla",
                2020,
                20000m,
                Condition.Used);
            listing.SetDealer(dealer.DealerId);
            _context.Listings.Add(listing);
        }
        await _context.SaveChangesAsync();

        // Act
        var history = await _service.GetInventoryHistoryAsync(
            _testTenantId,
            dealer.DealerId,
            fromDate,
            DateTime.UtcNow);

        // Assert
        Assert.NotNull(history);
        // History will be empty or minimal since listings are all recent
    }

    [Fact]
    public async Task GetLowReliabilityDealersAsync_ReturnsOnlyBelowThreshold()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Good Dealer");
        dealer1.UpdateAnalytics(90m, 30, 10, false);
        _context.Dealers.Add(dealer1);

        var dealer2 = Dealer.Create(_testTenantId, "Poor Dealer");
        dealer2.UpdateAnalytics(40m, 90, 5, true);
        _context.Dealers.Add(dealer2);

        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetLowReliabilityDealersAsync(_testTenantId, 50m);

        // Assert
        Assert.Single(results);
        Assert.Equal("Poor Dealer", results[0].DealerName);
        Assert.Equal(40m, results[0].ReliabilityScore);
    }

    [Fact]
    public async Task AnalyzeDealerAsync_UpdatesDealerModel()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Update Test Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        var listing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "BMW", "3 Series", 2022, 45000m, Condition.Used);
        listing.SetDealer(dealer.DealerId);
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        // Act
        await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert - reload dealer and check updates
        var updatedDealer = await _context.Dealers.FirstOrDefaultAsync(d => d.DealerId == dealer.DealerId);
        Assert.NotNull(updatedDealer);
        Assert.NotNull(updatedDealer.ReliabilityScore);
        Assert.NotNull(updatedDealer.LastAnalyzedAt);
        Assert.Equal(1, updatedDealer.TotalListingsHistorical);
    }

    [Fact]
    public async Task AnalyzeDealerAsync_ThrowsForNonExistentDealer()
    {
        // Arrange
        var fakeDealerId = DealerId.CreateNew();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AnalyzeDealerAsync(_testTenantId, fakeDealerId));
    }

    [Fact]
    public async Task AnalyzeDealerAsync_CalculatesReliabilityScoreProperly()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Score Test Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Add 10 listings, 8 active, 2 relisted
        for (int i = 0; i < 8; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-{i}", "TestSite", $"https://test.com/{i}",
                "Honda", "Accord", 2020, 25000m, Condition.Used);
            listing.SetDealer(dealer.DealerId);
            _context.Listings.Add(listing);
        }

        for (int i = 8; i < 10; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-{i}", "TestSite", $"https://test.com/{i}",
                "Honda", "Accord", 2020, 25000m, Condition.Used);
            listing.SetDealer(dealer.DealerId);
            listing.MarkAsRelisted(24000m);
            _context.Listings.Add(listing);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeDealerAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.InRange(result.ReliabilityScore, 0, 100);
        Assert.True(result.ReliabilityScore > 50); // Should be good dealer
        Assert.Equal(2, result.RelistedCount);
    }
}
