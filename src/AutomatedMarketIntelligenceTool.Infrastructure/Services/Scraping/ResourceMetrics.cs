namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Scraping;

/// <summary>
/// Resource utilization metrics.
/// </summary>
public class ResourceMetrics
{
    /// <summary>
    /// Gets the CPU usage percentage (0-100).
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>
    /// Gets the total available memory in bytes.
    /// </summary>
    public long TotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the memory usage percentage (0-100).
    /// </summary>
    public double MemoryUsagePercent => TotalMemoryBytes > 0
        ? (MemoryUsageBytes / (double)TotalMemoryBytes) * 100
        : 0;

    /// <summary>
    /// Gets a value indicating whether resources are under high load.
    /// </summary>
    public bool IsUnderHighLoad => CpuUsagePercent > 80 || MemoryUsagePercent > 80;

    /// <summary>
    /// Gets a value indicating whether resources are under moderate load.
    /// </summary>
    public bool IsUnderModerateLoad => CpuUsagePercent > 60 || MemoryUsagePercent > 60;
}
