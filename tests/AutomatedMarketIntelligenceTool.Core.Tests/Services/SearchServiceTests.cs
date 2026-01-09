using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class SearchServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly SearchService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public SearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new SearchService(_context, NullLogger<SearchService>.Instance);
    }

    [Fact]
    public async Task SearchListingsAsync_WithNoFilters_ShouldReturnAllListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.NotEmpty(result.Listings);
    }

    [Fact]
    public async Task SearchListingsAsync_WithMakeFilter_ShouldReturnMatchingListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Makes = new[] { "Toyota" },
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.Equal("Toyota", l.Listing.Make));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMakeFilterCaseInsensitive_ShouldReturnMatchingListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Makes = new[] { "toyota" }, // lowercase
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.All(result.Listings, l => Assert.Equal("Toyota", l.Listing.Make));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMultipleMakes_ShouldReturnAllMatches()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Makes = new[] { "Toyota", "Honda" },
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.All(result.Listings, l => 
            Assert.Contains(l.Listing.Make, new[] { "Toyota", "Honda" }));
    }

    [Fact]
    public async Task SearchListingsAsync_WithModelFilter_ShouldReturnMatchingListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Models = new[] { "Camry" },
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.Equal("Camry", l.Listing.Model));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMakeAndModelFilter_ShouldReturnMatchingListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Makes = new[] { "Toyota" },
            Models = new[] { "Camry" },
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l =>
        {
            Assert.Equal("Toyota", l.Listing.Make);
            Assert.Equal("Camry", l.Listing.Model);
        });
    }

    [Fact]
    public async Task SearchListingsAsync_WithYearMinFilter_ShouldReturnListingsAfterYear()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            YearMin = 2020,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.True(l.Listing.Year >= 2020));
    }

    [Fact]
    public async Task SearchListingsAsync_WithYearMaxFilter_ShouldReturnListingsBeforeYear()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            YearMax = 2020,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.True(l.Listing.Year <= 2020));
    }

    [Fact]
    public async Task SearchListingsAsync_WithYearRange_ShouldReturnListingsInRange()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            YearMin = 2019,
            YearMax = 2021,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => 
            Assert.True(l.Listing.Year >= 2019 && l.Listing.Year <= 2021));
    }

    [Fact]
    public async Task SearchListingsAsync_WithPriceMinFilter_ShouldReturnListingsAbovePrice()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            PriceMin = 25000m,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.True(l.Listing.Price >= 25000m));
    }

    [Fact]
    public async Task SearchListingsAsync_WithPriceMaxFilter_ShouldReturnListingsBelowPrice()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            PriceMax = 30000m,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => Assert.True(l.Listing.Price <= 30000m));
    }

    [Fact]
    public async Task SearchListingsAsync_WithPriceRange_ShouldReturnListingsInRange()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            PriceMin = 20000m,
            PriceMax = 30000m,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => 
            Assert.True(l.Listing.Price >= 20000m && l.Listing.Price <= 30000m));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMileageMinFilter_ShouldReturnListingsAboveMileage()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            MileageMin = 20000,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => 
            Assert.True(l.Listing.Mileage.HasValue && l.Listing.Mileage.Value >= 20000));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMileageMaxFilter_ShouldReturnListingsBelowMileage()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            MileageMax = 50000,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => 
            Assert.True(l.Listing.Mileage.HasValue && l.Listing.Mileage.Value <= 50000));
    }

    [Fact]
    public async Task SearchListingsAsync_WithMileageRange_ShouldReturnListingsInRange()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            MileageMin = 10000,
            MileageMax = 50000,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l => 
            Assert.True(l.Listing.Mileage.HasValue && 
                       l.Listing.Mileage.Value >= 10000 && 
                       l.Listing.Mileage.Value <= 50000));
    }

    [Fact]
    public async Task SearchListingsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.Listings.Count <= 2);
    }

    [Fact]
    public async Task SearchListingsAsync_WithSecondPage_ShouldReturnNextResults()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteriaPage1 = new SearchCriteria
        {
            TenantId = _testTenantId,
            Page = 1,
            PageSize = 2
        };

        var criteriaPage2 = new SearchCriteria
        {
            TenantId = _testTenantId,
            Page = 2,
            PageSize = 2
        };

        // Act
        var resultPage1 = await _service.SearchListingsAsync(criteriaPage1);
        var resultPage2 = await _service.SearchListingsAsync(criteriaPage2);

        // Assert
        Assert.NotNull(resultPage1);
        Assert.NotNull(resultPage2);
        Assert.Equal(2, resultPage2.Page);
        
        if (resultPage1.Listings.Any() && resultPage2.Listings.Any())
        {
            // Ensure page 2 has different listings than page 1
            var page1Ids = resultPage1.Listings.Select(l => l.Listing.ListingId.Value).ToList();
            var page2Ids = resultPage2.Listings.Select(l => l.Listing.ListingId.Value).ToList();
            Assert.Empty(page1Ids.Intersect(page2Ids));
        }
    }

    [Fact]
    public async Task SearchListingsAsync_SortByPriceAscending_ShouldReturnListingsInOrder()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            SortBy = SearchSortField.Price,
            SortDirection = SortDirection.Ascending,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Listings.Count > 1);
        
        for (int i = 1; i < result.Listings.Count; i++)
        {
            Assert.True(result.Listings[i].Listing.Price >= result.Listings[i - 1].Listing.Price);
        }
    }

    [Fact]
    public async Task SearchListingsAsync_SortByPriceDescending_ShouldReturnListingsInOrder()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            SortBy = SearchSortField.Price,
            SortDirection = SortDirection.Descending,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Listings.Count > 1);
        
        for (int i = 1; i < result.Listings.Count; i++)
        {
            Assert.True(result.Listings[i].Listing.Price <= result.Listings[i - 1].Listing.Price);
        }
    }

    [Fact]
    public async Task SearchListingsAsync_SortByYear_ShouldReturnListingsInOrder()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            SortBy = SearchSortField.Year,
            SortDirection = SortDirection.Descending,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Listings.Count > 1);
        
        for (int i = 1; i < result.Listings.Count; i++)
        {
            Assert.True(result.Listings[i].Listing.Year <= result.Listings[i - 1].Listing.Year);
        }
    }

    [Fact]
    public async Task SearchListingsAsync_WithDifferentTenant_ShouldReturnNoResults()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = Guid.NewGuid(), // Different tenant
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Listings);
    }

    [Fact]
    public async Task SearchListingsAsync_WithNullCriteria_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.SearchListingsAsync(null!));
    }

    [Fact]
    public async Task SearchListingsAsync_TotalPages_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            PageSize = 2
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        var expectedTotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize);
        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Fact]
    public async Task SearchListingsAsync_WithComplexFilter_ShouldReturnMatchingListings()
    {
        // Arrange
        await SeedTestDataAsync();

        var criteria = new SearchCriteria
        {
            TenantId = _testTenantId,
            Makes = new[] { "Toyota" },
            YearMin = 2020,
            PriceMax = 30000m,
            MileageMax = 50000,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchListingsAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Listings, l =>
        {
            Assert.Equal("Toyota", l.Listing.Make);
            Assert.True(l.Listing.Year >= 2020);
            Assert.True(l.Listing.Price <= 30000m);
            if (l.Listing.Mileage.HasValue)
            {
                Assert.True(l.Listing.Mileage.Value <= 50000);
            }
        });
    }

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

    private class TestContext : DbContext, IAutomatedMarketIntelligenceToolContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Core.Models.PriceHistoryAggregate.PriceHistory> PriceHistory => Set<Core.Models.PriceHistoryAggregate.PriceHistory>();
        public DbSet<Core.Models.SearchSessionAggregate.SearchSession> SearchSessions => Set<Core.Models.SearchSessionAggregate.SearchSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Listing entity
            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(l => l.ListingId);
                
                entity.Property(l => l.ListingId)
                    .HasConversion(
                        id => id.Value,
                        value => new ListingId(value));

                entity.Ignore(l => l.DomainEvents);
            });
        }
    }
}
