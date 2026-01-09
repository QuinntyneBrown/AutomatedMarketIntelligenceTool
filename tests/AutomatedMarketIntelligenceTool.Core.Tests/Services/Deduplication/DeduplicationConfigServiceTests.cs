using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationConfigAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Deduplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Deduplication;

public class DeduplicationConfigServiceTests
{
    private readonly DeduplicationTestContext _context;
    private readonly DeduplicationConfigService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DeduplicationConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<DeduplicationTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DeduplicationTestContext(options);
        _service = new DeduplicationConfigService(_context, NullLogger<DeduplicationConfigService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationConfigService(null!, NullLogger<DeduplicationConfigService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationConfigService(_context, null!));
    }

    [Fact]
    public async Task GetConfigAsync_WithExistingKey_ReturnsConfig()
    {
        // Arrange
        var config = DeduplicationConfig.Create(_testTenantId, "test.key", "test-value");
        _context.DeduplicationConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigAsync(_testTenantId, "test.key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-value", result.ConfigValue);
    }

    [Fact]
    public async Task GetConfigAsync_WithNonExistingKey_ReturnsNull()
    {
        // Act
        var result = await _service.GetConfigAsync(_testTenantId, "non.existing.key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConfigAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetConfigAsync(_testTenantId, ""));
    }

    [Fact]
    public async Task GetConfigAsync_WithWhitespaceKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetConfigAsync(_testTenantId, "   "));
    }

    [Fact]
    public async Task GetAllConfigsAsync_ReturnsAllConfigsForTenant()
    {
        // Arrange
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "key1", "value1"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "key2", "value2"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(Guid.NewGuid(), "other.key", "other")); // Different tenant
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllConfigsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.ConfigKey == "key1");
        Assert.Contains(result, c => c.ConfigKey == "key2");
    }

    [Fact]
    public async Task GetConfigsByPatternAsync_WithWildcard_ReturnsMatchingConfigs()
    {
        // Arrange
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "dedup.weight.make", "0.30"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "dedup.weight.year", "0.20"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "dedup.threshold", "85"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigsByPatternAsync(_testTenantId, "dedup.weight.*");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.StartsWith("dedup.weight.", c.ConfigKey));
    }

    [Fact]
    public async Task GetConfigsByPatternAsync_WithEmptyPattern_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetConfigsByPatternAsync(_testTenantId, ""));
    }

    [Fact]
    public async Task SetConfigAsync_WithNewKey_CreatesConfig()
    {
        // Act
        var result = await _service.SetConfigAsync(_testTenantId, "new.key", "new-value", "Test description", "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new.key", result.ConfigKey);
        Assert.Equal("new-value", result.ConfigValue);
        Assert.Equal("Test description", result.Description);

        var saved = await _context.DeduplicationConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "new.key");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task SetConfigAsync_WithExistingKey_UpdatesConfig()
    {
        // Arrange
        var config = DeduplicationConfig.Create(_testTenantId, "existing.key", "old-value");
        _context.DeduplicationConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetConfigAsync(_testTenantId, "existing.key", "new-value", null, "testuser");

        // Assert
        Assert.Equal("new-value", result.ConfigValue);

        var saved = await _context.DeduplicationConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == _testTenantId && c.ConfigKey == "existing.key");
        Assert.NotNull(saved);
        Assert.Equal("new-value", saved.ConfigValue);
    }

    [Fact]
    public async Task SetConfigAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetConfigAsync(_testTenantId, "", "value"));
    }

    [Fact]
    public async Task SetConfigAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SetConfigAsync(_testTenantId, "key", null!));
    }

    [Fact]
    public async Task DeleteConfigAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var config = DeduplicationConfig.Create(_testTenantId, "delete.me", "value");
        _context.DeduplicationConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteConfigAsync(_testTenantId, "delete.me");

        // Assert
        Assert.True(result);

        var deleted = await _context.DeduplicationConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == _testTenantId && c.ConfigKey == "delete.me");
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteConfigAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteConfigAsync(_testTenantId, "non.existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteConfigAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DeleteConfigAsync(_testTenantId, ""));
    }

    [Fact]
    public async Task GetOptionsAsync_WithNoStoredConfig_ReturnsDefaults()
    {
        // Act
        var result = await _service.GetOptionsAsync(_testTenantId);

        // Assert
        Assert.True(result.Enabled);
        Assert.Equal(DeduplicationConfig.Defaults.AutoThreshold, result.AutoThreshold);
        Assert.Equal(DeduplicationConfig.Defaults.ReviewThreshold, result.ReviewThreshold);
        Assert.False(result.StrictMode);
        Assert.True(result.EnableFuzzyMatching);
        Assert.False(result.EnableImageMatching);
        Assert.Equal(DeduplicationConfig.Defaults.MileageTolerance, result.MileageTolerance);
        Assert.Equal(DeduplicationConfig.Defaults.PriceTolerance, result.PriceTolerance);
        Assert.NotNull(result.Weights);
    }

    [Fact]
    public async Task GetOptionsAsync_WithStoredConfig_ReturnsOverriddenValues()
    {
        // Arrange
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(
            _testTenantId, DeduplicationConfig.Keys.AutoThreshold, "90"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(
            _testTenantId, DeduplicationConfig.Keys.StrictMode, "true"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(
            _testTenantId, DeduplicationConfig.Keys.MileageTolerance, "1000"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOptionsAsync(_testTenantId);

        // Assert
        Assert.Equal(90, result.AutoThreshold);
        Assert.True(result.StrictMode);
        Assert.Equal(1000, result.MileageTolerance);
        // Defaults should still apply to non-overridden values
        Assert.Equal(DeduplicationConfig.Defaults.ReviewThreshold, result.ReviewThreshold);
    }

    [Fact]
    public async Task GetOptionsAsync_WithWeightOverrides_ReturnsCustomWeights()
    {
        // Arrange
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(
            _testTenantId, DeduplicationConfig.Keys.WeightMakeModel, "0.40"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(
            _testTenantId, DeduplicationConfig.Keys.WeightYear, "0.25"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOptionsAsync(_testTenantId);

        // Assert
        Assert.Equal(0.40, result.Weights.MakeModel);
        Assert.Equal(0.25, result.Weights.Year);
        Assert.Equal(DeduplicationConfig.Defaults.WeightMileage, result.Weights.Mileage);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_RemovesAllConfigsForTenant()
    {
        // Arrange
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "key1", "value1"));
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(_testTenantId, "key2", "value2"));
        var otherTenantId = Guid.NewGuid();
        _context.DeduplicationConfigs.Add(DeduplicationConfig.Create(otherTenantId, "other.key", "other"));
        await _context.SaveChangesAsync();

        // Act
        await _service.ResetToDefaultsAsync(_testTenantId);

        // Assert
        var remaining = await _context.DeduplicationConfigs.IgnoreQueryFilters()
            .Where(c => c.TenantId == _testTenantId)
            .ToListAsync();
        Assert.Empty(remaining);

        // Other tenant's config should remain
        var otherConfig = await _context.DeduplicationConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == otherTenantId);
        Assert.NotNull(otherConfig);
    }

    [Fact]
    public void FieldWeights_IsValid_WhenSumToOne_ReturnsTrue()
    {
        // Arrange
        var weights = new FieldWeights
        {
            MakeModel = 0.30,
            Year = 0.20,
            Mileage = 0.15,
            Price = 0.15,
            Location = 0.10,
            Image = 0.10
        };

        // Act & Assert
        Assert.True(weights.IsValid());
    }

    [Fact]
    public void FieldWeights_IsValid_WhenNotSumToOne_ReturnsFalse()
    {
        // Arrange
        var weights = new FieldWeights
        {
            MakeModel = 0.50,
            Year = 0.50,
            Mileage = 0.50,
            Price = 0.50,
            Location = 0.50,
            Image = 0.50
        };

        // Act & Assert
        Assert.False(weights.IsValid());
    }

    [Fact]
    public void DeduplicationOptions_Default_HasExpectedValues()
    {
        // Act
        var options = DeduplicationOptions.Default;

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(85.0, options.AutoThreshold);
        Assert.Equal(60.0, options.ReviewThreshold);
        Assert.False(options.StrictMode);
        Assert.True(options.EnableFuzzyMatching);
        Assert.False(options.EnableImageMatching);
    }
}
