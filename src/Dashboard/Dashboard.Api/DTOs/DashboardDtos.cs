namespace Dashboard.Api.DTOs;

/// <summary>
/// Response for dashboard overview.
/// </summary>
public sealed record DashboardOverviewResponse
{
    /// <summary>
    /// Total number of listings in the system.
    /// </summary>
    public int TotalListings { get; init; }

    /// <summary>
    /// Number of active listings.
    /// </summary>
    public int ActiveListings { get; init; }

    /// <summary>
    /// Total number of vehicles tracked.
    /// </summary>
    public int TotalVehicles { get; init; }

    /// <summary>
    /// Number of active alerts.
    /// </summary>
    public int ActiveAlerts { get; init; }

    /// <summary>
    /// Number of pending reviews.
    /// </summary>
    public int PendingReviews { get; init; }

    /// <summary>
    /// Number of new listings today.
    /// </summary>
    public int NewListingsToday { get; init; }

    /// <summary>
    /// Number of price changes today.
    /// </summary>
    public int PriceChangesToday { get; init; }

    /// <summary>
    /// Number of listings removed today.
    /// </summary>
    public int ListingsRemovedToday { get; init; }

    /// <summary>
    /// Timestamp when this overview was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; }
}

/// <summary>
/// Response for market trends.
/// </summary>
public sealed record MarketTrendsResponse
{
    /// <summary>
    /// Average price across all active listings.
    /// </summary>
    public decimal AveragePrice { get; init; }

    /// <summary>
    /// Median price across all active listings.
    /// </summary>
    public decimal MedianPrice { get; init; }

    /// <summary>
    /// Price change percentage compared to previous period.
    /// </summary>
    public decimal PriceChangePercent { get; init; }

    /// <summary>
    /// Number of listings by source.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsBySource { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Number of listings by make.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsByMake { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Number of listings by province.
    /// </summary>
    public IReadOnlyDictionary<string, int> ListingsByProvince { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Average price by make.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> AveragePriceByMake { get; init; } = new Dictionary<string, decimal>();

    /// <summary>
    /// Average mileage across all active listings.
    /// </summary>
    public int? AverageMileage { get; init; }

    /// <summary>
    /// Most common year in listings.
    /// </summary>
    public int? MostCommonYear { get; init; }

    /// <summary>
    /// Timestamp when these trends were calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; }
}

/// <summary>
/// Request for market trends with optional filters.
/// </summary>
public sealed record MarketTrendsRequest
{
    /// <summary>
    /// Optional filter by vehicle make.
    /// </summary>
    public string? Make { get; init; }

    /// <summary>
    /// Optional filter by province.
    /// </summary>
    public string? Province { get; init; }
}

/// <summary>
/// Response for scraping status.
/// </summary>
public sealed record ScrapingStatusResponse
{
    /// <summary>
    /// Number of currently active scraping jobs.
    /// </summary>
    public int ActiveJobs { get; init; }

    /// <summary>
    /// Number of scraping jobs completed today.
    /// </summary>
    public int CompletedToday { get; init; }

    /// <summary>
    /// Number of scraping jobs that failed today.
    /// </summary>
    public int FailedToday { get; init; }

    /// <summary>
    /// Next scheduled scraping job time.
    /// </summary>
    public DateTimeOffset? NextScheduledJob { get; init; }

    /// <summary>
    /// Total listings scraped today.
    /// </summary>
    public int ListingsScrapedToday { get; init; }

    /// <summary>
    /// Status breakdown by source.
    /// </summary>
    public IReadOnlyList<SourceScrapingStatusResponse> SourceStatuses { get; init; } = [];

    /// <summary>
    /// Last successful scrape time.
    /// </summary>
    public DateTimeOffset? LastSuccessfulScrape { get; init; }

    /// <summary>
    /// Timestamp when this status was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; init; }
}

/// <summary>
/// Response for source scraping status.
/// </summary>
public sealed record SourceScrapingStatusResponse
{
    /// <summary>
    /// Name of the source.
    /// </summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the scraper is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Last scrape time for this source.
    /// </summary>
    public DateTimeOffset? LastScrapeTime { get; init; }

    /// <summary>
    /// Number of listings from this source.
    /// </summary>
    public int ListingCount { get; init; }

    /// <summary>
    /// Success rate for this source (percentage).
    /// </summary>
    public decimal SuccessRate { get; init; }
}

/// <summary>
/// Response for health check.
/// </summary>
public sealed record HealthResponse
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public string Status { get; init; } = "Healthy";

    /// <summary>
    /// Service name.
    /// </summary>
    public string Service { get; init; } = "Dashboard";

    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Status of dependent services.
    /// </summary>
    public IReadOnlyDictionary<string, string> Dependencies { get; init; } = new Dictionary<string, string>();
}
