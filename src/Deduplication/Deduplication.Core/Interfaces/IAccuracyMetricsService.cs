using Deduplication.Core.Models;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Service for calculating and tracking deduplication accuracy metrics.
/// </summary>
public interface IAccuracyMetricsService
{
    Task<AccuracyMetrics> CalculateCurrentMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccuracyMetrics>> GetHistoricalMetricsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task RecordMetricsSnapshotAsync(CancellationToken cancellationToken = default);
}
