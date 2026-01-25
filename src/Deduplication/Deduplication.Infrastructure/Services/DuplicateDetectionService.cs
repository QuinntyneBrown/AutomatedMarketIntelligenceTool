using System.Diagnostics;
using Deduplication.Core.Entities;
using Deduplication.Core.Events;
using Deduplication.Core.Interfaces;
using Deduplication.Core.Models;
using Deduplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;

namespace Deduplication.Infrastructure.Services;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly DeduplicationDbContext _context;
    private readonly IFuzzyMatchingService _matchingService;
    private readonly IDeduplicationConfigService _configService;
    private readonly IEventPublisher _eventPublisher;

    public DuplicateDetectionService(
        DeduplicationDbContext context,
        IFuzzyMatchingService matchingService,
        IDeduplicationConfigService configService,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _matchingService = matchingService;
        _configService = configService;
        _eventPublisher = eventPublisher;
    }

    public async Task<DeduplicationResult> DetectDuplicatesAsync(
        ListingData listing,
        CancellationToken cancellationToken = default)
    {
        // This method would need to fetch candidates from a listing service
        // For now, return empty result - full implementation requires integration
        var stopwatch = Stopwatch.StartNew();

        return DeduplicationResult.Successful(
            listing.Id,
            [],
            [],
            0,
            stopwatch.Elapsed);
    }

    public async Task<DeduplicationResult> DetectDuplicatesAsync(
        ListingData listing,
        IEnumerable<ListingData> candidates,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var config = await _configService.GetActiveConfigAsync(cancellationToken);
            var candidateList = candidates.ToList();

            var matchResults = await _matchingService.FindPotentialMatchesAsync(
                listing, candidateList, config, cancellationToken);

            var duplicates = new List<DuplicateMatch>();
            var reviewItems = new List<ReviewItem>();

            foreach (var result in matchResults)
            {
                if (result.IsAboveThreshold)
                {
                    var duplicate = await CreateDuplicateMatchAsync(result, cancellationToken);
                    duplicates.Add(duplicate);

                    await _eventPublisher.PublishAsync(new DuplicateFoundEvent
                    {
                        DuplicateMatchId = duplicate.Id,
                        SourceListingId = duplicate.SourceListingId,
                        TargetListingId = duplicate.TargetListingId,
                        MatchScore = duplicate.OverallScore,
                        Confidence = duplicate.Confidence
                    }, cancellationToken);
                }
                else if (result.RequiresReview)
                {
                    // First create the duplicate match entry
                    var duplicate = await CreateDuplicateMatchAsync(result, cancellationToken);
                    duplicates.Add(duplicate);

                    // Then create review item linked to it
                    var reviewItem = ReviewItem.Create(
                        duplicate.Id,
                        result.SourceId,
                        result.TargetId,
                        result.OverallScore);

                    _context.ReviewItems.Add(reviewItem);
                    await _context.SaveChangesAsync(cancellationToken);
                    reviewItems.Add(reviewItem);

                    duplicate.LinkToReview(reviewItem.Id);
                    await _context.SaveChangesAsync(cancellationToken);

                    await _eventPublisher.PublishAsync(new ReviewRequiredEvent
                    {
                        ReviewItemId = reviewItem.Id,
                        SourceListingId = reviewItem.SourceListingId,
                        TargetListingId = reviewItem.TargetListingId,
                        MatchScore = reviewItem.MatchScore,
                        Priority = reviewItem.Priority
                    }, cancellationToken);
                }
            }

            stopwatch.Stop();

            await _eventPublisher.PublishAsync(new DeduplicationCompletedEvent
            {
                ListingId = listing.Id,
                DuplicatesFound = duplicates.Count,
                ReviewItemsCreated = reviewItems.Count,
                AnalysisDuration = stopwatch.Elapsed
            }, cancellationToken);

            return DeduplicationResult.Successful(
                listing.Id,
                duplicates,
                reviewItems,
                candidateList.Count,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return DeduplicationResult.Failed(listing.Id, ex.Message, stopwatch.Elapsed);
        }
    }

    public async Task ProcessBatchAsync(
        IEnumerable<Guid> listingIds,
        CancellationToken cancellationToken = default)
    {
        // Batch processing would require integration with listing service
        // Placeholder for future implementation
        await Task.CompletedTask;
    }

    private async Task<DuplicateMatch> CreateDuplicateMatchAsync(
        MatchResult result,
        CancellationToken cancellationToken)
    {
        // Check if match already exists
        var existing = await _context.DuplicateMatches
            .FirstOrDefaultAsync(m =>
                (m.SourceListingId == result.SourceId && m.TargetListingId == result.TargetId) ||
                (m.SourceListingId == result.TargetId && m.TargetListingId == result.SourceId),
                cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var breakdown = new MatchScoreBreakdown
        {
            VinScore = result.VinScore,
            TitleScore = result.TitleScore,
            PriceScore = result.PriceScore,
            LocationScore = result.LocationScore,
            ImageHashScore = result.ImageScore
        };

        var match = DuplicateMatch.Create(
            result.SourceId,
            result.TargetId,
            result.OverallScore,
            breakdown);

        _context.DuplicateMatches.Add(match);
        await _context.SaveChangesAsync(cancellationToken);

        return match;
    }
}
