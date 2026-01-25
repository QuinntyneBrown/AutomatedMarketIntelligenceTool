using ScrapingWorker.Core.Services;

namespace ScrapingWorker.Infrastructure.Services;

/// <summary>
/// Service for generating random user agent strings.
/// </summary>
public sealed class UserAgentService : IUserAgentService
{
    private static readonly string[] ChromeUserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    ];

    private static readonly string[] FirefoxUserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0"
    ];

    private static readonly string[] SafariUserAgents =
    [
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Safari/605.1.15"
    ];

    private static readonly string[] EdgeUserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0"
    ];

    private static readonly string[] AllUserAgents = [.. ChromeUserAgents, .. FirefoxUserAgents, .. SafariUserAgents, .. EdgeUserAgents];

    private static readonly Random Random = new();

    /// <inheritdoc />
    public string GetRandomUserAgent()
    {
        return AllUserAgents[Random.Next(AllUserAgents.Length)];
    }

    /// <inheritdoc />
    public string GetUserAgent(BrowserType browserType)
    {
        var agents = browserType switch
        {
            BrowserType.Chrome => ChromeUserAgents,
            BrowserType.Firefox => FirefoxUserAgents,
            BrowserType.Safari => SafariUserAgents,
            BrowserType.Edge => EdgeUserAgents,
            _ => AllUserAgents
        };

        return agents[Random.Next(agents.Length)];
    }
}
