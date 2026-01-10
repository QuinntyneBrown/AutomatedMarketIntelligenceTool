using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Deduplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Deduplication;

public class AccuracyMetricsServiceTests
{
    private readonly DeduplicationTestContext _context;
    private readonly AccuracyMetricsService _service;
    private readonly DeduplicationAuditService _auditService;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public AccuracyMetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<DeduplicationTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DeduplicationTestContext(options);
        _service = new AccuracyMetricsService(_context, NullLogger<AccuracyMetricsService>.Instance);
        _auditService = new DeduplicationAuditService(_context, NullLogger<DeduplicationAuditService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AccuracyMetricsService(null!, NullLogger<AccuracyMetricsService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AccuracyMetricsService(_context, null!));
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithNoData_ReturnsZeroTotalDecisions()
    {
        // Act
        var result = await _service.CalculateMetricsAsync(_testTenantId);

        // Assert
        Assert.Equal(0, result.TotalDecisions);
        Assert.Equal(0, result.TruePositives);
        Assert.Equal(0, result.FalsePositives);
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithPerfectAccuracy_ReturnsExpectedMetrics()
    {
        // Arrange - All decisions are correct (no false positives/negatives)
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch); // True Positive
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch); // True Negative

        // Act
        var result = await _service.CalculateMetricsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.TotalDecisions);
        Assert.Equal(1, result.TruePositives);
        Assert.Equal(1, result.TrueNegatives);
        Assert.Equal(0, result.FalsePositives);
        Assert.Equal(0, result.FalseNegatives);
        Assert.Equal(1.0, result.Precision);
        Assert.Equal(1.0, result.Recall);
        Assert.Equal(1.0, result.F1Score);
        Assert.Equal(1.0, result.Accuracy);
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithFalsePositives_ReturnsCorrectPrecision()
    {
        // Arrange
        // 2 True Positives
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 90m);

