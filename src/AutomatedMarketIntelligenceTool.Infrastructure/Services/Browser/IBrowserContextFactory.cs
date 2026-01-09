using Microsoft.Playwright;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Browser;

public interface IBrowserContextFactory
{
    Task<IBrowserContext> CreateContextAsync(
        string browserType,
        bool headless = true,
        CancellationToken cancellationToken = default);
}
