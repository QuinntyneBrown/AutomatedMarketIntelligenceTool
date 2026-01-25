using Deduplication.Api.DTOs;
using Deduplication.Core.Enums;
using Deduplication.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Deduplication.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DuplicatesController : ControllerBase
{
    private readonly IDuplicateMatchRepository _repository;

    public DuplicatesController(IDuplicateMatchRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DuplicateMatchResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(id, cancellationToken);
        if (match == null)
        {
            return NotFound();
        }

        return Ok(new DuplicateMatchResponse(
            match.Id,
            match.SourceListingId,
            match.TargetListingId,
            match.OverallScore,
            match.Confidence.ToString(),
            new MatchScoreBreakdownResponse(
                match.ScoreBreakdown.TitleScore,
                match.ScoreBreakdown.VinScore,
                match.ScoreBreakdown.ImageHashScore,
                match.ScoreBreakdown.PriceScore,
                match.ScoreBreakdown.MileageScore,
                match.ScoreBreakdown.LocationScore,
                match.ScoreBreakdown.MatchReason),
            match.DetectedAt,
            match.IsConfirmed));
    }

    [HttpGet("listing/{listingId:guid}")]
    public async Task<ActionResult<IEnumerable<DuplicateMatchResponse>>> GetByListingId(
        Guid listingId,
        CancellationToken cancellationToken)
    {
        var matches = await _repository.GetByListingIdAsync(listingId, cancellationToken);

        return Ok(matches.Select(m => new DuplicateMatchResponse(
            m.Id,
            m.SourceListingId,
            m.TargetListingId,
            m.OverallScore,
            m.Confidence.ToString(),
            new MatchScoreBreakdownResponse(
                m.ScoreBreakdown.TitleScore,
                m.ScoreBreakdown.VinScore,
                m.ScoreBreakdown.ImageHashScore,
                m.ScoreBreakdown.PriceScore,
                m.ScoreBreakdown.MileageScore,
                m.ScoreBreakdown.LocationScore,
                m.ScoreBreakdown.MatchReason),
            m.DetectedAt,
            m.IsConfirmed)));
    }

    [HttpGet("confidence/{confidence}")]
    public async Task<ActionResult<IEnumerable<DuplicateMatchResponse>>> GetByConfidence(
        MatchConfidence confidence,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var matches = await _repository.GetByConfidenceAsync(confidence, skip, take, cancellationToken);

        return Ok(matches.Select(m => new DuplicateMatchResponse(
            m.Id,
            m.SourceListingId,
            m.TargetListingId,
            m.OverallScore,
            m.Confidence.ToString(),
            new MatchScoreBreakdownResponse(
                m.ScoreBreakdown.TitleScore,
                m.ScoreBreakdown.VinScore,
                m.ScoreBreakdown.ImageHashScore,
                m.ScoreBreakdown.PriceScore,
                m.ScoreBreakdown.MileageScore,
                m.ScoreBreakdown.LocationScore,
                m.ScoreBreakdown.MatchReason),
            m.DetectedAt,
            m.IsConfirmed)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
