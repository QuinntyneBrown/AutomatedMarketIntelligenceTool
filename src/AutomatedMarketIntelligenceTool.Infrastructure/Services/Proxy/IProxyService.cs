namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;

public interface IProxyService
{
    ProxyConfiguration GetConfiguration();
    Microsoft.Playwright.Proxy? GetPlaywrightProxy();
    bool IsEnabled();
}
