using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using AutomatedMarketIntelligenceTool.Core.Services.Scheduling;
using AutomatedMarketIntelligenceTool.Infrastructure.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Reporting;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.Extensions.DependencyInjection;

namespace AutomatedMarketIntelligenceTool.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IAutoCompleteService, AutoCompleteService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();
        services.AddScoped<IWatchListService, WatchListService>();

        // Duplicate detection
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IFuzzyMatchingService, FuzzyMatchingService>();
        services.AddScoped<IDeduplicationConfigService, DeduplicationConfigService>();
        services.AddScoped<IDeduplicationAuditService, DeduplicationAuditService>();
        services.AddScoped<IAccuracyMetricsService, AccuracyMetricsService>();
        services.AddScoped<IReviewService, ReviewService>();

        // Alert service
        services.AddScoped<IAlertService, AlertService>();

        // Reporting
        services.AddScoped<IReportGenerationService, ReportGenerationService>();
        services.AddScoped<IScheduledReportService, ScheduledReportService>();

        // Scraping
        services.AddSingleton<IScraperFactory, ScraperFactory>();
        services.AddSingleton<IScraperHealthService, ScraperHealthService>();
        services.AddSingleton<IRateLimiter, RateLimiter>();

        // Rate limiting configuration
        services.AddSingleton(new RateLimitConfiguration
        {
            DefaultDelayMs = 1000,
            MaxDelayMs = 30000,
            BackoffMultiplier = 2.0
        });

        return services;
    }
}
