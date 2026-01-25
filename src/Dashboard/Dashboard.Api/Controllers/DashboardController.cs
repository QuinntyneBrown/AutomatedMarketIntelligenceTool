using Dashboard.Api.DTOs;
using Dashboard.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers;

/// <summary>
/// API controller for dashboard operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the dashboard overview with key metrics.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting dashboard overview");

        var overview = await _dashboardService.GetOverviewAsync(cancellationToken);

        return Ok(new DashboardOverviewResponse
        {
            TotalListings = overview.TotalListings,
            ActiveListings = overview.ActiveListings,
            TotalVehicles = overview.TotalVehicles,
            ActiveAlerts = overview.ActiveAlerts,
            PendingReviews = overview.PendingReviews,
            NewListingsToday = overview.NewListingsToday,
            PriceChangesToday = overview.PriceChangesToday,
            ListingsRemovedToday = overview.ListingsRemovedToday,
            GeneratedAt = overview.GeneratedAt
        });
    }

    /// <summary>
    /// Gets market trends and analytics.
    /// </summary>
    [HttpGet("market-trends")]
    [ProducesResponseType(typeof(MarketTrendsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarketTrendsResponse>> GetMarketTrends(
        [FromQuery] MarketTrendsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting market trends for Make={Make}, Province={Province}", request.Make, request.Province);

        var trends = await _dashboardService.GetMarketTrendsAsync(request.Make, request.Province, cancellationToken);

        return Ok(new MarketTrendsResponse
        {
            AveragePrice = trends.AveragePrice,
            MedianPrice = trends.MedianPrice,
            PriceChangePercent = trends.PriceChangePercent,
            ListingsBySource = trends.ListingsBySource,
            ListingsByMake = trends.ListingsByMake,
            ListingsByProvince = trends.ListingsByProvince,
            AveragePriceByMake = trends.AveragePriceByMake,
            AverageMileage = trends.AverageMileage,
            MostCommonYear = trends.MostCommonYear,
            CalculatedAt = trends.CalculatedAt
        });
    }

    /// <summary>
    /// Gets the current scraping status.
    /// </summary>
    [HttpGet("scraping-status")]
    [ProducesResponseType(typeof(ScrapingStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScrapingStatusResponse>> GetScrapingStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting scraping status");

        var status = await _dashboardService.GetScrapingStatusAsync(cancellationToken);

        return Ok(new ScrapingStatusResponse
        {
            ActiveJobs = status.ActiveJobs,
            CompletedToday = status.CompletedToday,
            FailedToday = status.FailedToday,
            NextScheduledJob = status.NextScheduledJob,
            ListingsScrapedToday = status.ListingsScrapedToday,
            SourceStatuses = status.SourceStatuses.Select(s => new SourceScrapingStatusResponse
            {
                SourceName = s.SourceName,
                IsActive = s.IsActive,
                LastScrapeTime = s.LastScrapeTime,
                ListingCount = s.ListingCount,
                SuccessRate = s.SuccessRate
            }).ToList(),
            LastSuccessfulScrape = status.LastSuccessfulScrape,
            RetrievedAt = status.RetrievedAt
        });
    }

    /// <summary>
    /// Gets the health status of the dashboard service.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Service = "Dashboard",
            Timestamp = DateTimeOffset.UtcNow,
            Dependencies = new Dictionary<string, string>
            {
                ["ListingService"] = "Unknown",
                ["VehicleService"] = "Unknown",
                ["AlertService"] = "Unknown",
                ["ScrapingService"] = "Unknown"
            }
        });
    }
}
