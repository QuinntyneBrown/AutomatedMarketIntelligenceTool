namespace AutomatedMarketIntelligenceTool.Cli.Configuration;

/// <summary>
/// Application settings that can be configured via config file or environment variables.
/// </summary>
public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public ScrapingSettings Scraping { get; set; } = new();
    public SearchSettings Search { get; set; } = new();
    public DeactivationSettings Deactivation { get; set; } = new();
    public OutputSettings Output { get; set; } = new();
    public VerbositySettings Verbosity { get; set; } = new();
    public InteractiveSettings Interactive { get; set; } = new();
}

public class DatabaseSettings
{
    public string Provider { get; set; } = "SQLite";
    public string? ConnectionString { get; set; }
    public string SQLitePath { get; set; } = "car-search.db";
}

public class ScrapingSettings
{
    public int DefaultDelayMs { get; set; } = 3000;
    public int MaxRetries { get; set; } = 3;
    public int MaxPages { get; set; } = 50;
    public string[] DefaultSites { get; set; } = new[] { "autotrader", "cars.com", "cargurus" };
    public bool HeadedMode { get; set; } = false;
}

public class SearchSettings
{
    public double DefaultRadius { get; set; } = 40;
    public int DefaultPageSize { get; set; } = 25;
}

public class DeactivationSettings
{
    public int StaleDays { get; set; } = 7;
}

public class OutputSettings
{
    public string DefaultFormat { get; set; } = "table";
    public bool ColorEnabled { get; set; } = true;
}

public class VerbositySettings
{
    /// <summary>
    /// Default verbosity level (0 = normal, 1 = verbose, 2 = debug, 3 = trace).
    /// </summary>
    public int DefaultLevel { get; set; } = 0;

    /// <summary>
    /// Enable file logging at configured verbosity level.
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Log file directory path.
    /// </summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>
    /// Number of days to retain log files.
    /// </summary>
    public int RetainDays { get; set; } = 7;
}

public class InteractiveSettings
{
    /// <summary>
    /// Remember previous search values in interactive mode.
    /// </summary>
    public bool RememberPreviousValues { get; set; } = true;

    /// <summary>
    /// Default tenant ID to use in interactive mode.
    /// </summary>
    public Guid? DefaultTenantId { get; set; }

    /// <summary>
    /// Show command preview before execution in interactive mode.
    /// </summary>
    public bool ShowCommandPreview { get; set; } = true;
}
