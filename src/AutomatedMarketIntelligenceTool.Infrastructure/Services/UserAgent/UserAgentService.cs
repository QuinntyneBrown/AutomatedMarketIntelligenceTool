using Serilog;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

public class UserAgentService : IUserAgentService
{
    private readonly ILogger _logger;
    private readonly bool _rotationEnabled;
    private string? _customUserAgent;
    private readonly Dictionary<string, int> _currentIndex = new();
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
