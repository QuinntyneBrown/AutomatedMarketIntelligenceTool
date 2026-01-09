using System.Linq.Expressions;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services;

public class AutoCompleteServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<ILogger<AutoCompleteService>> _loggerMock;
    private readonly AutoCompleteService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AutoCompleteServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _loggerMock = new Mock<ILogger<AutoCompleteService>>();
        _service = new AutoCompleteService(_contextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AutoCompleteService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AutoCompleteService(_contextMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task GetMakeSuggestionsAsync_WithNoListings_ReturnsEmptyList()
    {
        // Arrange
        var listings = new List<Listing>().AsQueryable();
        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetMakeSuggestionsAsync(_tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMakeSuggestionsAsync_WithListings_ReturnsDistinctMakes()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListing(_tenantId, "Toyota", "Camry"),
            CreateListing(_tenantId, "Toyota", "Corolla"),
            CreateListing(_tenantId, "Honda", "Civic"),
            CreateListing(_tenantId, "Honda", "Accord")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetMakeSuggestionsAsync(_tenantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Toyota");
        result.Should().Contain("Honda");
    }

    [Fact]
    public async Task GetMakeSuggestionsAsync_WithPrefix_FiltersResults()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListing(_tenantId, "Toyota", "Camry"),
            CreateListing(_tenantId, "Tesla", "Model 3"),
            CreateListing(_tenantId, "Honda", "Civic")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetMakeSuggestionsAsync(_tenantId, "To");

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Toyota");
    }

    [Fact]
    public async Task GetModelSuggestionsAsync_WithMake_FiltersByMake()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListing(_tenantId, "Toyota", "Camry"),
            CreateListing(_tenantId, "Toyota", "Corolla"),
            CreateListing(_tenantId, "Honda", "Civic")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetModelSuggestionsAsync(_tenantId, make: "Toyota");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Camry");
        result.Should().Contain("Corolla");
        result.Should().NotContain("Civic");
    }

    [Fact]
    public async Task GetModelSuggestionsAsync_WithPrefix_FiltersResults()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListing(_tenantId, "Toyota", "Camry"),
            CreateListing(_tenantId, "Toyota", "Corolla"),
            CreateListing(_tenantId, "Honda", "Civic")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetModelSuggestionsAsync(_tenantId, make: "Toyota", prefix: "Ca");

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Camry");
    }

    [Fact]
    public async Task GetCitySuggestionsAsync_WithListings_ReturnsDistinctCities()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListingWithCity(_tenantId, "Toronto"),
            CreateListingWithCity(_tenantId, "Toronto"),
            CreateListingWithCity(_tenantId, "Vancouver"),
            CreateListingWithCity(_tenantId, "Calgary")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetCitySuggestionsAsync(_tenantId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Toronto");
        result.Should().Contain("Vancouver");
        result.Should().Contain("Calgary");
    }

    [Fact]
    public async Task GetYearRangeAsync_WithNoListings_ReturnsDefaultRange()
    {
        // Arrange
        var listings = new List<Listing>().AsQueryable();
        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetYearRangeAsync(_tenantId);

        // Assert
        var currentYear = DateTime.UtcNow.Year;
        result.MinYear.Should().Be(currentYear - 10);
        result.MaxYear.Should().Be(currentYear + 1);
    }

    [Fact]
    public async Task GetYearRangeAsync_WithListings_ReturnsActualRange()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListingWithYear(_tenantId, 2018),
            CreateListingWithYear(_tenantId, 2020),
            CreateListingWithYear(_tenantId, 2023)
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetYearRangeAsync(_tenantId);

        // Assert
        result.MinYear.Should().Be(2018);
        result.MaxYear.Should().Be(2023);
    }

    [Fact]
    public async Task GetPriceRangeAsync_WithNoListings_ReturnsDefaultRange()
    {
        // Arrange
        var listings = new List<Listing>().AsQueryable();
        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetPriceRangeAsync(_tenantId);

        // Assert
        result.MinPrice.Should().Be(0m);
        result.MaxPrice.Should().Be(100000m);
    }

    [Fact]
    public async Task GetPriceRangeAsync_WithListings_ReturnsActualRange()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListingWithPrice(_tenantId, 15000m),
            CreateListingWithPrice(_tenantId, 25000m),
            CreateListingWithPrice(_tenantId, 45000m)
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act
        var result = await _service.GetPriceRangeAsync(_tenantId);

        // Assert
        result.MinPrice.Should().Be(15000m);
        result.MaxPrice.Should().Be(45000m);
    }

    [Fact]
    public async Task GetMakeSuggestionsAsync_IsCaseInsensitive()
    {
        // Arrange
        var listings = new List<Listing>
        {
            CreateListing(_tenantId, "Toyota", "Camry"),
            CreateListing(_tenantId, "TESLA", "Model 3")
        }.AsQueryable();

        var mockDbSet = CreateMockDbSet(listings);
        _contextMock.Setup(c => c.Listings).Returns(mockDbSet.Object);

        // Act - search with lowercase
        var result = await _service.GetMakeSuggestionsAsync(_tenantId, "toy");

        // Assert - should find Toyota (case insensitive match)
        result.Should().HaveCount(1);
        result.Should().Contain("Toyota");
    }

    private static Listing CreateListing(Guid tenantId, string make, string model)
    {
        return Listing.Create(
            tenantId: tenantId,
            externalId: Guid.NewGuid().ToString(),
            sourceSite: "test",
            listingUrl: "http://test.com",
            make: make,
            model: model,
            year: 2020,
            price: 25000m);
    }

    private static Listing CreateListingWithCity(Guid tenantId, string city)
    {
        var listing = CreateListing(tenantId, "Toyota", "Camry");
        listing.UpdateLocation(city, "ON", null, null, null);
        return listing;
    }

    private static Listing CreateListingWithYear(Guid tenantId, int year)
    {
        return Listing.Create(
            tenantId: tenantId,
            externalId: Guid.NewGuid().ToString(),
            sourceSite: "test",
            listingUrl: "http://test.com",
            make: "Toyota",
            model: "Camry",
            year: year,
            price: 25000m);
    }

    private static Listing CreateListingWithPrice(Guid tenantId, decimal price)
    {
        return Listing.Create(
            tenantId: tenantId,
            externalId: Guid.NewGuid().ToString(),
            sourceSite: "test",
            listingUrl: "http://test.com",
            make: "Toyota",
            model: "Camry",
            year: 2020,
            price: price);
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mockSet;
    }
}

// Test helpers for async LINQ operations
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
