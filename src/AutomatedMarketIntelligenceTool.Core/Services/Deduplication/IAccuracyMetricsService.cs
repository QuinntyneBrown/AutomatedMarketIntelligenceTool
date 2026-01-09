using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for calculating and reporting deduplication accuracy metrics.
/// </summary>
public interface IAccuracyMetricsService
{
    /// <summary>
    /// Calculates comprehensive accuracy metrics for a tenant.
    /// </summary>
    Task<AccuracyMetrics> CalculateMetricsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accuracy metrics broken down by matching method.
    /// </summary>
    Task<Dictionary<AuditReason, AccuracyMetrics>> GetMetricsByReasonAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accuracy trend over time.
    /// </summary>
    Task<IReadOnlyList<AccuracyTrendPoint>> GetAccuracyTrendAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        TrendGranularity granularity = TrendGranularity.Daily,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets threshold analysis showing accuracy at different confidence thresholds.
    /// </summary>
    Task<IReadOnlyList<ThresholdAnalysis>> GetThresholdAnalysisAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Comprehensive accuracy metrics for deduplication.
/// </summary>
public class AccuracyMetrics
{
    public int TotalDecisions { get; init; }
    public int TruePositives { get; init; }
    public int TrueNegatives { get; init; }
    public int FalsePositives { get; init; }
    public int FalseNegatives { get; init; }

    /// <summary>
    /// Precision = TP / (TP + FP)
    /// How many of the identified duplicates are actually duplicates.
    /// </summary>
    public double Precision => TruePositives + FalsePositives > 0
        ? (double)TruePositives / (TruePositives + FalsePositives)
        : 1.0;

    /// <summary>
    /// Recall = TP / (TP + FN)
    /// How many of the actual duplicates were correctly identified.
    /// </summary>
    public double Recall => TruePositives + FalseNegatives > 0
        ? (double)TruePositives / (TruePositives + FalseNegatives)
        : 1.0;

    /// <summary>
    /// F1 Score = 2 * (Precision * Recall) / (Precision + Recall)
    /// Harmonic mean of precision and recall.
    /// </summary>
    public double F1Score => Precision + Recall > 0
        ? 2 * (Precision * Recall) / (Precision + Recall)
        : 0;

    /// <summary>
    /// Overall accuracy = (TP + TN) / Total
    /// </summary>
    public double Accuracy => TotalDecisions > 0
        ? (double)(TruePositives + TrueNegatives) / TotalDecisions
        : 1.0;

    /// <summary>
    /// False positive rate = FP / (FP + TN)
    /// </summary>
    public double FalsePositiveRate => FalsePositives + TrueNegatives > 0
        ? (double)FalsePositives / (FalsePositives + TrueNegatives)
        : 0;

    /// <summary>
    /// False negative rate = FN / (FN + TP)
    /// </summary>
    public double FalseNegativeRate => FalseNegatives + TruePositives > 0
        ? (double)FalseNegatives / (FalseNegatives + TruePositives)
        : 0;

    /// <summary>
    /// Specificity = TN / (TN + FP)
    /// How many non-duplicates were correctly identified.
    /// </summary>
    public double Specificity => TrueNegatives + FalsePositives > 0
        ? (double)TrueNegatives / (TrueNegatives + FalsePositives)
        : 1.0;

    /// <summary>
    /// Period covered by these metrics.
    /// </summary>
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// A single point in an accuracy trend over time.
/// </summary>
public class AccuracyTrendPoint
{
    public DateTime Date { get; init; }
    public int TotalDecisions { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public double Accuracy { get; init; }
    public int FalsePositives { get; init; }
    public int FalseNegatives { get; init; }
}

/// <summary>
/// Granularity for trend analysis.
/// </summary>
public enum TrendGranularity
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Analysis of accuracy at a specific confidence threshold.
/// </summary>
public class ThresholdAnalysis
{
    public int Threshold { get; init; }
    public int TotalAtOrAbove { get; init; }
    public int TruePositives { get; init; }
    public int FalsePositives { get; init; }
    public double Precision { get; init; }
    public double CumulativeRecall { get; init; }
}
