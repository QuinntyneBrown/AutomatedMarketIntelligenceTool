using Deduplication.Core.Enums;

namespace Deduplication.Api.DTOs;

// Request DTOs
public sealed record AnalyzeListingRequest(
    Guid ListingId,
    string Title,
    string? VIN,
    string? Make,
    string? Model,
    int? Year,
    decimal? Price,
    int? Mileage,
    string? ImageHash,
    string? City,
    string? Province,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string Source,
    string SourceListingId);

public sealed record AnalyzeBatchRequest(
    Guid SourceListingId,
    List<AnalyzeListingRequest> Candidates);

public sealed record CreateConfigRequest(
    string Name,
    double OverallMatchThreshold,
    double ReviewThreshold,
    double VinWeight,
    double TitleWeight,
    double PriceWeight,
    double LocationWeight,
    double ImageHashWeight);

public sealed record UpdateConfigRequest(
    double OverallMatchThreshold,
    double ReviewThreshold,
    double VinWeight,
    double TitleWeight,
    double PriceWeight,
    double LocationWeight,
    double ImageHashWeight);

public sealed record ResolveReviewRequest(
    ReviewStatus Status,
    string? ReviewedBy,
    string? Notes);

// Response DTOs
public sealed record MatchScoreBreakdownResponse(
    double TitleScore,
    double VinScore,
    double ImageHashScore,
    double PriceScore,
    double MileageScore,
    double LocationScore,
    string? MatchReason);

public sealed record DuplicateMatchResponse(
    Guid Id,
    Guid SourceListingId,
    Guid TargetListingId,
    double OverallScore,
    string Confidence,
    MatchScoreBreakdownResponse ScoreBreakdown,
    DateTimeOffset DetectedAt,
    bool IsConfirmed);

public sealed record ReviewItemResponse(
    Guid Id,
    Guid DuplicateMatchId,
    Guid SourceListingId,
    Guid TargetListingId,
    double MatchScore,
    int Priority,
    string Status,
    Guid? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? ReviewNotes,
    DateTimeOffset CreatedAt);

public sealed record DeduplicationConfigResponse(
    Guid Id,
    string Name,
    bool IsActive,
    double TitleSimilarityThreshold,
    double ImageHashSimilarityThreshold,
    double OverallMatchThreshold,
    double ReviewThreshold,
    double TitleWeight,
    double VinWeight,
    double ImageHashWeight,
    double PriceWeight,
    double MileageWeight,
    double LocationWeight,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record DeduplicationResultResponse(
    Guid AnalyzedListingId,
    List<DuplicateMatchResponse> Duplicates,
    List<ReviewItemResponse> ReviewItems,
    int TotalCandidatesChecked,
    TimeSpan Duration,
    bool Success,
    string? ErrorMessage);

public sealed record AccuracyMetricsResponse(
    int TotalMatches,
    int ConfirmedDuplicates,
    int ConfirmedNotDuplicates,
    int PendingReviews,
    double Precision,
    double Recall,
    double F1Score,
    double AverageMatchScore,
    DateTimeOffset CalculatedAt);
