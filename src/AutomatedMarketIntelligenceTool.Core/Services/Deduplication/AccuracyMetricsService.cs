using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for calculating and reporting deduplication accuracy metrics.
/// </summary>
public class AccuracyMetricsService : IAccuracyMetricsService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<AccuracyMetricsService> _logger;

    public AccuracyMetricsService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<AccuracyMetricsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AccuracyMetrics> CalculateMetricsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetFilteredEntriesAsync(tenantId, fromDate, toDate, cancellationToken);

        var (tp, tn, fp, fn) = CalculateConfusionMatrix(entries);

        return new AccuracyMetrics
        {
            TotalDecisions = entries.Count,
            TruePositives = tp,
            TrueNegatives = tn,
            FalsePositives = fp,
            FalseNegatives = fn,
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<Dictionary<AuditReason, AccuracyMetrics>> GetMetricsByReasonAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetFilteredEntriesAsync(tenantId, fromDate, toDate, cancellationToken);

        var groupedByReason = entries
            .GroupBy(e => e.Reason)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var (tp, tn, fp, fn) = CalculateConfusionMatrix(g.ToList());
                    return new AccuracyMetrics
                    {
                        TotalDecisions = g.Count(),
                        TruePositives = tp,
                        TrueNegatives = tn,
                        FalsePositives = fp,
                        FalseNegatives = fn,
                        FromDate = fromDate,
                        ToDate = toDate
                    };
                });

        return groupedByReason;
    }

    public async Task<IReadOnlyList<AccuracyTrendPoint>> GetAccuracyTrendAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        TrendGranularity granularity = TrendGranularity.Daily,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetFilteredEntriesAsync(tenantId, fromDate, toDate, cancellationToken);

        var grouped = granularity switch
        {
            TrendGranularity.Weekly => entries.GroupBy(e => GetWeekStart(e.CreatedAt)),
            TrendGranularity.Monthly => entries.GroupBy(e => new DateTime(e.CreatedAt.Year, e.CreatedAt.Month, 1)),
            _ => entries.GroupBy(e => e.CreatedAt.Date)
        };

        var trendPoints = grouped
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var groupEntries = g.ToList();
                var (tp, tn, fp, fn) = CalculateConfusionMatrix(groupEntries);

                var precision = tp + fp > 0 ? (double)tp / (tp + fp) : 1.0;
                var recall = tp + fn > 0 ? (double)tp / (tp + fn) : 1.0;
                var f1 = precision + recall > 0 ? 2 * (precision * recall) / (precision + recall) : 0;
                var accuracy = groupEntries.Count > 0 ? (double)(tp + tn) / groupEntries.Count : 1.0;

                return new AccuracyTrendPoint
                {
                    Date = g.Key,
                    TotalDecisions = groupEntries.Count,
                    Precision = precision,
                    Recall = recall,
                    F1Score = f1,
                    Accuracy = accuracy,
                    FalsePositives = fp,
                    FalseNegatives = fn
                };
            })
            .ToList();

        return trendPoints;
    }

    public async Task<IReadOnlyList<ThresholdAnalysis>> GetThresholdAnalysisAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await GetFilteredEntriesAsync(tenantId, fromDate, toDate, cancellationToken);

        // Only include entries with confidence scores (fuzzy/image matches)
        var entriesWithScores = entries
            .Where(e => e.ConfidenceScore.HasValue)
            .ToList();

        if (!entriesWithScores.Any())
            return [];

        // Analyze at different threshold levels
        var thresholds = new[] { 50, 60, 70, 75, 80, 85, 90, 95 };
        var totalTruePositives = entriesWithScores.Count(e =>
            e.Decision == AuditDecision.Duplicate && !e.IsFalsePositive);

        var results = thresholds.Select(threshold =>
        {
            var atOrAbove = entriesWithScores
                .Where(e => e.ConfidenceScore >= threshold)
                .ToList();

            var tp = atOrAbove.Count(e =>
                e.Decision == AuditDecision.Duplicate && !e.IsFalsePositive);
            var fp = atOrAbove.Count(e =>
                e.Decision == AuditDecision.Duplicate && e.IsFalsePositive);

            var precision = tp + fp > 0 ? (double)tp / (tp + fp) : 1.0;
            var cumulativeRecall = totalTruePositives > 0 ? (double)tp / totalTruePositives : 1.0;

            return new ThresholdAnalysis
            {
                Threshold = threshold,
                TotalAtOrAbove = atOrAbove.Count,
                TruePositives = tp,
                FalsePositives = fp,
                Precision = precision,
                CumulativeRecall = cumulativeRecall
            };
        }).ToList();

        return results;
    }

    private async Task<List<AuditEntry>> GetFilteredEntriesAsync(
        Guid tenantId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditEntries
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query.ToListAsync(cancellationToken);
    }

    private static (int TruePositives, int TrueNegatives, int FalsePositives, int FalseNegatives) CalculateConfusionMatrix(
        IList<AuditEntry> entries)
    {
        // True Positive: Correctly identified as duplicate
        // - Decision was Duplicate AND not marked as false positive
        var truePositives = entries.Count(e =>
            e.Decision == AuditDecision.Duplicate && !e.IsFalsePositive);

        // True Negative: Correctly identified as not duplicate
        // - Decision was NewListing AND not marked as false negative
        var trueNegatives = entries.Count(e =>
            e.Decision == AuditDecision.NewListing && !e.IsFalseNegative);

        // False Positive: Incorrectly identified as duplicate
        // - Decision was Duplicate AND marked as false positive
        var falsePositives = entries.Count(e =>
            e.Decision == AuditDecision.Duplicate && e.IsFalsePositive);

        // False Negative: Missed duplicate (incorrectly marked as new)
        // - Decision was NewListing AND marked as false negative
        var falseNegatives = entries.Count(e =>
            e.Decision == AuditDecision.NewListing && e.IsFalseNegative);

        return (truePositives, trueNegatives, falsePositives, falseNegatives);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
