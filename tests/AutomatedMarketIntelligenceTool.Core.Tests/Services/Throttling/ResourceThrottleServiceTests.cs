using AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Throttling;
using AutomatedMarketIntelligenceTool.Core.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Throttling;

public class ResourceThrottleServiceTests
{
    private readonly InMemoryTestContext _context;
    private readonly ResourceThrottleService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ResourceThrottleServiceTests()
    {
        var options = new DbContextOptionsBuilder<InMemoryTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InMemoryTestContext(options);
        _service = new ResourceThrottleService(_context, NullLogger<ResourceThrottleService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ResourceThrottleService(null!, NullLogger<ResourceThrottleService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ResourceThrottleService(_context, null!));
    }

    [Fact]
    public async Task CreateThrottleAsync_WithValidData_CreatesThrottle()
    {
        // Act
        var throttle = await _service.CreateThrottleAsync(
            _testTenantId,
            ResourceType.ApiRequests,
            1000,
            ThrottleTimeWindow.PerMinute);

        // Assert
        Assert.NotNull(throttle);
        Assert.Equal(ResourceType.ApiRequests, throttle.ResourceType);
        Assert.Equal(1000, throttle.MaxValue);
        Assert.Equal(ThrottleTimeWindow.PerMinute, throttle.TimeWindow);
        Assert.True(throttle.IsEnabled);
    }

    [Fact]
    public async Task CreateThrottleAsync_WithDuplicateResourceType_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 500, ThrottleTimeWindow.PerHour));
    }

    [Fact]
    public async Task GetThrottleAsync_WithExistingId_ReturnsThrottle()
    {
        // Arrange
        var throttle = await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);

        // Act
        var result = await _service.GetThrottleAsync(_testTenantId, throttle.ResourceThrottleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ResourceType.ApiRequests, result.ResourceType);
    }

    [Fact]
    public async Task GetThrottleByTypeAsync_WithExistingType_ReturnsThrottle()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ConcurrentScrapers, 5, ThrottleTimeWindow.Concurrent);

        // Act
        var result = await _service.GetThrottleByTypeAsync(_testTenantId, ResourceType.ConcurrentScrapers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.MaxValue);
    }

    [Fact]
    public async Task GetAllThrottlesAsync_ReturnsAllThrottlesForTenant()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ConcurrentScrapers, 5, ThrottleTimeWindow.Concurrent);

        // Act
        var result = await _service.GetAllThrottlesAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task EnableThrottleAsync_WithExistingThrottle_ReturnsTrue()
    {
        // Arrange
        var throttle = await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);
        await _service.DisableThrottleAsync(_testTenantId, throttle.ResourceThrottleId);

        // Act
        var result = await _service.EnableThrottleAsync(_testTenantId, throttle.ResourceThrottleId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetThrottleAsync(_testTenantId, throttle.ResourceThrottleId);
        Assert.True(updated!.IsEnabled);
    }

    [Fact]
    public async Task DisableThrottleAsync_WithExistingThrottle_ReturnsTrue()
    {
        // Arrange
        var throttle = await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);

        // Act
        var result = await _service.DisableThrottleAsync(_testTenantId, throttle.ResourceThrottleId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetThrottleAsync(_testTenantId, throttle.ResourceThrottleId);
        Assert.False(updated!.IsEnabled);
    }

    [Fact]
    public async Task TryAcquireAsync_WithAvailableCapacity_ReturnsAllowed()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 10, ThrottleTimeWindow.PerMinute);

        // Act
        var result = await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(1, result.CurrentUsage);
    }

    [Fact]
    public async Task TryAcquireAsync_WithNoCapacity_ReturnsRejected()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 2, ThrottleTimeWindow.PerMinute);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests);

        // Act
        var result = await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(2, result.CurrentUsage);
        Assert.Equal(2, result.MaxValue);
    }

    [Fact]
    public async Task TryAcquireAsync_WithNoThrottle_ReturnsAllowed()
    {
        // Act
        var result = await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal("No throttle configured for this resource type", result.Message);
    }

    [Fact]
    public async Task ReleaseAsync_DecrementsUsage()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ConcurrentScrapers, 5, ThrottleTimeWindow.Concurrent);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ConcurrentScrapers);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ConcurrentScrapers);

        // Act
        await _service.ReleaseAsync(_testTenantId, ResourceType.ConcurrentScrapers);

        // Assert
        var throttle = await _service.GetThrottleByTypeAsync(_testTenantId, ResourceType.ConcurrentScrapers);
        Assert.Equal(1, throttle!.CurrentUsage);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsStatusForAllThrottles()
    {
        // Arrange
        await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests, 50);

        // Act
        var statuses = await _service.GetStatusAsync(_testTenantId);

        // Assert
        Assert.Single(statuses);
        Assert.Equal(50, statuses[0].CurrentUsage);
        Assert.Equal(100, statuses[0].MaxValue);
        Assert.Equal(50, statuses[0].UsagePercent);
    }

    [Fact]
    public async Task ResetUsageAsync_ResetsUsageToZero()
    {
        // Arrange
        var throttle = await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        await _service.TryAcquireAsync(_testTenantId, ResourceType.ApiRequests, 50);

        // Act
        var result = await _service.ResetUsageAsync(_testTenantId, throttle.ResourceThrottleId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetThrottleAsync(_testTenantId, throttle.ResourceThrottleId);
        Assert.Equal(0, updated!.CurrentUsage);
    }

    [Fact]
    public async Task InitializeDefaultThrottlesAsync_CreatesDefaultThrottles()
    {
        // Act
        await _service.InitializeDefaultThrottlesAsync(_testTenantId);

        // Assert
        var throttles = await _service.GetAllThrottlesAsync(_testTenantId);
        Assert.True(throttles.Count >= 4); // At least API, Concurrent, Reports, Database
    }

    [Fact]
    public async Task DeleteThrottleAsync_WithExistingThrottle_ReturnsTrue()
    {
        // Arrange
        var throttle = await _service.CreateThrottleAsync(_testTenantId, ResourceType.ApiRequests, 1000, ThrottleTimeWindow.PerMinute);

        // Act
        var result = await _service.DeleteThrottleAsync(_testTenantId, throttle.ResourceThrottleId);

        // Assert
        Assert.True(result);
        var deleted = await _service.GetThrottleAsync(_testTenantId, throttle.ResourceThrottleId);
        Assert.Null(deleted);
    }
}

