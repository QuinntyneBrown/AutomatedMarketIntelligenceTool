namespace Deduplication.Core.Models;

/// <summary>
/// Metrics for deduplication accuracy.
/// </summary>
public sealed record AccuracyMetrics
{
    public int TotalMatches { get; init; }
    public int ConfirmedDuplicates { get; init; }
    public int ConfirmedNotDuplicates { get; init; }
    public int PendingReviews { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public double AverageMatchScore { get; init; }
    public DateTimeOffset CalculatedAt { get; init; }

    public static AccuracyMetrics Calculate(
        int totalMatches,
        int confirmedDuplicates,
        int confirmedNotDuplicates,
        int pendingReviews,
        double averageScore)
    {
        var truePositives = confirmedDuplicates;
        var falsePositives = confirmedNotDuplicates;
        var reviewed = truePositives + falsePositives;

        var precision = reviewed > 0 ? (double)truePositives / reviewed : 0;
        var recall = totalMatches > 0 ? (double)truePositives / totalMatches : 0;
        var f1 = precision + recall > 0 ? 2 * (precision * recall) / (precision + recall) : 0;

        return new AccuracyMetrics
        {
            TotalMatches = totalMatches,
            ConfirmedDuplicates = confirmedDuplicates,
            ConfirmedNotDuplicates = confirmedNotDuplicates,
            PendingReviews = pendingReviews,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            AverageMatchScore = averageScore,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }
}
