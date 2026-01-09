using AutomatedMarketIntelligenceTool.Core.Models.CacheAggregate;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Caching;

/// <summary>
/// Background service that periodically cleans up expired cache entries.
/// </summary>
public class CacheCleanupService : BackgroundService
{
    private readonly ResponseCacheService _cacheService;
    private readonly CacheConfiguration _config;
    private readonly ILogger<CacheCleanupService> _logger;

    public CacheCleanupService(
        ResponseCacheService cacheService,
        IOptions<CacheConfiguration> config,
        ILogger<CacheCleanupService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _config = config?.Value ?? new CacheConfiguration();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Cache cleanup service started. Cleanup interval: {Interval} minutes",
            _config.CleanupIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_config.CleanupIntervalMinutes),
                    stoppingToken);

                var cleanedCount = _cacheService.CleanupExpiredEntries();

                if (cleanedCount > 0)
                {
                    var stats = await _cacheService.GetStatisticsAsync(stoppingToken);
                    _logger.LogInformation(
                        "Cache cleanup completed. Removed: {Removed}, Remaining: {Remaining}, Size: {Size}MB",
                        cleanedCount,
                        stats.TotalEntries,
                        stats.TotalSizeBytes / (1024 * 1024));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        _logger.LogInformation("Cache cleanup service stopped");
    }
}
