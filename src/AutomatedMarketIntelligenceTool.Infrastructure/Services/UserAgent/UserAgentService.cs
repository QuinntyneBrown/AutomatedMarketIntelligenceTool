using Serilog;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

/// <summary>
/// Service for managing User-Agent strings for web scraping.
/// Phase 5 - REQ-WS-008: User Agent Management with mobile support
/// </summary>
public class UserAgentService : IUserAgentService
{
    private readonly ILogger _logger;
    private readonly bool _rotationEnabled;
    private string? _customUserAgent;
    private readonly Dictionary<string, int> _currentIndex = new();
    private readonly Dictionary<string, int> _mobileIndex = new();
    private readonly object _lock = new();

    public UserAgentService(ILogger logger, bool rotationEnabled = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rotationEnabled = rotationEnabled;
    }

    public string GetUserAgent(string browserType)
    {
        if (!string.IsNullOrWhiteSpace(_customUserAgent))
        {
            _logger.Debug("Using custom user agent");
            return _customUserAgent;
        }

        return UserAgentPool.GetDefaultUserAgent(browserType);
    }

    public string GetNextUserAgent(string browserType)
    {
        if (!string.IsNullOrWhiteSpace(_customUserAgent))
        {
            _logger.Debug("Using custom user agent");
            return _customUserAgent;
        }

        if (!_rotationEnabled)
        {
            return UserAgentPool.GetDefaultUserAgent(browserType);
        }

        lock (_lock)
        {
            var agents = UserAgentPool.GetUserAgentsForBrowser(browserType);
            
            if (!_currentIndex.ContainsKey(browserType))
            {
                _currentIndex[browserType] = 0;
            }

            var index = _currentIndex[browserType];
            var userAgent = agents[index];
            
            _currentIndex[browserType] = (index + 1) % agents.Count;
            
            _logger.Debug(
                "Rotating user agent for {BrowserType}: Index {Index} of {Total}",
                browserType,
                index,
                agents.Count);

            return userAgent;
        }
    }

    public string GetMobileUserAgent(string browserType)
    {
        if (!string.IsNullOrWhiteSpace(_customUserAgent) && UserAgentPool.IsMobileUserAgent(_customUserAgent))
        {
            _logger.Debug("Using custom mobile user agent");
            return _customUserAgent;
        }

        return UserAgentPool.GetDefaultMobileUserAgent(browserType);
    }

    public string GetNextMobileUserAgent(string browserType)
    {
        if (!string.IsNullOrWhiteSpace(_customUserAgent) && UserAgentPool.IsMobileUserAgent(_customUserAgent))
        {
            _logger.Debug("Using custom mobile user agent");
            return _customUserAgent;
        }

        if (!_rotationEnabled)
        {
            return UserAgentPool.GetDefaultMobileUserAgent(browserType);
        }

        lock (_lock)
        {
            var agents = UserAgentPool.GetMobileUserAgentsForBrowser(browserType);
            
            var key = $"mobile_{browserType}";
            if (!_mobileIndex.ContainsKey(key))
            {
                _mobileIndex[key] = 0;
            }

            var index = _mobileIndex[key];
            var userAgent = agents[index];
            
            _mobileIndex[key] = (index + 1) % agents.Count;
            
            _logger.Debug(
                "Rotating mobile user agent for {BrowserType}: Index {Index} of {Total}",
                browserType,
                index,
                agents.Count);

            return userAgent;
        }
    }

    public bool IsMobile(string? userAgent = null)
    {
        var ua = userAgent ?? _customUserAgent;
        return !string.IsNullOrWhiteSpace(ua) && UserAgentPool.IsMobileUserAgent(ua);
    }

    public void SetCustomUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            throw new ArgumentException("User agent cannot be null or empty", nameof(userAgent));
        }

        _customUserAgent = userAgent;
        _logger.Information("Custom user agent set: {UserAgent}", userAgent);
    }

    public bool HasCustomUserAgent()
    {
        return !string.IsNullOrWhiteSpace(_customUserAgent);
    }
}
