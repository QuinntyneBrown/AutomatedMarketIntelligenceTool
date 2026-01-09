using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Analytics;

public class DeduplicationConfigServiceTests
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly DeduplicationConfigService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DeduplicationConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestContext(options);
        _service = new DeduplicationConfigService(_context, NullLogger<DeduplicationConfigService>.Instance);
    }

    [Fact]
    public async Task GetDealerThresholdAsync_WithNoConfig_ReturnsNull()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();

        // Act
        var result = await _service.GetDealerThresholdAsync(_testTenantId, dealerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetDealerThresholdAsync_SetsThreshold()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var threshold = 75m;

        // Act
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId, threshold);

        // Assert
        var result = await _service.GetDealerThresholdAsync(_testTenantId, dealerId);
        Assert.Equal(threshold, result);
    }

    [Fact]
    public async Task SetDealerThresholdAsync_UpdatesExistingThreshold()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId, 75m);

        // Act
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId, 90m);

        // Assert
        var result = await _service.GetDealerThresholdAsync(_testTenantId, dealerId);
        Assert.Equal(90m, result);
    }

    [Fact]
    public async Task SetDealerThresholdAsync_ThrowsForInvalidThreshold()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SetDealerThresholdAsync(_testTenantId, dealerId, -10m));
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SetDealerThresholdAsync(_testTenantId, dealerId, 150m));
    }

    [Fact]
    public async Task RemoveDealerThresholdAsync_RemovesThreshold()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId, 80m);

        // Act
        await _service.RemoveDealerThresholdAsync(_testTenantId, dealerId);

        // Assert
        var result = await _service.GetDealerThresholdAsync(_testTenantId, dealerId);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveDealerThresholdAsync_HandlesNonExistentConfig()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();

        // Act & Assert - should not throw
        await _service.RemoveDealerThresholdAsync(_testTenantId, dealerId);
    }

    [Fact]
    public async Task GetAllDealerConfigsAsync_ReturnsAllConfigs()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Dealer One");
        var dealer2 = Dealer.Create(_testTenantId, "Dealer Two");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        await _service.SetDealerThresholdAsync(_testTenantId, dealer1.DealerId, 75m);
        await _service.SetDealerThresholdAsync(_testTenantId, dealer2.DealerId, 85m);

        // Act
        var configs = await _service.GetAllDealerConfigsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, configs.Count);
        Assert.Contains(configs, c => c.DealerName == "Dealer One" && c.CustomThreshold == 75m);
        Assert.Contains(configs, c => c.DealerName == "Dealer Two" && c.CustomThreshold == 85m);
    }

    [Fact]
    public async Task GetAllDealerConfigsAsync_FiltersById()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        
        var dealer1 = Dealer.Create(tenant1, "Tenant1 Dealer");
        var dealer2 = Dealer.Create(tenant2, "Tenant2 Dealer");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        await _service.SetDealerThresholdAsync(tenant1, dealer1.DealerId, 70m);
        await _service.SetDealerThresholdAsync(tenant2, dealer2.DealerId, 80m);

        // Act
        var configs = await _service.GetAllDealerConfigsAsync(tenant1);

        // Assert
        Assert.Single(configs);
        Assert.Equal("Tenant1 Dealer", configs[0].DealerName);
    }

    [Fact]
    public async Task SetDealerStrictModeAsync_EnablesStrictMode()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();

        // Act
        await _service.SetDealerStrictModeAsync(_testTenantId, dealerId, true);

        // Assert
        var result = await _service.GetDealerStrictModeAsync(_testTenantId, dealerId);
        Assert.True(result);
    }

    [Fact]
    public async Task SetDealerStrictModeAsync_DisablesStrictMode()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        await _service.SetDealerStrictModeAsync(_testTenantId, dealerId, true);

        // Act
        await _service.SetDealerStrictModeAsync(_testTenantId, dealerId, false);

        // Assert
        var result = await _service.GetDealerStrictModeAsync(_testTenantId, dealerId);
        Assert.False(result);
    }

    [Fact]
    public async Task GetDealerStrictModeAsync_DefaultsToFalse()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();

        // Act
        var result = await _service.GetDealerStrictModeAsync(_testTenantId, dealerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllDealerConfigsAsync_IncludesStrictMode()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Strict Dealer");
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();

        await _service.SetDealerThresholdAsync(_testTenantId, dealer.DealerId, 95m);
        await _service.SetDealerStrictModeAsync(_testTenantId, dealer.DealerId, true);

        // Act
        var configs = await _service.GetAllDealerConfigsAsync(_testTenantId);

        // Assert
        var config = Assert.Single(configs);
        Assert.Equal("Strict Dealer", config.DealerName);
        Assert.Equal(95m, config.CustomThreshold);
        Assert.True(config.StrictMode);
    }

    [Fact]
    public async Task SetDealerThresholdAsync_AtBoundaries()
    {
        // Arrange
        var dealerId1 = DealerId.CreateNew();
        var dealerId2 = DealerId.CreateNew();

        // Act
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId1, 0m);
        await _service.SetDealerThresholdAsync(_testTenantId, dealerId2, 100m);

        // Assert
        var result1 = await _service.GetDealerThresholdAsync(_testTenantId, dealerId1);
        var result2 = await _service.GetDealerThresholdAsync(_testTenantId, dealerId2);
        Assert.Equal(0m, result1);
        Assert.Equal(100m, result2);
    }

    [Fact]
    public async Task GetAllDealerConfigsAsync_ExcludesDealersWithoutConfigs()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Configured Dealer");
        var dealer2 = Dealer.Create(_testTenantId, "Unconfigured Dealer");
        _context.Dealers.Add(dealer1);
        _context.Dealers.Add(dealer2);
        await _context.SaveChangesAsync();

        await _service.SetDealerThresholdAsync(_testTenantId, dealer1.DealerId, 80m);
        // dealer2 has no config

        // Act
        var configs = await _service.GetAllDealerConfigsAsync(_testTenantId);

        // Assert
        Assert.Single(configs);
        Assert.Equal("Configured Dealer", configs[0].DealerName);
    }

    [Fact]
    public async Task ConfigService_IsolatesTenants()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var dealerId = DealerId.CreateNew();

        // Act
        await _service.SetDealerThresholdAsync(tenant1, dealerId, 70m);
        await _service.SetDealerThresholdAsync(tenant2, dealerId, 90m);

        // Assert
        var result1 = await _service.GetDealerThresholdAsync(tenant1, dealerId);
        var result2 = await _service.GetDealerThresholdAsync(tenant2, dealerId);
        Assert.Equal(70m, result1);
        Assert.Equal(90m, result2);
    }

    [Fact]
    public async Task ConfigService_HandlesMultipleSimultaneousOperations()
    {
        // Arrange
        var dealers = Enumerable.Range(0, 10).Select(_ => DealerId.CreateNew()).ToList();

        // Act
        var tasks = dealers.Select((dealerId, index) =>
            _service.SetDealerThresholdAsync(_testTenantId, dealerId, 50m + index));
        await Task.WhenAll(tasks);

        // Assert
        for (int i = 0; i < dealers.Count; i++)
        {
            var result = await _service.GetDealerThresholdAsync(_testTenantId, dealers[i]);
            Assert.Equal(50m + i, result);
        }
    }
}
