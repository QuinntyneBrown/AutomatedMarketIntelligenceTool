using System.Collections.Concurrent;
using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Models.ScraperHealthAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

/// <summary>
/// Implementation of scraper health monitoring service.
/// </summary>
public class ScraperHealthService : IScraperHealthService
{
    private readonly AutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<ScraperHealthService> _logger;
    private readonly ConcurrentDictionary<string, HealthMetrics> _metricsCache = new();

    public ScraperHealthService(
        AutomatedMarketIntelligenceToolContext context,
        ILogger<ScraperHealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void RecordAttempt(string siteName, bool success, long responseTimeMs, int listingsFound = 0, string? error = null)
    {
        var metrics = _metricsCache.GetOrAdd(siteName, _ => new HealthMetrics { SiteName = siteName });

        lock (metrics)
        {
            metrics.TotalAttempts++;
            metrics.LastAttemptedAt = DateTime.UtcNow;

            if (success)
            {
                metrics.SuccessfulAttempts++;
                metrics.ListingsFound += listingsFound;
                metrics.LastSuccessAt = DateTime.UtcNow;
                
                // Warn if zero results detected
                if (listingsFound == 0)
                {
                    _logger.LogWarning(
                        "Zero results from {SiteName} - possible scraper breakage",
                        siteName);
                }
            }
            else
            {
                metrics.FailedAttempts++;
                metrics.LastError = error;
                _logger.LogWarning(
                    "Scrape attempt failed for {SiteName}: {Error}",
                    siteName, error);
            }

            metrics.ResponseTimes.Add(responseTimeMs);

            // Keep only last 100 response times to prevent unbounded growth
            if (metrics.ResponseTimes.Count > 100)
            {
                metrics.ResponseTimes.RemoveAt(0);
            }

            // Log health status changes
            var status = metrics.GetHealthStatus();
            if (status != ScraperHealthStatus.Healthy)
            {
                _logger.LogWarning(
                    "Scraper health for {SiteName} is {Status} - Success rate: {SuccessRate:F1}%, Missing elements: {MissingElements}",
                    siteName, status, metrics.SuccessRate, metrics.MissingElementCount);
            }
        }
    }

    public void RecordMissingElements(string siteName, IEnumerable<string> missingElements)
    {
        var elementsList = missingElements.ToList();
        if (!elementsList.Any())
            return;

        var metrics = _metricsCache.GetOrAdd(siteName, _ => new HealthMetrics { SiteName = siteName });

        lock (metrics)
        {
            foreach (var element in elementsList)
            {
                if (!metrics.MissingElements.Contains(element))
                {
                    metrics.MissingElements.Add(element);
                    metrics.MissingElementCount++;
                    _logger.LogWarning(
                        "Missing expected element on {SiteName}: {Element}",
                        siteName, element);
                }
            }
        }
    }

    public HealthMetrics GetHealthMetrics(string siteName)
    {
        return _metricsCache.GetOrAdd(siteName, _ => new HealthMetrics { SiteName = siteName });
    }

    public IDictionary<string, HealthMetrics> GetAllHealthMetrics()
    {
        return new Dictionary<string, HealthMetrics>(_metricsCache);
    }

    public async Task SaveHealthRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = new List<ScraperHealthRecord>();

        foreach (var (siteName, metrics) in _metricsCache)
        {
            var missingElementsJson = metrics.MissingElements.Any()
                ? JsonSerializer.Serialize(metrics.MissingElements)
                : null;

            var record = ScraperHealthRecord.Create(
                siteName,
                metrics.SuccessRate,
                metrics.ListingsFound,
                metrics.FailedAttempts,
                metrics.AverageResponseTime,
                metrics.LastError,
                metrics.GetHealthStatus().ToString(),
                metrics.MissingElementCount,
                missingElementsJson);

            records.Add(record);
        }

        if (records.Any())
        {
            await _context.Set<ScraperHealthRecord>().AddRangeAsync(records, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} health records to database", records.Count);
        }
    }

    public async Task<IEnumerable<ScraperHealthRecord>> GetHealthHistoryAsync(
        string siteName,
        int maxRecords = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ScraperHealthRecord>()
            .Where(r => r.SiteName == siteName)
            .OrderByDescending(r => r.RecordedAt)
            .Take(maxRecords)
            .ToListAsync(cancellationToken);
    }

    public void ClearMetrics(string? siteName = null)
    {
        if (siteName != null)
        {
            _metricsCache.TryRemove(siteName, out _);
            _logger.LogInformation("Cleared metrics for {SiteName}", siteName);
        }
        else
        {
            _metricsCache.Clear();
            _logger.LogInformation("Cleared all metrics");
        }
    }
}
