using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrapingOrchestration.Api.DTOs;
using ScrapingOrchestration.Core.Entities;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.Services;
using ScrapingOrchestration.Core.ValueObjects;
using ScrapingOrchestration.Infrastructure.Data;

namespace ScrapingOrchestration.Api.Controllers;

/// <summary>
/// API controller for scraping operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ScrapingController : ControllerBase
{
    private readonly ISearchSessionService _sessionService;
    private readonly ScrapingOrchestrationDbContext _dbContext;
    private readonly ILogger<ScrapingController> _logger;

    public ScrapingController(
        ISearchSessionService sessionService,
        ScrapingOrchestrationDbContext dbContext,
        ILogger<ScrapingController> logger)
    {
        _sessionService = sessionService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new scraping job.
    /// </summary>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(ScrapingSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScrapingSessionResponse>> CreateJob(
        [FromBody] CreateScrapingJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Sources.Count == 0)
        {
            return BadRequest("At least one source must be specified");
        }

        var parameters = new SearchParameters
        {
            Make = request.Make,
            Model = request.Model,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            MaxMileage = request.MaxMileage,
            PostalCode = request.PostalCode,
            RadiusKm = request.RadiusKm,
            Province = request.Province,
            MaxResults = request.MaxResults
        };

        if (!parameters.IsValid())
        {
            return BadRequest("Invalid search parameters");
        }

        var session = await _sessionService.CreateSessionAsync(
            parameters,
            request.Sources,
            cancellationToken: cancellationToken);

        return CreatedAtAction(
            nameof(GetJob),
            new { id = session.Id },
            MapToResponse(session));
    }

    /// <summary>
    /// Gets a scraping session by ID.
    /// </summary>
    [HttpGet("jobs/{id:guid}")]
    [ProducesResponseType(typeof(ScrapingSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScrapingSessionResponse>> GetJob(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(id, cancellationToken);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(session));
    }

    /// <summary>
    /// Cancels a scraping session.
    /// </summary>
    [HttpPost("jobs/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelJob(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(id, cancellationToken);
        if (session == null)
        {
            return NotFound();
        }

        await _sessionService.CancelSessionAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets all sessions.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<ScrapingSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScrapingSessionResponse>>> GetSessions(
        [FromQuery] SearchSessionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SearchSession> sessions;

        if (status.HasValue)
        {
            sessions = await _sessionService.GetSessionsByStatusAsync(status.Value, cancellationToken);
        }
        else
        {
            sessions = await _dbContext.Sessions
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        return Ok(sessions.Select(MapToResponse));
    }

    /// <summary>
    /// Gets jobs for a session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/jobs")]
    [ProducesResponseType(typeof(IEnumerable<ScrapingJobResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScrapingJobResponse>>> GetSessionJobs(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.Jobs
            .Where(j => j.SessionId == sessionId)
            .OrderBy(j => j.Source)
            .ToListAsync(cancellationToken);

        return Ok(jobs.Select(MapJobToResponse));
    }

    /// <summary>
    /// Gets health status of the scraping service.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ScrapingHealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScrapingHealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        var activeSessions = await _dbContext.Sessions
            .CountAsync(s => s.Status == SearchSessionStatus.Running, cancellationToken);

        var pendingJobs = await _dbContext.Jobs
            .CountAsync(j => j.Status == ScrapingJobStatus.Pending, cancellationToken);

        var runningJobs = await _dbContext.Jobs
            .CountAsync(j => j.Status == ScrapingJobStatus.Running, cancellationToken);

        return Ok(new ScrapingHealthResponse
        {
            Status = "Healthy",
            ActiveSessions = activeSessions,
            PendingJobs = pendingJobs,
            RunningJobs = runningJobs
        });
    }

    private static ScrapingSessionResponse MapToResponse(SearchSession session)
    {
        return new ScrapingSessionResponse
        {
            Id = session.Id,
            Status = session.Status,
            Sources = session.Sources,
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            TotalListingsFound = session.TotalListingsFound,
            TotalErrors = session.TotalErrors,
            ErrorMessage = session.ErrorMessage,
            Parameters = new SearchParametersDto
            {
                Make = session.Parameters.Make,
                Model = session.Parameters.Model,
                YearFrom = session.Parameters.YearFrom,
                YearTo = session.Parameters.YearTo,
                MinPrice = session.Parameters.MinPrice,
                MaxPrice = session.Parameters.MaxPrice,
                MaxMileage = session.Parameters.MaxMileage,
                PostalCode = session.Parameters.PostalCode,
                RadiusKm = session.Parameters.RadiusKm,
                Province = session.Parameters.Province,
                MaxResults = session.Parameters.MaxResults
            }
        };
    }

    private static ScrapingJobResponse MapJobToResponse(ScrapingJob job)
    {
        return new ScrapingJobResponse
        {
            Id = job.Id,
            SessionId = job.SessionId,
            Source = job.Source,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ListingsFound = job.ListingsFound,
            PagesCrawled = job.PagesCrawled,
            ErrorMessage = job.ErrorMessage,
            RetryCount = job.RetryCount
        };
    }
}
