using Serilog;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;

public class ProxyService : IProxyService
{
    private readonly ProxyConfiguration _configuration;
    private readonly ILogger _logger;

    public ProxyService(ProxyConfiguration configuration, ILogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ProxyConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public Microsoft.Playwright.Proxy? GetPlaywrightProxy()
    {
        if (!_configuration.Enabled || string.IsNullOrWhiteSpace(_configuration.Address))
        {
            return null;
        }

        var proxy = new Microsoft.Playwright.Proxy
        {
            Server = _configuration.Address
        };

        if (!string.IsNullOrWhiteSpace(_configuration.Username))
        {
            proxy.Username = _configuration.Username;
            proxy.Password = _configuration.Password;
            
            _logger.Information(
                "Proxy configured with authentication: {ProxyServer}, Username: {Username}",
                _configuration.Address,
                _configuration.Username);
        }
        else
        {
            _logger.Information(
                "Proxy configured without authentication: {ProxyServer}",
                _configuration.Address);
        }

        return proxy;
    }

    public bool IsEnabled()
    {
        return _configuration.Enabled && !string.IsNullOrWhiteSpace(_configuration.Address);
    }
}
