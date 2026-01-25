namespace ScrapingWorker.Core.Services;

/// <summary>
/// Service for managing user agents for web scraping.
/// </summary>
public interface IUserAgentService
{
    /// <summary>
    /// Gets a random user agent string.
    /// </summary>
    string GetRandomUserAgent();

    /// <summary>
    /// Gets a user agent for a specific browser type.
    /// </summary>
    string GetUserAgent(BrowserType browserType);
}

/// <summary>
/// Supported browser types for user agents.
/// </summary>
public enum BrowserType
{
    Chrome,
    Firefox,
    Safari,
    Edge
}
