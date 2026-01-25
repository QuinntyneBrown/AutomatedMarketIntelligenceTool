using Microsoft.Extensions.DependencyInjection;
using ScrapingWorker.Core.Services;
using ScrapingWorker.Infrastructure.Scrapers;
using ScrapingWorker.Infrastructure.Services;

namespace ScrapingWorker.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring ScrapingWorker dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ScrapingWorker infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddScrapingWorkerInfrastructure(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IRateLimiter>(_ => new RateLimiter(requestsPerMinute: 30));
        services.AddSingleton<IUserAgentService, UserAgentService>();
        services.AddSingleton<IScraperFactory, ScraperFactory>();

        // HTTP client for scrapers
        services.AddHttpClient("Scraper", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register scrapers
        services.AddSingleton<ISiteScraper, AutotraderScraper>();
        // Add more scrapers here as they are implemented:
        // services.AddSingleton<ISiteScraper, KijijiScraper>();
        // services.AddSingleton<ISiteScraper, CarGurusScraper>();
        // etc.

        return services;
    }
}