public class ResourceThrottleModelTests
{
    [Fact]
    public void Create_WithValidData_CreatesThrottle()
    {
        // Act
        var throttle = ResourceThrottle.Create(
            Guid.NewGuid(),
            ResourceType.ApiRequests,
            1000,
            ThrottleTimeWindow.PerMinute);

        // Assert
        Assert.NotNull(throttle);
        Assert.Equal(ResourceType.ApiRequests, throttle.ResourceType);
        Assert.Equal(1000, throttle.MaxValue);
        Assert.True(throttle.IsEnabled);
        Assert.Equal(0, throttle.CurrentUsage);
    }

    [Fact]
    public void Create_WithZeroMaxValue_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 0, ThrottleTimeWindow.PerMinute));
    }

    [Fact]
    public void Create_WithNegativeMaxValue_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, -1, ThrottleTimeWindow.PerMinute));
    }

    [Fact]
    public void Create_WithInvalidWarningThreshold_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute, warningThresholdPercent: 150));
    }

    [Fact]
    public void TryAcquire_WithAvailableCapacity_ReturnsTrue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 10, ThrottleTimeWindow.PerMinute);

        // Act
        var result = throttle.TryAcquire();

        // Assert
        Assert.True(result);
        Assert.Equal(1, throttle.CurrentUsage);
    }

    [Fact]
    public void TryAcquire_WithNoCapacity_ReturnsFalse()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 2, ThrottleTimeWindow.PerMinute);
        throttle.TryAcquire();
        throttle.TryAcquire();

        // Act
        var result = throttle.TryAcquire();

        // Assert
        Assert.False(result);
        Assert.Equal(2, throttle.CurrentUsage);
    }

    [Fact]
    public void TryAcquire_WhenDisabled_ReturnsTrue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 1, ThrottleTimeWindow.PerMinute);
        throttle.Disable();

        // Act
        var result = throttle.TryAcquire();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Release_DecrementsUsage()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ConcurrentScrapers, 5, ThrottleTimeWindow.Concurrent);
        throttle.TryAcquire();
        throttle.TryAcquire();

        // Act
        throttle.Release();

        // Assert
        Assert.Equal(1, throttle.CurrentUsage);
    }

    [Fact]
    public void Release_DoesNotGoBelowZero()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 5, ThrottleTimeWindow.PerMinute);

        // Act
        throttle.Release();

        // Assert
        Assert.Equal(0, throttle.CurrentUsage);
    }

    [Fact]
    public void GetUsagePercent_ReturnsCorrectPercentage()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        throttle.TryAcquire(50);

        // Act
        var percent = throttle.GetUsagePercent();

        // Assert
        Assert.Equal(50, percent);
    }

    [Fact]
    public void IsAtWarningThreshold_WhenBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute, warningThresholdPercent: 80);
        throttle.TryAcquire(50);

        // Act & Assert
        Assert.False(throttle.IsAtWarningThreshold());
    }

    [Fact]
    public void IsAtWarningThreshold_WhenAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute, warningThresholdPercent: 80);
        throttle.TryAcquire(85);

        // Act & Assert
        Assert.True(throttle.IsAtWarningThreshold());
    }

    [Fact]
    public void IsLimitReached_WhenAtCapacity_ReturnsTrue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 10, ThrottleTimeWindow.PerMinute);
        throttle.TryAcquire(10);

        // Act & Assert
        Assert.True(throttle.IsLimitReached());
    }

    [Fact]
    public void GetRemainingCapacity_ReturnsCorrectValue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        throttle.TryAcquire(30);

        // Act
        var remaining = throttle.GetRemainingCapacity();

        // Assert
        Assert.Equal(70, remaining);
    }

    [Fact]
    public void Enable_SetsIsEnabledToTrue()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        throttle.Disable();

        // Act
        throttle.Enable();

        // Assert
        Assert.True(throttle.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);

        // Act
        throttle.Disable();

        // Assert
        Assert.False(throttle.IsEnabled);
    }

    [Fact]
    public void ResetUsage_ClearsCurrentUsage()
    {
        // Arrange
        var throttle = ResourceThrottle.Create(Guid.NewGuid(), ResourceType.ApiRequests, 100, ThrottleTimeWindow.PerMinute);
        throttle.TryAcquire(50);

        // Act
        throttle.ResetUsage();

        // Assert
        Assert.Equal(0, throttle.CurrentUsage);
    }
}
