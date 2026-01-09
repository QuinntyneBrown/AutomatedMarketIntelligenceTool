using AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;
using Microsoft.Playwright;
using Serilog;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Browser;

public class BrowserContextFactory : IBrowserContextFactory
{
    private readonly IPlaywright _playwright;
    private readonly IProxyService _proxyService;
    private readonly IUserAgentService _userAgentService;
    private readonly ILogger _logger;

    public BrowserContextFactory(
        IPlaywright playwright,
        IProxyService proxyService,
        IUserAgentService userAgentService,
        ILogger logger)
    {
        _playwright = playwright ?? throw new ArgumentNullException(nameof(playwright));
        _proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
        _userAgentService = userAgentService ?? throw new ArgumentNullException(nameof(userAgentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IBrowserContext> CreateContextAsync(
        string browserType,
        bool headless = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(browserType))
        {
            browserType = "chromium";
        }

        browserType = browserType.ToLowerInvariant();

        _logger.Information(
            "Creating browser context: Type={BrowserType}, Headless={Headless}",
            browserType,
            headless);

        var browser = await LaunchBrowserAsync(browserType, headless, cancellationToken);
        var userAgent = _userAgentService.GetNextUserAgent(browserType);

        var contextOptions = new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
            TimezoneId = "America/New_York"
        };

        // Apply proxy if enabled
        var proxy = _proxyService.GetPlaywrightProxy();
        if (proxy != null)
        {
            contextOptions.Proxy = proxy;
            _logger.Information("Proxy configured for browser context");
        }

        var context = await browser.NewContextAsync(contextOptions);
        
        _logger.Information(
            "Browser context created successfully with UserAgent: {UserAgent}",
            userAgent);

        return context;
    }

    private async Task<IBrowser> LaunchBrowserAsync(
        string browserType,
        bool headless,
        CancellationToken cancellationToken)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[]
            {
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--no-sandbox"
            }
        };

        IBrowser browser = browserType switch
        {
            "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
            "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
            _ => await _playwright.Chromium.LaunchAsync(launchOptions)
        };

        _logger.Debug("Browser launched: {BrowserType}", browserType);
        
        return browser;
    }
}
