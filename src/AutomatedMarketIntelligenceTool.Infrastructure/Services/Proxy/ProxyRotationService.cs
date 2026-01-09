using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;

/// <summary>
/// Default implementation of proxy rotation service.
/// </summary>
public class ProxyRotationService : IProxyRotationService
{
    private readonly ILogger<ProxyRotationService> _logger;
    private readonly List<ProxyInfo> _proxies = new();
    private readonly HashSet<string> _failedProxies = new();
    private readonly object _lock = new();
    private int _currentIndex = 0;

    public ProxyRotationService(ILogger<ProxyRotationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LoadProxiesFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Proxy file path cannot be empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Proxy file not found: {filePath}", filePath);
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var loadedProxies = 0;

            lock (_lock)
            {
                _proxies.Clear();
                _failedProxies.Clear();
                _currentIndex = 0;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                    {
                        continue;
                    }

                    var proxyInfo = ParseProxyLine(trimmedLine);
                    if (proxyInfo != null)
                    {
                        _proxies.Add(proxyInfo);
                        loadedProxies++;
                    }
                }
            }

            _logger.LogInformation(
                "Loaded {Count} proxies from file: {FilePath}",
                loadedProxies,
                filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load proxies from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Microsoft.Playwright.Proxy? GetNextProxy()
    {
        lock (_lock)
        {
            if (_proxies.Count == 0)
            {
                _logger.LogWarning("No proxies available");
                return null;
            }

            // Find next available proxy (not failed)
            var startIndex = _currentIndex;
            var checkedCount = 0;

            while (checkedCount < _proxies.Count)
            {
                var proxy = _proxies[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _proxies.Count;
                checkedCount++;

                if (!_failedProxies.Contains(proxy.Server))
                {
                    _logger.LogDebug("Selected proxy: {ProxyServer}", proxy.Server);
                    return proxy.ToPlaywrightProxy();
                }
            }

            _logger.LogWarning("All proxies have failed");
            return null;
        }
    }

    /// <inheritdoc/>
    public void MarkProxyAsFailed(string proxyServer)
    {
        if (string.IsNullOrWhiteSpace(proxyServer))
        {
            return;
        }

        lock (_lock)
        {
            _failedProxies.Add(proxyServer);
            _logger.LogWarning(
                "Marked proxy as failed: {ProxyServer}. Failed proxies: {FailedCount}/{TotalCount}",
                proxyServer,
                _failedProxies.Count,
                _proxies.Count);
        }
    }

    /// <inheritdoc/>
    public int GetAvailableProxyCount()
    {
        lock (_lock)
        {
            return _proxies.Count - _failedProxies.Count;
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled()
    {
        lock (_lock)
        {
            return _proxies.Count > 0 && GetAvailableProxyCount() > 0;
        }
    }

    private ProxyInfo? ParseProxyLine(string line)
    {
        try
        {
            // Expected formats:
            // http://host:port
            // https://host:port
            // socks5://host:port
            // host:port (defaults to http)
            // user:pass@host:port
            // http://user:pass@host:port

            string? username = null;
            string? password = null;
            string server;

            // Check if the line contains authentication
            var atIndex = line.LastIndexOf('@');
            if (atIndex > 0)
            {
                var authPart = line.Substring(0, atIndex);
                var serverPart = line.Substring(atIndex + 1);

                // Extract username and password
                var colonIndex = authPart.IndexOf(':');
                if (colonIndex > 0)
                {
                    // Check if this is part of a protocol (http://)
                    if (authPart.Substring(0, colonIndex).Contains("//"))
                    {
                        // Extract protocol and credentials
                        var protocolEnd = authPart.IndexOf("://");
                        var protocol = authPart.Substring(0, protocolEnd + 3);
                        var credentials = authPart.Substring(protocolEnd + 3);
                        
                        colonIndex = credentials.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            username = credentials.Substring(0, colonIndex);
                            password = credentials.Substring(colonIndex + 1);
                        }
                        
                        server = protocol + serverPart;
                    }
                    else
                    {
                        username = authPart.Substring(0, colonIndex);
                        password = authPart.Substring(colonIndex + 1);
                        
                        // Add http:// if no protocol specified
                        server = serverPart.Contains("://") ? serverPart : $"http://{serverPart}";
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid proxy format (missing password): {Line}", line);
                    return null;
                }
            }
            else
            {
                // No authentication
                server = line.Contains("://") ? line : $"http://{line}";
            }

            return new ProxyInfo
            {
                Server = server,
                Username = username,
                Password = password
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse proxy line: {Line}", line);
            return null;
        }
    }

    private class ProxyInfo
    {
        public required string Server { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }

        public Microsoft.Playwright.Proxy ToPlaywrightProxy()
        {
            var proxy = new Microsoft.Playwright.Proxy
            {
                Server = Server
            };

            if (!string.IsNullOrWhiteSpace(Username))
            {
                proxy.Username = Username;
                proxy.Password = Password;
            }

            return proxy;
        }
    }
}
