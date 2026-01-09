namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

/// <summary>
/// Pool of user agents for web scraping.
/// Phase 5 - REQ-WS-008: User Agent Management
/// AC-008.5: Mobile User-Agents available for mobile site scraping
/// </summary>
public class UserAgentPool
{
    private static readonly string[] ChromiumUserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
    };

    private static readonly string[] FirefoxUserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0"
    };

    private static readonly string[] WebKitUserAgents = new[]
    {
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Safari/605.1.15",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7_2) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Safari/605.1.15",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1"
    };

    // Phase 5 - AC-008.5: Mobile User-Agents for mobile site scraping
    private static readonly string[] MobileChromiumUserAgents = new[]
    {
        "Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 14; SM-A546B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/131.0.6778.73 Mobile/15E148 Safari/604.1"
    };

    private static readonly string[] MobileFirefoxUserAgents = new[]
    {
        "Mozilla/5.0 (Android 14; Mobile; rv:133.0) Gecko/133.0 Firefox/133.0",
        "Mozilla/5.0 (Android 13; Mobile; rv:133.0) Gecko/133.0 Firefox/133.0",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/133.0 Mobile/15E148 Safari/605.1.15"
    };

    private static readonly string[] MobileWebKitUserAgents = new[]
    {
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Mobile/15E148 Safari/604.1"
    };

    public static IReadOnlyList<string> GetUserAgentsForBrowser(string browserType)
    {
        return browserType?.ToLowerInvariant() switch
        {
            "firefox" => FirefoxUserAgents,
            "webkit" => WebKitUserAgents,
            _ => ChromiumUserAgents
        };
    }

    public static IReadOnlyList<string> GetMobileUserAgentsForBrowser(string browserType)
    {
        return browserType?.ToLowerInvariant() switch
        {
            "firefox" => MobileFirefoxUserAgents,
            "webkit" => MobileWebKitUserAgents,
            _ => MobileChromiumUserAgents
        };
    }

    public static string GetDefaultUserAgent(string browserType)
    {
        var agents = GetUserAgentsForBrowser(browserType);
        return agents[0];
    }

    public static string GetDefaultMobileUserAgent(string browserType)
    {
        var agents = GetMobileUserAgentsForBrowser(browserType);
        return agents[0];
    }

    public static bool IsMobileUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        var lowerUserAgent = userAgent.ToLowerInvariant();
        return lowerUserAgent.Contains("mobile") || 
               lowerUserAgent.Contains("android") || 
               (lowerUserAgent.Contains("iphone") || lowerUserAgent.Contains("ipad"));
    }
}
