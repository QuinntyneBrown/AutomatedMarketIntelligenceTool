using Deduplication.Api.DTOs;
using Deduplication.Core.Entities;
using Deduplication.Core.Interfaces;
using Deduplication.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Deduplication.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeduplicationController : ControllerBase
{
    private readonly IDuplicateDetectionService _detectionService;
    private readonly IAccuracyMetricsService _metricsService;

    public DeduplicationController(
        IDuplicateDetectionService detectionService,
        IAccuracyMetricsService metricsService)
    {
        _detectionService = detectionService;
        _metricsService = metricsService;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DeduplicationResultResponse>> AnalyzeWithCandidates(
        [FromBody] AnalyzeBatchRequest request,
        CancellationToken cancellationToken)
    {
        var sourceCandidate = request.Candidates.FirstOrDefault(c => c.ListingId == request.SourceListingId);
        if (sourceCandidate == null)
        {
            return BadRequest("Source listing must be included in candidates");
        }

        var sourceListing = MapToListingData(sourceCandidate);
        var candidates = request.Candidates
            .Where(c => c.ListingId != request.SourceListingId)
            .Select(MapToListingData)
            .ToList();

        var result = await _detectionService.DetectDuplicatesAsync(
            sourceListing, candidates, cancellationToken);

        return Ok(MapToResponse(result));
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<AccuracyMetricsResponse>> GetMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _metricsService.CalculateCurrentMetricsAsync(cancellationToken);

        return Ok(new AccuracyMetricsResponse(
            metrics.TotalMatches,
            metrics.ConfirmedDuplicates,
            metrics.ConfirmedNotDuplicates,
            metrics.PendingReviews,
            metrics.Precision,
            metrics.Recall,
            metrics.F1Score,
            metrics.AverageMatchScore,
            metrics.CalculatedAt));
    }

    private static ListingData MapToListingData(AnalyzeListingRequest request)
    {
        return new ListingData
        {
            Id = request.ListingId,
            Title = request.Title,
            VIN = request.VIN,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            ImageHash = request.ImageHash,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Source = request.Source,
            SourceListingId = request.SourceListingId
        };
    }

    private static DeduplicationResultResponse MapToResponse(DeduplicationResult result)
    {
        return new DeduplicationResultResponse(
            result.AnalyzedListingId,
            result.Duplicates.Select(MapDuplicateMatch).ToList(),
            result.ReviewItems.Select(MapReviewItem).ToList(),
            result.TotalCandidatesChecked,
            result.Duration,
            result.Success,
            result.ErrorMessage);
    }

    private static DuplicateMatchResponse MapDuplicateMatch(DuplicateMatch d)
    {
        return new DuplicateMatchResponse(
            d.Id,
            d.SourceListingId,
            d.TargetListingId,
            d.OverallScore,
            d.Confidence.ToString(),
            new MatchScoreBreakdownResponse(
                d.ScoreBreakdown.TitleScore,
                d.ScoreBreakdown.VinScore,
                d.ScoreBreakdown.ImageHashScore,
                d.ScoreBreakdown.PriceScore,
                d.ScoreBreakdown.MileageScore,
                d.ScoreBreakdown.LocationScore,
                d.ScoreBreakdown.MatchReason),
            d.DetectedAt,
            d.IsConfirmed);
    }

    private static ReviewItemResponse MapReviewItem(ReviewItem r)
    {
        return new ReviewItemResponse(
            r.Id,
            r.DuplicateMatchId,
            r.SourceListingId,
            r.TargetListingId,
            r.MatchScore,
            r.Priority,
            r.Status.ToString(),
            r.ReviewedBy,
            r.ReviewedAt,
            r.ReviewNotes,
            r.CreatedAt);
    }
}
