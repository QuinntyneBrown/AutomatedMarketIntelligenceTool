namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Headers;

/// <summary>
/// Service for managing HTTP request headers for web scraping operations.
/// Phase 5 - REQ-WS-013: Request Header Configuration
/// </summary>
public interface IHeaderConfigurationService
{
    /// <summary>
    /// Gets the default headers for web requests.
    /// </summary>
    IDictionary<string, string> GetDefaultHeaders();

    /// <summary>
    /// Gets headers with custom values merged with defaults.
    /// </summary>
    /// <param name="customHeaders">Custom headers to add or override.</param>
    IDictionary<string, string> GetHeaders(IDictionary<string, string>? customHeaders = null);

    /// <summary>
    /// Sets custom headers that will be applied to all requests.
    /// </summary>
    void SetCustomHeaders(IDictionary<string, string> headers);

    /// <summary>
    /// Clears all custom headers.
    /// </summary>
    void ClearCustomHeaders();

    /// <summary>
    /// Enables or disables the Do Not Track (DNT) header.
    /// </summary>
    void SetDoNotTrack(bool enabled);

    /// <summary>
    /// Gets the current Do Not Track setting.
    /// </summary>
    bool GetDoNotTrack();

    /// <summary>
    /// Sets the Accept-Language header value.
    /// </summary>
    void SetAcceptLanguage(string acceptLanguage);

    /// <summary>
    /// Gets the current Accept-Language value.
    /// </summary>
    string GetAcceptLanguage();

    /// <summary>
    /// Sets the Referer header for the next request.
    /// </summary>
    void SetReferer(string? referer);

    /// <summary>
    /// Gets headers including Referer if set.
    /// </summary>
    IDictionary<string, string> GetHeadersWithReferer();
}
