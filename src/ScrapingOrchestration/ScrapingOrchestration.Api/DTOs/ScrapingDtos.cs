using ScrapingOrchestration.Core.Enums;

namespace ScrapingOrchestration.Api.DTOs;

/// <summary>
/// Request to create a scraping job.
/// </summary>
public sealed record CreateScrapingJobRequest
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? PostalCode { get; init; }
    public int? RadiusKm { get; init; }
    public string? Province { get; init; }
    public IReadOnlyList<ScrapingSource> Sources { get; init; } = [];
    public int MaxResults { get; init; } = 100;
}

/// <summary>
/// Response for a scraping session.
/// </summary>
public sealed record ScrapingSessionResponse
{
    public Guid Id { get; init; }
    public SearchSessionStatus Status { get; init; }
    public IReadOnlyList<ScrapingSource> Sources { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int TotalListingsFound { get; init; }
    public int TotalErrors { get; init; }
    public string? ErrorMessage { get; init; }
    public SearchParametersDto Parameters { get; init; } = new();
}

/// <summary>
/// Response for a scraping job.
/// </summary>
public sealed record ScrapingJobResponse
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public ScrapingSource Source { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int ListingsFound { get; init; }
    public int PagesCrawled { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
}

/// <summary>
/// Search parameters DTO.
/// </summary>
public sealed record SearchParametersDto
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? PostalCode { get; init; }
    public int? RadiusKm { get; init; }
    public string? Province { get; init; }
    public int MaxResults { get; init; }
}

/// <summary>
/// Health status response.
/// </summary>
public sealed record ScrapingHealthResponse
{
    public string Status { get; init; } = "Healthy";
    public int ActiveSessions { get; init; }
    public int PendingJobs { get; init; }
    public int RunningJobs { get; init; }
    public IDictionary<string, SourceHealthStatus> SourceStatuses { get; init; } = new Dictionary<string, SourceHealthStatus>();
}

/// <summary>
/// Health status for a specific source.
/// </summary>
public sealed record SourceHealthStatus
{
    public bool IsHealthy { get; init; }
    public int SuccessRate { get; init; }
    public DateTimeOffset? LastSuccessAt { get; init; }
    public DateTimeOffset? LastFailureAt { get; init; }
}
