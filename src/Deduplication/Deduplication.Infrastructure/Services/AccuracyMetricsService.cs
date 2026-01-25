using Deduplication.Core.Enums;
using Deduplication.Core.Interfaces;
using Deduplication.Core.Models;
using Deduplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deduplication.Infrastructure.Services;

public sealed class AccuracyMetricsService : IAccuracyMetricsService
{
    private readonly DeduplicationDbContext _context;

    public AccuracyMetricsService(DeduplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccuracyMetrics> CalculateCurrentMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalMatches = await _context.DuplicateMatches.CountAsync(cancellationToken);

        var confirmedDuplicates = await _context.ReviewItems
            .CountAsync(r => r.Status == ReviewStatus.ConfirmedDuplicate, cancellationToken);

        var confirmedNotDuplicates = await _context.ReviewItems
            .CountAsync(r => r.Status == ReviewStatus.ConfirmedNotDuplicate, cancellationToken);

        var pendingReviews = await _context.ReviewItems
            .CountAsync(r => r.Status == ReviewStatus.Pending, cancellationToken);

        var averageScore = totalMatches > 0
            ? await _context.DuplicateMatches.AverageAsync(m => m.OverallScore, cancellationToken)
            : 0.0;

        return AccuracyMetrics.Calculate(
            totalMatches,
            confirmedDuplicates,
            confirmedNotDuplicates,
            pendingReviews,
            averageScore);
    }

    public Task<IReadOnlyList<AccuracyMetrics>> GetHistoricalMetricsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        // Historical metrics would require a separate metrics history table
        // For now, return empty list - can be extended later
        return Task.FromResult<IReadOnlyList<AccuracyMetrics>>([]);
    }

    public async Task RecordMetricsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // Record current metrics to history table for trending
        // Implementation would store snapshot to a MetricsHistory table
        var metrics = await CalculateCurrentMetricsAsync(cancellationToken);
        // Store metrics snapshot...
        await Task.CompletedTask;
    }
}
