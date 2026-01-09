using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.DealerDeduplicationRuleAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Analytics;

public class DealerDeduplicationRuleServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<ILogger<DealerDeduplicationRuleService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DealerDeduplicationRuleServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _loggerMock = new Mock<ILogger<DealerDeduplicationRuleService>>();
    }

    private DealerDeduplicationRuleService CreateService()
    {
        return new DealerDeduplicationRuleService(_contextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DealerDeduplicationRuleService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DealerDeduplicationRuleService(_contextMock.Object, null!));
    }

    [Fact]
    public async Task CreateRuleAsync_CreatesNewRule()
    {
        // Arrange
        var dealerId = DealerId.CreateNew();
        var rulesDbSet = CreateMockDbSet(new List<DealerDeduplicationRule>());
        _contextMock.Setup(c => c.DealerDeduplicationRules).Returns(rulesDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = CreateService();

        // Act
        var result = await service.CreateRuleAsync(_tenantId, dealerId, "Test Rule", "Test Description", "TestUser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal(dealerId, result.DealerId);
        Assert.Equal("Test Rule", result.RuleName);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("TestUser", result.CreatedBy);
        Assert.True(result.IsActive);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return mockSet;
    }
}

public class DealerDeduplicationRuleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsCorrectDefaults()
    {
        var dealerId = DealerId.CreateNew();

        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test Rule");

        Assert.NotNull(rule);
        Assert.Equal(_tenantId, rule.TenantId);
        Assert.Equal(dealerId, rule.DealerId);
        Assert.Equal("Test Rule", rule.RuleName);
        Assert.True(rule.IsActive);
        Assert.Equal(0, rule.Priority);
        Assert.Equal(RuleCondition.Always, rule.Condition);
        Assert.Equal(0, rule.TimesApplied);
    }

    [Fact]
    public void CreateStrictVinOnlyRule_ConfiguresCorrectly()
    {
        var dealerId = DealerId.CreateNew();

        var rule = DealerDeduplicationRule.CreateStrictVinOnlyRule(_tenantId, dealerId);

        Assert.Equal("Strict VIN-Only Matching", rule.RuleName);
        Assert.True(rule.StrictModeEnabled);
        Assert.True(rule.EnableVinMatching);
        Assert.False(rule.EnableFuzzyMatching);
        Assert.False(rule.EnableImageMatching);
        Assert.Equal(100, rule.Priority);
    }

    [Fact]
    public void CreateRelaxedRule_ConfiguresCorrectly()
    {
        var dealerId = DealerId.CreateNew();

        var rule = DealerDeduplicationRule.CreateRelaxedRule(_tenantId, dealerId);

        Assert.Equal("Relaxed Matching", rule.RuleName);
        Assert.Equal(75.0, rule.AutoMatchThreshold);
        Assert.Equal(50.0, rule.ReviewThreshold);
        Assert.Equal(1000, rule.MileageTolerance);
        Assert.Equal(1000m, rule.PriceTolerance);
        Assert.Equal(50, rule.Priority);
    }

    [Fact]
    public void CreateHighValueRule_ConfiguresCorrectly()
    {
        var dealerId = DealerId.CreateNew();

        var rule = DealerDeduplicationRule.CreateHighValueRule(_tenantId, dealerId, 50000m);

        Assert.Equal("High-Value Vehicle Matching", rule.RuleName);
        Assert.Equal(RuleCondition.PriceRange, rule.Condition);
        Assert.Equal(50000m, rule.MinPrice);
        Assert.Equal(95.0, rule.AutoMatchThreshold);
        Assert.Equal(80.0, rule.ReviewThreshold);
        Assert.Equal(100, rule.MileageTolerance);
        Assert.True(rule.EnableImageMatching);
        Assert.Equal(0.30, rule.ImageWeight);
        Assert.Equal(75, rule.Priority);
    }

    [Fact]
    public void SetThresholds_UpdatesValues()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetThresholds(90.0, 70.0);

        Assert.Equal(90.0, rule.AutoMatchThreshold);
        Assert.Equal(70.0, rule.ReviewThreshold);
        Assert.NotNull(rule.UpdatedAt);
    }

    [Fact]
    public void SetFieldWeights_UpdatesValues()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetFieldWeights(0.30, 0.20, 0.15, 0.15, 0.10, 0.10);

        Assert.Equal(0.30, rule.MakeModelWeight);
        Assert.Equal(0.20, rule.YearWeight);
        Assert.Equal(0.15, rule.MileageWeight);
        Assert.Equal(0.15, rule.PriceWeight);
        Assert.Equal(0.10, rule.LocationWeight);
        Assert.Equal(0.10, rule.ImageWeight);
    }

    [Fact]
    public void SetTolerances_UpdatesValues()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetTolerances(500, 250m, 2);

        Assert.Equal(500, rule.MileageTolerance);
        Assert.Equal(250m, rule.PriceTolerance);
        Assert.Equal(2, rule.YearTolerance);
    }

    [Fact]
    public void SetFeatureFlags_UpdatesValues()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetFeatureFlags(true, true, true, false);

        Assert.True(rule.EnableVinMatching);
        Assert.True(rule.EnableFuzzyMatching);
        Assert.True(rule.EnableImageMatching);
        Assert.False(rule.StrictModeEnabled);
    }

    [Fact]
    public void SetCondition_UpdatesValues()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetCondition(RuleCondition.PriceRange, 20000m, 50000m, 2018, 2024, "Toyota", "Camry");

        Assert.Equal(RuleCondition.PriceRange, rule.Condition);
        Assert.Equal(20000m, rule.MinPrice);
        Assert.Equal(50000m, rule.MaxPrice);
        Assert.Equal(2018, rule.MinYear);
        Assert.Equal(2024, rule.MaxYear);
        Assert.Equal("Toyota", rule.MakeFilter);
        Assert.Equal("Camry", rule.ModelFilter);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");
        rule.Deactivate();

        rule.Activate();

        Assert.True(rule.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.Deactivate();

        Assert.False(rule.IsActive);
    }

    [Fact]
    public void SetPriority_UpdatesValue()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.SetPriority(100);

        Assert.Equal(100, rule.Priority);
    }

    [Fact]
    public void RecordApplication_IncrementsCount()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        rule.RecordApplication();
        rule.RecordApplication();

        Assert.Equal(2, rule.TimesApplied);
        Assert.NotNull(rule.LastAppliedAt);
    }

    [Fact]
    public void AppliesToListing_ReturnsTrue_WhenAlwaysCondition()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");

        var applies = rule.AppliesToListing(25000m, 2020, "Toyota", "Camry");

        Assert.True(applies);
    }

    [Fact]
    public void AppliesToListing_ReturnsFalse_WhenInactive()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");
        rule.Deactivate();

        var applies = rule.AppliesToListing(25000m, 2020, "Toyota", "Camry");

        Assert.False(applies);
    }

    [Fact]
    public void AppliesToListing_ChecksPriceRange_WhenPriceRangeCondition()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");
        rule.SetCondition(RuleCondition.PriceRange, 20000m, 30000m);

        Assert.True(rule.AppliesToListing(25000m, null, null, null)); // Within range
        Assert.False(rule.AppliesToListing(15000m, null, null, null)); // Below min
        Assert.False(rule.AppliesToListing(35000m, null, null, null)); // Above max
    }

    [Fact]
    public void AppliesToListing_ChecksYearRange_WhenYearRangeCondition()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");
        rule.SetCondition(RuleCondition.YearRange, minYear: 2018, maxYear: 2022);

        Assert.True(rule.AppliesToListing(null, 2020, null, null)); // Within range
        Assert.False(rule.AppliesToListing(null, 2015, null, null)); // Below min
        Assert.False(rule.AppliesToListing(null, 2025, null, null)); // Above max
    }

    [Fact]
    public void AppliesToListing_ChecksMakeModel_WhenMakeModelCondition()
    {
        var dealerId = DealerId.CreateNew();
        var rule = DealerDeduplicationRule.Create(_tenantId, dealerId, "Test");
        rule.SetCondition(RuleCondition.MakeModel, makeFilter: "Toyota", modelFilter: "Camry");

        Assert.True(rule.AppliesToListing(null, null, "Toyota", "Camry"));
        Assert.True(rule.AppliesToListing(null, null, "TOYOTA", "CAMRY")); // Case insensitive
        Assert.False(rule.AppliesToListing(null, null, "Honda", "Accord"));
    }
}

public class RuleConditionTests
{
    [Theory]
    [InlineData(RuleCondition.Always)]
    [InlineData(RuleCondition.PriceRange)]
    [InlineData(RuleCondition.YearRange)]
    [InlineData(RuleCondition.MakeModel)]
    [InlineData(RuleCondition.Combined)]
    public void AllConditionsExist(RuleCondition condition)
    {
        Assert.True(Enum.IsDefined(typeof(RuleCondition), condition));
    }
}

public class RuleApplicationStatisticsTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var stats = new RuleApplicationStatistics();

        Assert.Equal(0, stats.TotalRules);
        Assert.Equal(0, stats.ActiveRules);
        Assert.Equal(0, stats.TotalApplications);
        Assert.Empty(stats.ApplicationsByRuleName);
        Assert.Empty(stats.ApplicationsByDealer);
        Assert.Empty(stats.MostUsedRules);
        Assert.Empty(stats.NeverUsedRules);
    }
}
