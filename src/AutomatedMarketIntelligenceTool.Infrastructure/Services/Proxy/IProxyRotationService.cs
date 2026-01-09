using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;

/// <summary>
/// Service for rotating through multiple proxies.
/// </summary>
public interface IProxyRotationService
{
    /// <summary>
    /// Loads proxies from a file.
    /// </summary>
    /// <param name="filePath">Path to the proxy file (one proxy per line).</param>
    /// <returns>Task representing the async operation.</returns>
    Task LoadProxiesFromFileAsync(string filePath);

    /// <summary>
    /// Gets the next available proxy.
    /// </summary>
    /// <returns>A Playwright proxy configuration, or null if no proxies available.</returns>
    Microsoft.Playwright.Proxy? GetNextProxy();

    /// <summary>
    /// Marks a proxy as failed and removes it from rotation.
    /// </summary>
    /// <param name="proxyServer">The proxy server address that failed.</param>
    void MarkProxyAsFailed(string proxyServer);

    /// <summary>
    /// Gets the count of available proxies.
    /// </summary>
    /// <returns>Number of available proxies.</returns>
    int GetAvailableProxyCount();

    /// <summary>
    /// Gets whether proxy rotation is enabled.
    /// </summary>
    /// <returns>True if proxies are configured and available.</returns>
    bool IsEnabled();
}
