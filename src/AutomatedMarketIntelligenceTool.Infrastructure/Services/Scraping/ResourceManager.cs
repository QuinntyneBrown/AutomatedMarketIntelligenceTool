using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Default implementation of resource manager for monitoring CPU and memory usage.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly Process _currentProcess;

    // Thresholds for resource management
    private const double HighCpuThreshold = 80.0;
    private const double ModerateCpuThreshold = 60.0;
    private const double HighMemoryThreshold = 80.0;
    private const double ModerateMemoryThreshold = 60.0;

    public ResourceManager(ILogger<ResourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentProcess = Process.GetCurrentProcess();
    }

    /// <inheritdoc/>
    public ResourceMetrics GetCurrentMetrics()
    {
        try
        {
            _currentProcess.Refresh();

            var memoryUsage = _currentProcess.WorkingSet64;
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

            // CPU usage requires sampling over time, so we approximate
            // For now, we'll use a simplified approach
            var cpuUsage = GetCpuUsage();

            var metrics = new ResourceMetrics
            {
                CpuUsagePercent = cpuUsage,
                MemoryUsageBytes = memoryUsage,
                TotalMemoryBytes = totalMemory
            };

            _logger.LogDebug(
                "Resource metrics - CPU: {CpuUsage:F2}%, Memory: {MemoryUsage:F2}%",
                metrics.CpuUsagePercent,
                metrics.MemoryUsagePercent);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve resource metrics");
            
            // Return default metrics on error
            return new ResourceMetrics
            {
                CpuUsagePercent = 0,
                MemoryUsageBytes = 0,
                TotalMemoryBytes = 1
            };
        }
    }

    /// <inheritdoc/>
    public bool HasAvailableResources(int currentConcurrency, int maxConcurrency)
    {
        if (currentConcurrency >= maxConcurrency)
        {
            return false;
        }

        var metrics = GetCurrentMetrics();
        
        if (metrics.IsUnderHighLoad)
        {
            _logger.LogWarning(
                "System under high load - CPU: {CpuUsage:F2}%, Memory: {MemoryUsage:F2}%",
                metrics.CpuUsagePercent,
                metrics.MemoryUsagePercent);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public int GetRecommendedConcurrency(int requestedConcurrency)
    {
        var metrics = GetCurrentMetrics();

        // If under high load, reduce concurrency
        if (metrics.IsUnderHighLoad)
        {
            var reduced = Math.Max(1, requestedConcurrency / 2);
            _logger.LogInformation(
                "Reducing concurrency from {Requested} to {Adjusted} due to high resource usage",
                requestedConcurrency,
                reduced);
            return reduced;
        }

        // If under moderate load, maintain current concurrency
        if (metrics.IsUnderModerateLoad)
        {
            _logger.LogDebug(
                "Maintaining concurrency at {Concurrency} due to moderate resource usage",
                requestedConcurrency);
            return requestedConcurrency;
        }

        // Otherwise, allow requested concurrency
        return requestedConcurrency;
    }

    private double GetCpuUsage()
    {
        try
        {
            // This is a simplified CPU usage calculation
            // For production, consider using PerformanceCounter or more sophisticated approaches
            var startTime = _currentProcess.TotalProcessorTime;
            var startWallTime = DateTime.UtcNow;

            // Sample for a brief moment
            Thread.Sleep(100);

            _currentProcess.Refresh();
            var endTime = _currentProcess.TotalProcessorTime;
            var endWallTime = DateTime.UtcNow;

            var cpuUsedMs = (endTime - startTime).TotalMilliseconds;
            var totalMsPassed = (endWallTime - startWallTime).TotalMilliseconds;
            var cpuUsagePercent = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;

            return Math.Min(100, Math.Max(0, cpuUsagePercent));
        }
        catch
        {
            return 0;
        }
    }
}
