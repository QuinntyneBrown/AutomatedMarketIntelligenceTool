using System.Linq.Expressions;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScrapedListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Deduplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Deduplication;

public class BatchDeduplicationServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<IDuplicateDetectionService> _duplicateDetectionServiceMock;
    private readonly Mock<ILogger<BatchDeduplicationService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public BatchDeduplicationServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _duplicateDetectionServiceMock = new Mock<IDuplicateDetectionService>();
        _loggerMock = new Mock<ILogger<BatchDeduplicationService>>();
    }

    private BatchDeduplicationService CreateService()
    {
        return new BatchDeduplicationService(
            _contextMock.Object,
            _duplicateDetectionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationService(null!, _duplicateDetectionServiceMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenDuplicateDetectionServiceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationService(_contextMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationService(_contextMock.Object, _duplicateDetectionServiceMock.Object, null!));
    }

    [Fact]
    public async Task ProcessBatchAsync_WithEmptyList_ReturnsEmptyResult()
    {
        var service = CreateService();
        var emptyList = new List<ScrapedListing>();

        var result = await service.ProcessBatchAsync(emptyList, _tenantId);

        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.NewListingCount);
        Assert.Equal(0, result.DuplicateCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_ReportsProgressCorrectly()
    {
        var service = CreateService();
        var listings = CreateTestListings(10);
        var progressReports = new List<BatchProgress>();
        var progress = new Progress<BatchProgress>(p => progressReports.Add(p));

        SetupEmptyListingsDbSet();

        await service.ProcessBatchAsync(listings, _tenantId, progress: progress);

        // Give time for progress reports to be processed
        await Task.Delay(100);

        Assert.NotEmpty(progressReports);
        Assert.Equal(10, progressReports.Last().Total);
    }

    [Fact]
    public async Task ProcessBatchAsync_SetsProcessingTime()
    {
        var service = CreateService();
        var listings = CreateTestListings(5);

        SetupEmptyListingsDbSet();

        var result = await service.ProcessBatchAsync(listings, _tenantId);

        Assert.True(result.ProcessingTimeMs > 0);
    }

    [Fact]
    public async Task ProcessBatchAsync_CalculatesAverageTimeCorrectly()
    {
        var service = CreateService();
        var listings = CreateTestListings(5);

        SetupEmptyListingsDbSet();

        var result = await service.ProcessBatchAsync(listings, _tenantId);

        Assert.True(result.AverageTimePerListingMs >= 0);
    }

    private List<ScrapedListing> CreateTestListings(int count)
    {
        var listings = new List<ScrapedListing>();
        for (int i = 0; i < count; i++)
        {
            listings.Add(new ScrapedListing
            {
                ExternalId = $"ext-{i}",
                SourceSite = "TestSite",
                Make = "Toyota",
                Model = "Camry",
                Year = 2020 + (i % 5),
                Price = 25000 + (i * 1000),
                Mileage = 30000 + (i * 5000),
                City = "Toronto",
                ListingUrl = $"https://example.com/listing/{i}",
                Condition = Condition.Used
            });
        }
        return listings;
    }

    private void SetupEmptyListingsDbSet()
    {
        var listings = new List<Listing>().AsQueryable();

        var mockSet = new Mock<DbSet<Listing>>();
        mockSet.As<IAsyncEnumerable<Listing>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<Listing>(listings.GetEnumerator()));

        mockSet.As<IQueryable<Listing>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<Listing>(listings.Provider));

        mockSet.As<IQueryable<Listing>>().Setup(m => m.Expression).Returns(listings.Expression);
        mockSet.As<IQueryable<Listing>>().Setup(m => m.ElementType).Returns(listings.ElementType);
        mockSet.As<IQueryable<Listing>>().Setup(m => m.GetEnumerator()).Returns(listings.GetEnumerator());

        _contextMock.Setup(c => c.Listings).Returns(mockSet.Object);
    }
}

public class CandidateBlockIndexTests
{
    [Fact]
    public void Constructor_WithEmptyCandidates_CreatesEmptyIndex()
    {
        var index = new CandidateBlockIndex(Enumerable.Empty<CandidateListing>());

        Assert.Equal(0, index.TotalCandidates);
        Assert.Equal(0, index.BlockCount);
    }

    [Fact]
    public void Constructor_WithCandidates_CreatesBlocks()
    {
        var candidates = new List<CandidateListing>
        {
            new() { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() },
            new() { Make = "Toyota", Year = 2021, ListingId = Guid.NewGuid() },
            new() { Make = "Honda", Year = 2020, ListingId = Guid.NewGuid() }
        };

        var index = new CandidateBlockIndex(candidates);

        Assert.Equal(3, index.TotalCandidates);
        Assert.Equal(3, index.BlockCount);
    }