        // 1 False Positive
        var fp = await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 70m);
        await _auditService.MarkAsFalsePositiveAsync(_testTenantId, fp.AuditEntryId);

        // Act
        var result = await _service.CalculateMetricsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.TruePositives);
        Assert.Equal(1, result.FalsePositives);
        // Precision = TP / (TP + FP) = 2 / (2 + 1) = 0.667
        Assert.InRange(result.Precision, 0.66, 0.67);
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithFalseNegatives_ReturnsCorrectRecall()
    {
        // Arrange
        // 1 True Positive
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);

        // 1 False Negative (missed duplicate)
        var fn = await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);
        await _auditService.MarkAsFalseNegativeAsync(_testTenantId, fn.AuditEntryId);

        // Act
        var result = await _service.CalculateMetricsAsync(_testTenantId);

        // Assert
        Assert.Equal(1, result.TruePositives);
        Assert.Equal(1, result.FalseNegatives);
        // Recall = TP / (TP + FN) = 1 / (1 + 1) = 0.5
        Assert.Equal(0.5, result.Recall);
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithMixedResults_ReturnsCorrectF1Score()
    {
        // Arrange - Create realistic scenario
        // 3 True Positives
        for (int i = 0; i < 3; i++)
        {
            await _auditService.RecordAutomaticDecisionAsync(
                _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
                AuditDecision.Duplicate, AuditReason.VinMatch);
        }

        // 1 False Positive
        var fp = await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 75m);
        await _auditService.MarkAsFalsePositiveAsync(_testTenantId, fp.AuditEntryId);

        // 1 False Negative
        var fn = await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);
        await _auditService.MarkAsFalseNegativeAsync(_testTenantId, fn.AuditEntryId);

        // 2 True Negatives
        for (int i = 0; i < 2; i++)
        {
            await _auditService.RecordAutomaticDecisionAsync(
                _testTenantId, Guid.NewGuid(), null,
                AuditDecision.NewListing, AuditReason.NoMatch);
        }

        // Act
        var result = await _service.CalculateMetricsAsync(_testTenantId);

        // Assert
        Assert.Equal(7, result.TotalDecisions);
        Assert.Equal(3, result.TruePositives);
        Assert.Equal(2, result.TrueNegatives);
        Assert.Equal(1, result.FalsePositives);
        Assert.Equal(1, result.FalseNegatives);

        // Precision = 3 / (3 + 1) = 0.75
        Assert.Equal(0.75, result.Precision);
        // Recall = 3 / (3 + 1) = 0.75
        Assert.Equal(0.75, result.Recall);
        // F1 = 2 * (0.75 * 0.75) / (0.75 + 0.75) = 0.75
        Assert.Equal(0.75, result.F1Score);
    }

    [Fact]
    public async Task CalculateMetricsAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);

        // Act
        var result = await _service.CalculateMetricsAsync(
            _testTenantId,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.Equal(1, result.TotalDecisions);
        Assert.NotNull(result.FromDate);
        Assert.NotNull(result.ToDate);
    }

    [Fact]
    public async Task GetMetricsByReasonAsync_ReturnsBreakdownByReason()
    {
        // Arrange
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 90m);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.GetMetricsByReasonAsync(_testTenantId);

        // Assert
        Assert.True(result.ContainsKey(AuditReason.VinMatch));
        Assert.True(result.ContainsKey(AuditReason.FuzzyMatch));
        Assert.True(result.ContainsKey(AuditReason.NoMatch));
        Assert.Equal(2, result[AuditReason.VinMatch].TotalDecisions);
        Assert.Equal(1, result[AuditReason.FuzzyMatch].TotalDecisions);
    }

    [Fact]
    public async Task GetAccuracyTrendAsync_WithDailyGranularity_ReturnsCorrectTrend()
    {
        // Arrange
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.GetAccuracyTrendAsync(
            _testTenantId,
            DateTime.UtcNow.Date.AddDays(-7),
            DateTime.UtcNow.Date.AddDays(1),
            TrendGranularity.Daily);

        // Assert
        Assert.NotEmpty(result);
        var todayPoint = result.FirstOrDefault(p => p.Date == DateTime.UtcNow.Date);
        Assert.NotNull(todayPoint);
        Assert.Equal(2, todayPoint.TotalDecisions);
    }

    [Fact]
    public async Task GetAccuracyTrendAsync_WithWeeklyGranularity_GroupsByWeek()
    {
        // Arrange
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);

        // Act
        var result = await _service.GetAccuracyTrendAsync(
            _testTenantId,
            DateTime.UtcNow.Date.AddDays(-30),
            DateTime.UtcNow.Date.AddDays(1),
            TrendGranularity.Weekly);

        // Assert
        Assert.NotEmpty(result);
        // Verify date is a Monday (week start)
        var point = result.First();
        Assert.Equal(DayOfWeek.Monday, point.Date.DayOfWeek);
    }

    [Fact]
    public async Task GetAccuracyTrendAsync_WithMonthlyGranularity_GroupsByMonth()
    {
        // Arrange
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);

        // Act
        var result = await _service.GetAccuracyTrendAsync(
            _testTenantId,
            DateTime.UtcNow.Date.AddMonths(-3),
            DateTime.UtcNow.Date.AddDays(1),
            TrendGranularity.Monthly);

        // Assert
        Assert.NotEmpty(result);
        // Verify date is first of month
        var point = result.First();
        Assert.Equal(1, point.Date.Day);
    }

    [Fact]
    public async Task GetThresholdAnalysisAsync_WithNoData_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetThresholdAnalysisAsync(_testTenantId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetThresholdAnalysisAsync_WithFuzzyMatches_ReturnsThresholdBreakdown()
    {
        // Arrange - Create entries with different confidence scores
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 95m);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 85m);
        await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 75m);
        var fp = await _auditService.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 65m);
        await _auditService.MarkAsFalsePositiveAsync(_testTenantId, fp.AuditEntryId);

        // Act
        var result = await _service.GetThresholdAnalysisAsync(_testTenantId);

        // Assert
        Assert.NotEmpty(result);

        // Higher thresholds should have fewer items
        var threshold90 = result.FirstOrDefault(t => t.Threshold == 90);
        var threshold70 = result.FirstOrDefault(t => t.Threshold == 70);

        Assert.NotNull(threshold90);
        Assert.NotNull(threshold70);
        Assert.True(threshold90.TotalAtOrAbove <= threshold70.TotalAtOrAbove);

        // Higher threshold should have better precision
        Assert.True(threshold90.Precision >= threshold70.Precision);
    }

    [Fact]
    public void AccuracyMetrics_CalculatedProperties_AreCorrect()
    {
        // Arrange
        var metrics = new AccuracyMetrics
        {
            TotalDecisions = 100,
            TruePositives = 40,
            TrueNegatives = 45,
            FalsePositives = 5,
            FalseNegatives = 10
        };

        // Act & Assert
        // Precision = 40 / (40 + 5) = 0.889
        Assert.InRange(metrics.Precision, 0.88, 0.90);

        // Recall = 40 / (40 + 10) = 0.8
        Assert.Equal(0.8, metrics.Recall);

        // F1 = 2 * (0.889 * 0.8) / (0.889 + 0.8) = 0.842
        Assert.InRange(metrics.F1Score, 0.84, 0.85);

        // Accuracy = (40 + 45) / 100 = 0.85
        Assert.Equal(0.85, metrics.Accuracy);

        // Specificity = 45 / (45 + 5) = 0.9
        Assert.Equal(0.9, metrics.Specificity);

        // FP Rate = 5 / (5 + 45) = 0.1
        Assert.Equal(0.1, metrics.FalsePositiveRate);

        // FN Rate = 10 / (10 + 40) = 0.2
        Assert.Equal(0.2, metrics.FalseNegativeRate);
    }

    [Fact]
    public void AccuracyMetrics_WithZeroValues_HandlesGracefully()
    {
        // Arrange
        var metrics = new AccuracyMetrics
        {
            TotalDecisions = 0,
            TruePositives = 0,
            TrueNegatives = 0,
            FalsePositives = 0,
            FalseNegatives = 0
        };

        // Act & Assert - Should not throw and return default values
        Assert.Equal(1.0, metrics.Precision); // No FP means perfect precision
        Assert.Equal(1.0, metrics.Recall); // No FN means perfect recall
        Assert.Equal(1.0, metrics.F1Score); // 2 * (1.0 * 1.0) / (1.0 + 1.0) = 1.0
        Assert.Equal(1.0, metrics.Accuracy); // No decisions = no errors
    }

    [Fact]
    public void AuditQueryResult_HasMore_CalculatesCorrectly()
    {
        // Arrange & Act & Assert
        var withMore = new AuditQueryResult { Items = [], TotalCount = 100, Skip = 0, Take = 10 };
        Assert.True(withMore.HasMore);

        var atEnd = new AuditQueryResult { Items = [], TotalCount = 10, Skip = 5, Take = 10 };
        Assert.False(atEnd.HasMore);

        var exactFit = new AuditQueryResult { Items = [], TotalCount = 10, Skip = 0, Take = 10 };
        Assert.False(exactFit.HasMore);
    }
}
