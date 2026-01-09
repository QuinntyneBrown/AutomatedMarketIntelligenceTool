namespace AutomatedMarketIntelligenceTool.Core.Models.ResourceThrottleAggregate;

/// <summary>
/// Defines the types of resources that can be throttled.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// API requests per time window.
    /// </summary>
    ApiRequests = 0,

    /// <summary>
    /// Concurrent scraping operations.
    /// </summary>
    ConcurrentScrapers = 1,

    /// <summary>
    /// Report generations per day.
    /// </summary>
    ReportGenerations = 2,

    /// <summary>
    /// Database queries per minute.
    /// </summary>
    DatabaseQueries = 3,

    /// <summary>
    /// Memory usage percentage.
    /// </summary>
    MemoryUsage = 4,

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    CpuUsage = 5,

    /// <summary>
    /// Storage usage in MB.
    /// </summary>
    StorageUsage = 6,

    /// <summary>
    /// Concurrent user sessions.
    /// </summary>
    ConcurrentSessions = 7,

    /// <summary>
    /// Export operations per day.
    /// </summary>
    ExportOperations = 8,

    /// <summary>
    /// Email notifications per day.
    /// </summary>
    EmailNotifications = 9
}