    [Fact]
    public void GetCandidates_WithNullMake_ReturnsEmpty()
    {
        var index = new CandidateBlockIndex();

        var result = index.GetCandidates(null, 2020);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCandidates_WithEmptyMake_ReturnsEmpty()
    {
        var index = new CandidateBlockIndex();

        var result = index.GetCandidates("", 2020);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCandidates_ReturnsMatchingCandidates()
    {
        var candidates = new List<CandidateListing>
        {
            new() { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() },
            new() { Make = "Toyota", Year = 2021, ListingId = Guid.NewGuid() },
            new() { Make = "Honda", Year = 2020, ListingId = Guid.NewGuid() }
        };

        var index = new CandidateBlockIndex(candidates);

        var result = index.GetCandidates("Toyota", 2020).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("Toyota", c.Make));
    }

    [Fact]
    public void GetCandidates_IncludesYearRange()
    {
        var candidates = new List<CandidateListing>
        {
            new() { Make = "Toyota", Year = 2018, ListingId = Guid.NewGuid() },
            new() { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() },
            new() { Make = "Toyota", Year = 2022, ListingId = Guid.NewGuid() }
        };

        var index = new CandidateBlockIndex(candidates);

        var result = index.GetCandidates("Toyota", 2020).ToList();

        Assert.Equal(3, result.Count); // Within ±2 year range
    }

    [Fact]
    public void GetCandidates_IsCaseInsensitive()
    {
        var candidates = new List<CandidateListing>
        {
            new() { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() }
        };

        var index = new CandidateBlockIndex(candidates);

        var result = index.GetCandidates("TOYOTA", 2020).ToList();

        Assert.Single(result);
    }

    [Fact]
    public void AddCandidate_AddsToIndex()
    {
        var index = new CandidateBlockIndex();
        var candidate = new CandidateListing
        {
            Make = "Toyota",
            Year = 2020,
            ListingId = Guid.NewGuid()
        };

        index.AddCandidate(candidate);

        Assert.Equal(1, index.TotalCandidates);
        Assert.Equal(1, index.BlockCount);
    }

    [Fact]
    public void AddCandidate_MultipleSameMakeYear_AddsToSameBlock()
    {
        var index = new CandidateBlockIndex();

        index.AddCandidate(new CandidateListing { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() });
        index.AddCandidate(new CandidateListing { Make = "Toyota", Year = 2020, ListingId = Guid.NewGuid() });

        Assert.Equal(2, index.TotalCandidates);
        Assert.Equal(1, index.BlockCount);
    }
}

public class BatchDeduplicationResultTests
{
    [Fact]
    public void AverageTimePerListingMs_CalculatesCorrectly()
    {
        var result = new BatchDeduplicationResult
        {
            TotalProcessed = 10,
            ProcessingTimeMs = 1000
        };

        Assert.Equal(100, result.AverageTimePerListingMs);
    }

    [Fact]
    public void AverageTimePerListingMs_ReturnsZeroWhenNoProcessed()
    {
        var result = new BatchDeduplicationResult
        {
            TotalProcessed = 0,
            ProcessingTimeMs = 0
        };

        Assert.Equal(0, result.AverageTimePerListingMs);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var result = new BatchDeduplicationResult();

        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.NewListingCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Equal(0, result.NearMatchCount);
        Assert.Equal(0, result.VinMatchCount);
        Assert.Equal(0, result.FuzzyMatchCount);
        Assert.Equal(0, result.ImageMatchCount);
        Assert.Equal(0, result.ProcessingTimeMs);
        Assert.Empty(result.ItemResults);
        Assert.Empty(result.Errors);
    }
}

public class BatchProgressTests
{
    [Fact]
    public void PercentComplete_CalculatesCorrectly()
    {
        var progress = new BatchProgress(50, 100);

        Assert.Equal(50, progress.PercentComplete);
    }

    [Fact]
    public void PercentComplete_ReturnsZeroWhenTotalIsZero()
    {
        var progress = new BatchProgress(0, 0);

        Assert.Equal(0, progress.PercentComplete);
    }

    [Fact]
    public void PercentComplete_Returns100WhenComplete()
    {
        var progress = new BatchProgress(100, 100);

        Assert.Equal(100, progress.PercentComplete);
    }

    [Fact]
    public void Properties_SetCorrectly()
    {
        var progress = new BatchProgress(25, 50) { Message = "Processing..." };

        Assert.Equal(25, progress.Processed);
        Assert.Equal(50, progress.Total);
        Assert.Equal("Processing...", progress.Message);
    }
}

public class BatchItemResultTests
{
    [Fact]
    public void RequiredProperties_MustBeSet()
    {
        var result = new BatchItemResult
        {
            ExternalId = "ext-123",
            SourceSite = "TestSite",
            Result = DuplicateCheckResult.NewListing()
        };

        Assert.Equal("ext-123", result.ExternalId);
        Assert.Equal("TestSite", result.SourceSite);
        Assert.NotNull(result.Result);
    }
}

public class CandidateListingTests
{
    [Fact]
    public void Properties_SetCorrectly()
    {
        var listingId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var candidate = new CandidateListing
        {
            ListingId = listingId,
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 25000,
            Mileage = 30000,
            Vin = "1HGBH41JXMN109186",
            City = "Toronto",
            Latitude = 43.6532m,
            Longitude = -79.3832m,
            ExternalId = "ext-123",
            SourceSite = "TestSite",
            LinkedVehicleId = vehicleId
        };

        Assert.Equal(listingId, candidate.ListingId);
        Assert.Equal("Toyota", candidate.Make);
        Assert.Equal("Camry", candidate.Model);
        Assert.Equal(2020, candidate.Year);
        Assert.Equal(25000, candidate.Price);
        Assert.Equal(30000, candidate.Mileage);
        Assert.Equal("1HGBH41JXMN109186", candidate.Vin);
        Assert.Equal("Toronto", candidate.City);
        Assert.Equal(43.6532m, candidate.Latitude);
        Assert.Equal(-79.3832m, candidate.Longitude);
        Assert.Equal("ext-123", candidate.ExternalId);
        Assert.Equal("TestSite", candidate.SourceSite);
        Assert.Equal(vehicleId, candidate.LinkedVehicleId);
    }
}

// Helper classes for async query testing
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider
    {
        get { return new TestAsyncQueryProvider<T>(this); }
    }
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}
