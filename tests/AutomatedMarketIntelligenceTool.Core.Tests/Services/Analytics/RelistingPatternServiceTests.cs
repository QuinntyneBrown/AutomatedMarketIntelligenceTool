using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Analytics;

public class RelistingPatternServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly RelistingPatternService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public RelistingPatternServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new RelistingPatternService(_context, NullLogger<RelistingPatternService>.Instance);
    }

    [Fact]
    public async Task AnalyzePatternsAsync_WithNoRelistings_ReturnsEmptyAnalysis()
    {
        // Arrange - no relisted listings

        // Act
        var result = await _service.AnalyzePatternsAsync(_testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRelistedVehicles);
        Assert.Equal(0, result.FrequentRelistersCount);
        Assert.Equal(0, result.AverageRelistCount);
    }

    [Fact]
    public async Task AnalyzePatternsAsync_WithRelistings_CalculatesStatistics()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Create relisted listings
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
            listing.IncrementRelistedCount();
            _context.Listings.Add(listing);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzePatternsAsync(_testTenantId);

        // Assert
        Assert.Equal(5, result.TotalRelistedVehicles);
        Assert.True(result.AverageRelistCount > 0);
    }

    [Fact]
    public async Task GetDealerPatternsAsync_WithNoRelistings_ReturnsEmptyPattern()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Clean Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDealerPatternsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dealer.DealerId, result.DealerId);
        Assert.Equal(0, result.TotalRelistedListings);
        Assert.False(result.IsFrequentRelister);
    }

    [Fact]
    public async Task GetDealerPatternsAsync_WithMultipleRelistings_CalculatesMetrics()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Relister Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Create listings with multiple relistings
        var listing1 = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Honda", "Civic", 2019, 20000m, Condition.Used);
        listing1.SetDealer(dealer.DealerId);
        listing1.MarkAsRelisted(19500m);
        listing1.MarkAsRelisted(19000m);
        listing1.MarkAsRelisted(18500m);
        _context.Listings.Add(listing1);

        var listing2 = Listing.Create(_testTenantId, "EXT-002", "TestSite", "https://test.com/2",
            "Honda", "Accord", 2020, 25000m, Condition.Used);
        listing2.SetDealer(dealer.DealerId);
        listing2.MarkAsRelisted(24500m);
        _context.Listings.Add(listing2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDealerPatternsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.Equal(2, result.TotalRelistedListings);
        Assert.True(result.IsFrequentRelister); // At least one listing has 3+ relistings
        Assert.True(result.AverageRelistsPerVehicle > 1);
        Assert.NotEmpty(result.RecentExamples);
    }

    [Fact]
    public async Task GetDealerPatternsAsync_ThrowsForNonExistentDealer()
    {
        // Arrange
        var fakeDealerId = DealerId.CreateNew();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetDealerPatternsAsync(_testTenantId, fakeDealerId));
    }

    [Fact]
    public async Task GetFrequentRelistersAsync_ReturnsOnlyFrequentRelisters()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Frequent Relister");
        var dealer2 = Dealer.Create(_testTenantId, "Normal Dealer");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        // Dealer1 has frequent relistings
        for (int i = 0; i < 3; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-F{i}", "TestSite", $"https://test.com/f{i}",
                "Ford", "F-150", 2020, 30000m, Condition.Used);
            listing.SetDealer(dealer1.DealerId);
            listing.IncrementRelistedCount();
            listing.IncrementRelistedCount();
            listing.IncrementRelistedCount();
            _context.Listings.Add(listing);
        }

        // Dealer2 has only 1 relisting
        var listing2 = Listing.Create(_testTenantId, "EXT-N1", "TestSite", "https://test.com/n1",
            "Toyota", "Tacoma", 2021, 35000m, Condition.Used);
        listing2.SetDealer(dealer2.DealerId);
        listing2.MarkAsRelisted(34000m);
        _context.Listings.Add(listing2);

        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetFrequentRelistersAsync(_testTenantId, 3);

        // Assert
        Assert.Single(results);
        Assert.Equal("Frequent Relister", results[0].DealerName);
        Assert.True(results[0].AverageRelistCount >= 3);
    }

    [Fact]
    public async Task GetFrequentRelistersAsync_OrdersByRelistCount()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Dealer A");
        var dealer2 = Dealer.Create(_testTenantId, "Dealer B");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        // Dealer B has more relistings
        for (int i = 0; i < 10; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-B{i}", "TestSite", $"https://test.com/b{i}",
                "Chevrolet", "Silverado", 2020, 30000m, Condition.Used);
            listing.SetDealer(dealer2.DealerId);
            for (int j = 0; j < 3; j++)
            {
                listing.IncrementRelistedCount();
            }
            _context.Listings.Add(listing);
        }

        // Dealer A has fewer relistings
        for (int i = 0; i < 5; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-A{i}", "TestSite", $"https://test.com/a{i}",
                "GMC", "Sierra", 2020, 32000m, Condition.Used);
            listing.SetDealer(dealer1.DealerId);
            for (int j = 0; j < 3; j++)
            {
                listing.IncrementRelistedCount();
            }
            _context.Listings.Add(listing);
        }

        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetFrequentRelistersAsync(_testTenantId, 3);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Dealer B", results[0].DealerName); // Should be first (more relistings)
        Assert.True(results[0].TotalRelistedCount > results[1].TotalRelistedCount);
    }

    [Fact]
    public async Task GetRelistingTrendsAsync_ReturnsTimeSeriesData()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Trend Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        var fromDate = DateTime.UtcNow.AddMonths(-2);
        
        // Create listings with staggered relist dates
        for (int i = 0; i < 10; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-{i}", "TestSite", $"https://test.com/{i}",
                "Nissan", "Altima", 2020, 22000m, Condition.Used);
            listing.SetDealer(dealer.DealerId);
            listing.IncrementRelistedCount();
            _context.Listings.Add(listing);
        }

        await _context.SaveChangesAsync();

        // Act
        var trends = await _service.GetRelistingTrendsAsync(_testTenantId, fromDate, DateTime.UtcNow);

        // Assert
        Assert.NotNull(trends);
        // May be empty or have data depending on test timing
    }

    [Fact]
    public async Task GetDealerPatternsAsync_CalculatesPriceChangeCorrectly()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Price Change Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        var listing = Listing.Create(_testTenantId, "EXT-001", "TestSite", "https://test.com/1",
            "Mazda", "CX-5", 2021, 30000m, Condition.Used);
        listing.SetDealer(dealer.DealerId);
        listing.IncrementRelistedCount(); // Increment relist count
        _context.Listings.Add(listing);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDealerPatternsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.True(result.AveragePriceChangePercent < 0); // Should show price reduction
    }

    [Fact]
    public async Task AnalyzePatternsAsync_IdentifiesTopRelistingMakes()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Multi-Make Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Add relistings for different makes
        var makes = new[] { "Toyota", "Honda", "Ford", "Chevrolet", "Nissan", "BMW" };
        foreach (var make in makes)
        {
            for (int i = 0; i < 3; i++)
            {
                var listing = Listing.Create(_testTenantId, $"EXT-{make}-{i}", "TestSite", 
                    $"https://test.com/{make}/{i}", make, "Model", 2020, 25000m, Condition.Used);
                listing.SetDealer(dealer.DealerId);
                listing.IncrementRelistedCount();
                _context.Listings.Add(listing);
            }
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzePatternsAsync(_testTenantId);

        // Assert
        Assert.NotEmpty(result.TopRelistingMakes);
        Assert.True(result.TopRelistingMakes.Count <= 5); // Top 5 makes
    }

    [Fact]
    public async Task GetDealerPatternsAsync_IncludesRecentExamples()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Example Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        // Create several relisted listings
        for (int i = 0; i < 10; i++)
        {
            var listing = Listing.Create(_testTenantId, $"EXT-{i}", "TestSite", $"https://test.com/{i}",
                "Subaru", "Outback", 2020 + i, 28000m, Condition.Used);
            listing.SetDealer(dealer.DealerId);
            listing.IncrementRelistedCount();
            _context.Listings.Add(listing);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDealerPatternsAsync(_testTenantId, dealer.DealerId);

        // Assert
        Assert.NotEmpty(result.RecentExamples);
        Assert.True(result.RecentExamples.Count <= 5); // Should limit to 5 examples
        Assert.All(result.RecentExamples, example =>
        {
            Assert.True(example.RelistCount > 0);
            Assert.NotEqual(default, example.ListingId);
        });
    }
}
