namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

/// <summary>
/// Service for managing User-Agent strings for web scraping.
/// Phase 5 - REQ-WS-008: User Agent Management with mobile support
/// </summary>
public interface IUserAgentService
{
    string GetUserAgent(string browserType);
    string GetNextUserAgent(string browserType);
    void SetCustomUserAgent(string userAgent);
    bool HasCustomUserAgent();
    
    /// <summary>
    /// Gets a mobile user agent for the specified browser type.
    /// Phase 5 - AC-008.5: Mobile User-Agents available for mobile site scraping
    /// </summary>
    string GetMobileUserAgent(string browserType);
    
    /// <summary>
    /// Gets the next mobile user agent from the rotation pool.
    /// </summary>
    string GetNextMobileUserAgent(string browserType);
    
    /// <summary>
    /// Checks if the current or provided user agent is a mobile user agent.
    /// </summary>
    bool IsMobile(string? userAgent = null);
}
