using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.CustomMarkets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Sprint7;

public class CustomMarketServiceTests
{
    private readonly Sprint7TestContext _context;
    private readonly CustomMarketService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public CustomMarketServiceTests()
    {
        var options = new DbContextOptionsBuilder<Sprint7TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Sprint7TestContext(options);
        _service = new CustomMarketService(_context, NullLogger<CustomMarketService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMarketService(null!, NullLogger<CustomMarketService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CustomMarketService(_context, null!));
    }

    [Fact]
    public async Task CreateMarketAsync_WithValidData_CreatesMarket()
    {
        // Act
        var market = await _service.CreateMarketAsync(
            _testTenantId,
            "Greater Toronto Area",
            "M5V,M5S,M4T",
            "Downtown Toronto markets",
            "ON",
            43.6532,
            -79.3832,
            50);

        // Assert
        Assert.NotNull(market);
        Assert.Equal("Greater Toronto Area", market.Name);
        Assert.Equal("M5V,M5S,M4T", market.PostalCodes);
        Assert.Equal("ON", market.Provinces);
        Assert.True(market.IsActive);
        Assert.Equal(100, market.Priority);
    }

    [Fact]
    public async Task CreateMarketAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMarketAsync(_testTenantId, "Test Market", "M5S"));
    }

    [Fact]
    public async Task GetMarketAsync_WithExistingId_ReturnsMarket()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V");

        // Act
        var result = await _service.GetMarketAsync(_testTenantId, market.CustomMarketId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Market", result.Name);
    }

    [Fact]
    public async Task GetMarketAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetMarketAsync(_testTenantId, new CustomMarketId(Guid.NewGuid()));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMarketByNameAsync_WithExistingName_ReturnsMarket()
    {
        // Arrange
        await _service.CreateMarketAsync(_testTenantId, "Pacific Northwest", "V5V,V6V");

        // Act
        var result = await _service.GetMarketByNameAsync(_testTenantId, "Pacific Northwest");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("V5V,V6V", result.PostalCodes);
    }

    [Fact]
    public async Task GetMarketByNameAsync_WithNonExistingName_ReturnsNull()
    {
        // Act
        var result = await _service.GetMarketByNameAsync(_testTenantId, "Non Existing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllMarketsAsync_ReturnsAllMarketsForTenant()
    {
        // Arrange
        await _service.CreateMarketAsync(_testTenantId, "Market 1", "M5V");
        await _service.CreateMarketAsync(_testTenantId, "Market 2", "M5S");
        await _service.CreateMarketAsync(Guid.NewGuid(), "Other Tenant", "V5V"); // Different tenant

        // Act
        var result = await _service.GetAllMarketsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Name == "Market 1");
        Assert.Contains(result, m => m.Name == "Market 2");
    }

    [Fact]
    public async Task GetAllMarketsAsync_WithActiveOnly_ReturnsOnlyActiveMarkets()
    {
        // Arrange
        var market1 = await _service.CreateMarketAsync(_testTenantId, "Active Market", "M5V");
        var market2 = await _service.CreateMarketAsync(_testTenantId, "Inactive Market", "M5S");
        await _service.DeactivateMarketAsync(_testTenantId, market2.CustomMarketId);

        // Act
        var result = await _service.GetAllMarketsAsync(_testTenantId, activeOnly: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Market", result[0].Name);
    }

    [Fact]
    public async Task UpdateMarketAsync_WithValidData_UpdatesMarket()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Original Name", "M5V");

        // Act
        var updated = await _service.UpdateMarketAsync(
            _testTenantId,
            market.CustomMarketId,
            "Updated Name",
            "M5V,M5S,M4T",
            "Updated description",
            "ON,QC");

        // Assert
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("M5V,M5S,M4T", updated.PostalCodes);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("ON,QC", updated.Provinces);
    }

    [Fact]
    public async Task UpdateMarketAsync_WithNonExistingId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateMarketAsync(
                _testTenantId,
                new CustomMarketId(Guid.NewGuid()),
                "Name",
                "M5V"));
    }

    [Fact]
    public async Task ActivateMarketAsync_WithExistingMarket_ReturnsTrue()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V");
        await _service.DeactivateMarketAsync(_testTenantId, market.CustomMarketId);

        // Act
        var result = await _service.ActivateMarketAsync(_testTenantId, market.CustomMarketId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetMarketAsync(_testTenantId, market.CustomMarketId);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task DeactivateMarketAsync_WithExistingMarket_ReturnsTrue()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V");

        // Act
        var result = await _service.DeactivateMarketAsync(_testTenantId, market.CustomMarketId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetMarketAsync(_testTenantId, market.CustomMarketId);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task DeleteMarketAsync_WithExistingMarket_ReturnsTrue()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V");

        // Act
        var result = await _service.DeleteMarketAsync(_testTenantId, market.CustomMarketId);

        // Assert
        Assert.True(result);
        var deleted = await _service.GetMarketAsync(_testTenantId, market.CustomMarketId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteMarketAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteMarketAsync(_testTenantId, new CustomMarketId(Guid.NewGuid()));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetMarketPostalCodesAsync_ReturnsPostalCodeList()
    {
        // Arrange
        var market = await _service.CreateMarketAsync(_testTenantId, "Test Market", "M5V, M5S, M4T");

        // Act
        var result = await _service.GetMarketPostalCodesAsync(_testTenantId, market.CustomMarketId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("M5V", result);
        Assert.Contains("M5S", result);
        Assert.Contains("M4T", result);
    }
}

public class CustomMarketModelTests
{
    [Fact]
    public void Create_WithValidData_CreatesMarket()
    {
        // Act
        var market = CustomMarket.Create(
            Guid.NewGuid(),
            "Test Market",
            "M5V,M5S");

        // Assert
        Assert.NotNull(market.CustomMarketId);
        Assert.Equal("Test Market", market.Name);
        Assert.Equal("M5V,M5S", market.PostalCodes);
        Assert.True(market.IsActive);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            CustomMarket.Create(Guid.NewGuid(), "", "M5V"));
    }

    [Fact]
    public void Create_WithEmptyPostalCodes_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            CustomMarket.Create(Guid.NewGuid(), "Test", ""));
    }

    [Fact]
    public void GetPostalCodeList_ReturnsCorrectList()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V, M5S, M4T");

        // Act
        var codes = market.GetPostalCodeList();

        // Assert
        Assert.Equal(3, codes.Count);
        Assert.Contains("M5V", codes);
        Assert.Contains("M5S", codes);
        Assert.Contains("M4T", codes);
    }

    [Fact]
    public void GetProvinceList_WithProvinces_ReturnsCorrectList()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V", provinces: "ON, QC, BC");

        // Act
        var provinces = market.GetProvinceList();

        // Assert
        Assert.Equal(3, provinces.Count);
        Assert.Contains("ON", provinces);
        Assert.Contains("QC", provinces);
        Assert.Contains("BC", provinces);
    }

    [Fact]
    public void GetProvinceList_WithNoProvinces_ReturnsEmptyList()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V");

        // Act
        var provinces = market.GetProvinceList();

        // Assert
        Assert.Empty(provinces);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V");
        market.Deactivate();

        // Act
        market.Activate();

        // Assert
        Assert.True(market.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V");

        // Act
        market.Deactivate();

        // Assert
        Assert.False(market.IsActive);
    }

    [Fact]
    public void SetPriority_UpdatesPriority()
    {
        // Arrange
        var market = CustomMarket.Create(Guid.NewGuid(), "Test", "M5V");

        // Act
        market.SetPriority(50);

        // Assert
        Assert.Equal(50, market.Priority);
    }
}
