using Deduplication.Api.DTOs;
using Deduplication.Core.Entities;
using Deduplication.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Deduplication.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReviewItemResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var review = await _reviewService.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(review));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<ReviewItemResponse>>> GetPending(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewService.GetPendingReviewsAsync(skip, take, cancellationToken);
        return Ok(reviews.Select(MapToResponse));
    }

    [HttpGet("pending/count")]
    public async Task<ActionResult<int>> GetPendingCount(CancellationToken cancellationToken)
    {
        var count = await _reviewService.GetPendingCountAsync(cancellationToken);
        return Ok(count);
    }

    [HttpGet("priority/{minPriority:int}")]
    public async Task<ActionResult<IEnumerable<ReviewItemResponse>>> GetByPriority(
        int minPriority,
        CancellationToken cancellationToken)
    {
        var reviews = await _reviewService.GetByPriorityAsync(minPriority, cancellationToken);
        return Ok(reviews.Select(MapToResponse));
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveReviewRequest request,
        CancellationToken cancellationToken)
    {
        var review = await _reviewService.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            return NotFound();
        }

        await _reviewService.ResolveAsync(
            id,
            request.Status,
            request.ReviewedBy,
            request.Notes,
            cancellationToken);

        return NoContent();
    }

    private static ReviewItemResponse MapToResponse(ReviewItem review)
    {
        return new ReviewItemResponse(
            review.Id,
            review.DuplicateMatchId,
            review.SourceListingId,
            review.TargetListingId,
            review.MatchScore,
            review.Priority,
            review.Status.ToString(),
            review.ReviewedBy,
            review.ReviewedAt,
            review.ReviewNotes,
            review.CreatedAt);
    }
}
