namespace AutomatedMarketIntelligenceTool.Cli;

/// <summary>
/// Standard exit codes for the CLI application.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success - operation completed without errors.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// General error - unspecified error occurred.
    /// </summary>
    public const int GeneralError = 1;

    /// <summary>
    /// Validation error - invalid arguments or input.
    /// </summary>
    public const int ValidationError = 2;

    /// <summary>
    /// Network error - connection or network-related failure.
    /// </summary>
    public const int NetworkError = 3;

    /// <summary>
    /// Database error - database access or operation failure.
    /// </summary>
    public const int DatabaseError = 4;

    /// <summary>
    /// Scraping error - web scraping operation failure.
    /// </summary>
    public const int ScrapingError = 5;

    /// <summary>
    /// Not found - requested resource was not found.
    /// </summary>
    public const int NotFound = 6;
}
