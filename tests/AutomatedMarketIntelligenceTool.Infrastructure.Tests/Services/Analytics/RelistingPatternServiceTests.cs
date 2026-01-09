using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.RelistingPatternAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Analytics;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Analytics;

public class RelistingPatternServiceTests
{
    private readonly Mock<IAutomatedMarketIntelligenceToolContext> _contextMock;
    private readonly Mock<ILogger<RelistingPatternService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RelistingPatternServiceTests()
    {
        _contextMock = new Mock<IAutomatedMarketIntelligenceToolContext>();
        _loggerMock = new Mock<ILogger<RelistingPatternService>>();
    }

    private RelistingPatternService CreateService()
    {
        return new RelistingPatternService(_contextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RelistingPatternService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new RelistingPatternService(_contextMock.Object, null!));
    }
}

public class RelistingPatternTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_SetsBasicProperties()
    {
        var currentListingId = ListingId.Create();
        var previousListingId = ListingId.Create();
        var dealerId = DealerId.CreateNew();

        var pattern = RelistingPattern.Create(
            _tenantId,
            currentListingId,
            previousListingId,
            dealerId,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            24000m,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            30,
            "1HGBH41JXMN109186",
            "Toyota",
            "Camry",
            2020);

        Assert.NotNull(pattern);
        Assert.Equal(_tenantId, pattern.TenantId);
        Assert.Equal(currentListingId, pattern.CurrentListingId);
        Assert.Equal(previousListingId, pattern.PreviousListingId);
        Assert.Equal(dealerId, pattern.DealerId);
        Assert.Equal(RelistingType.VinMatch, pattern.Type);
        Assert.Equal(100.0, pattern.MatchConfidence);
        Assert.Equal("VIN", pattern.MatchMethod);
    }

    [Fact]
    public void Create_CalculatesPriceChange()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            24000m, // $1000 decrease
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            30);

        Assert.Equal(-1000m, pattern.PriceChange);
        Assert.True(pattern.PriceChangePercent < 0); // Negative percentage
    }

    [Fact]
    public void Create_CalculatesDaysBetweenListings()
    {
        var deactivatedDate = DateTime.UtcNow.AddDays(-10);
        var listedDate = DateTime.UtcNow.AddDays(-3);

        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            25000m,
            deactivatedDate,
            listedDate,
            30);

        Assert.Equal(7, pattern.DaysBetweenListings);
    }

    [Fact]
    public void Create_DetectsQuickFlipAsSuspicious()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            27000m, // Price increase
            DateTime.UtcNow.AddDays(-2), // Deactivated 2 days ago
            DateTime.UtcNow, // Listed today
            30);

        Assert.True(pattern.IsSuspiciousPattern);
        Assert.Contains("Quick flip", pattern.SuspiciousReason);
    }

    [Fact]
    public void Create_DetectsSignificantPriceChangeAsSuspicious()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            32000m, // 28% increase - above 20% threshold
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            30);

        Assert.True(pattern.IsSuspiciousPattern);
        Assert.Contains("price change", pattern.SuspiciousReason);
    }

    [Fact]
    public void Create_DetectsDaysOnMarketResetAsSuspicious()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            25000m,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow,
            90); // Previous listing was on market for 90 days

        Assert.True(pattern.IsSuspiciousPattern);
        Assert.Contains("reset", pattern.SuspiciousReason);
    }

    [Fact]
    public void Create_NormalRelistingNotSuspicious()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            24500m, // Small price decrease
            DateTime.UtcNow.AddDays(-30), // 30 days between listings
            DateTime.UtcNow,
            30);

        Assert.False(pattern.IsSuspiciousPattern);
        Assert.Null(pattern.SuspiciousReason);
    }

    [Fact]
    public void UpdateType_ChangesType()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.FuzzyMatch,
            80.0,
            "Fuzzy",
            null,
            null,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow,
            0);

        pattern.UpdateType(RelistingType.CombinedMatch);

        Assert.Equal(RelistingType.CombinedMatch, pattern.Type);
    }

    [Fact]
    public void MarkAsSuspicious_SetsFlag()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            null,
            null,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            30);

        pattern.MarkAsSuspicious("Manual review flagged");

        Assert.True(pattern.IsSuspiciousPattern);
        Assert.Contains("Manual review", pattern.SuspiciousReason);
    }

    [Fact]
    public void ClearSuspiciousFlag_ResetsFlag()
    {
        var pattern = RelistingPattern.Create(
            _tenantId,
            ListingId.Create(),
            ListingId.Create(),
            null,
            RelistingType.VinMatch,
            100.0,
            "VIN",
            25000m,
            35000m, // Will trigger suspicious
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            30);

        Assert.True(pattern.IsSuspiciousPattern);

        pattern.ClearSuspiciousFlag();

        Assert.False(pattern.IsSuspiciousPattern);
        Assert.Null(pattern.SuspiciousReason);
    }
}

public class RelistingDetectionResultTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var result = new RelistingDetectionResult();

        Assert.False(result.IsRelisting);
        Assert.Null(result.Pattern);
        Assert.Null(result.MatchedListingId);
        Assert.Equal(0, result.MatchConfidence);
        Assert.Null(result.MatchMethod);
    }
}

public class RelistingScanResultTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var result = new RelistingScanResult();

        Assert.Equal(0, result.ListingsScanned);
        Assert.Equal(0, result.RelistingsFound);
        Assert.Equal(0, result.SuspiciousPatterns);
        Assert.Equal(0, result.ProcessingTimeMs);
        Assert.Empty(result.Patterns);
    }
}

public class DealerRelistingStatisticsTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var stats = new DealerRelistingStatistics();

        Assert.Null(stats.DealerId);
        Assert.Equal(0, stats.TotalListings);
        Assert.Equal(0, stats.TotalRelistings);
        Assert.Equal(0, stats.RelistingRate);
        Assert.Equal(0, stats.SuspiciousRelistings);
        Assert.Equal(0, stats.AverageDaysBetweenRelistings);
        Assert.Equal(0m, stats.AveragePriceChangeOnRelist);
        Assert.Equal(0, stats.AveragePriceChangePercentOnRelist);
        Assert.False(stats.IsFrequentRelister);
    }
}

public class MarketRelistingStatisticsTests
{
    [Fact]
    public void DefaultValues_Correct()
    {
        var stats = new MarketRelistingStatistics();

        Assert.Equal(0, stats.TotalListings);
        Assert.Equal(0, stats.TotalRelistings);
        Assert.Equal(0, stats.MarketRelistingRate);
        Assert.Equal(0, stats.SuspiciousRelistings);
        Assert.Equal(0, stats.FrequentRelisterCount);
        Assert.Equal(0, stats.AverageDaysBetweenRelistings);
        Assert.Equal(0m, stats.AveragePriceChangeOnRelist);
        Assert.Empty(stats.CountByType);
        Assert.Empty(stats.TopRelistingDealers);
    }
}

public class RelistingTypeTests
{
    [Theory]
    [InlineData(RelistingType.VinMatch)]
    [InlineData(RelistingType.ExternalIdMatch)]
    [InlineData(RelistingType.FuzzyMatch)]
    [InlineData(RelistingType.ImageMatch)]
    [InlineData(RelistingType.CombinedMatch)]
    public void AllTypesExist(RelistingType type)
    {
        Assert.True(Enum.IsDefined(typeof(RelistingType), type));
    }
}
