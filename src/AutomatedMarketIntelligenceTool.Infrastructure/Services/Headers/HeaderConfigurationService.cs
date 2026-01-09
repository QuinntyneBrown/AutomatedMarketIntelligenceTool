using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Headers;

/// <summary>
/// Implementation of header configuration service for web scraping.
/// Phase 5 - REQ-WS-013: Request Header Configuration
/// AC-013.1: Standard headers (Accept, Accept-Language, etc.) set appropriately
/// AC-013.2: User can add custom headers
/// AC-013.3: Referer header set appropriately for each request
/// AC-013.4: DNT (Do Not Track) header configurable
/// </summary>
public class HeaderConfigurationService : IHeaderConfigurationService
{
    private readonly ILogger<HeaderConfigurationService> _logger;
    private readonly Dictionary<string, string> _customHeaders;
    private bool _doNotTrack;
    private string _acceptLanguage;
    private string? _referer;
    private readonly object _lock = new();

    public HeaderConfigurationService(ILogger<HeaderConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _acceptLanguage = "en-CA,en;q=0.9";
        _doNotTrack = false;
    }

    public IDictionary<string, string> GetDefaultHeaders()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9," +
                         "image/avif,image/webp,image/apng,*/*;q=0.8",
            ["Accept-Language"] = _acceptLanguage,
            ["Accept-Encoding"] = "gzip, deflate, br",
            ["Cache-Control"] = "no-cache",
            ["Pragma"] = "no-cache"
        };

        if (_doNotTrack)
        {
            headers["DNT"] = "1";
        }

        return headers;
    }

    public IDictionary<string, string> GetHeaders(IDictionary<string, string>? customHeaders = null)
    {
        lock (_lock)
        {
            var headers = new Dictionary<string, string>(GetDefaultHeaders(), StringComparer.OrdinalIgnoreCase);

            // Apply stored custom headers
            foreach (var kvp in _customHeaders)
            {
                headers[kvp.Key] = kvp.Value;
            }

            // Apply method-specific custom headers (highest priority)
            if (customHeaders != null)
            {
                foreach (var kvp in customHeaders)
                {
                    headers[kvp.Key] = kvp.Value;
                    _logger.LogDebug("Applied custom header: {Key} = {Value}", kvp.Key, kvp.Value);
                }
            }

            return headers;
        }
    }

    public void SetCustomHeaders(IDictionary<string, string> headers)
    {
        if (headers == null)
        {
            throw new ArgumentNullException(nameof(headers));
        }

        lock (_lock)
        {
            _customHeaders.Clear();
            foreach (var kvp in headers)
            {
                _customHeaders[kvp.Key] = kvp.Value;
                _logger.LogInformation("Set custom header: {Key} = {Value}", kvp.Key, kvp.Value);
            }
        }
    }

    public void ClearCustomHeaders()
    {
        lock (_lock)
        {
            var count = _customHeaders.Count;
            _customHeaders.Clear();
            _logger.LogInformation("Cleared {Count} custom headers", count);
        }
    }

    public void SetDoNotTrack(bool enabled)
    {
        lock (_lock)
        {
            _doNotTrack = enabled;
            _logger.LogInformation("Do Not Track header {Status}", enabled ? "enabled" : "disabled");
        }
    }

    public bool GetDoNotTrack()
    {
        lock (_lock)
        {
            return _doNotTrack;
        }
    }

    public void SetAcceptLanguage(string acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            throw new ArgumentException("Accept-Language cannot be null or empty", nameof(acceptLanguage));
        }

        lock (_lock)
        {
            _acceptLanguage = acceptLanguage;
            _logger.LogInformation("Accept-Language set to: {AcceptLanguage}", acceptLanguage);
        }
    }

    public string GetAcceptLanguage()
    {
        lock (_lock)
        {
            return _acceptLanguage;
        }
    }

    public void SetReferer(string? referer)
    {
        lock (_lock)
        {
            _referer = referer;
            if (!string.IsNullOrWhiteSpace(referer))
            {
                _logger.LogDebug("Referer set to: {Referer}", referer);
            }
        }
    }

    public IDictionary<string, string> GetHeadersWithReferer()
    {
        lock (_lock)
        {
            var headers = GetHeaders();

            if (!string.IsNullOrWhiteSpace(_referer))
            {
                headers["Referer"] = _referer;
            }

            return headers;
        }
    }
}
