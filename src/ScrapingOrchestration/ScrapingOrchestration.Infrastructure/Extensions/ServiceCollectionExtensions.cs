using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrapingOrchestration.Core.Services;
using ScrapingOrchestration.Infrastructure.Data;
using ScrapingOrchestration.Infrastructure.Services;

namespace ScrapingOrchestration.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring ScrapingOrchestration service dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ScrapingOrchestration service infrastructure dependencies with SQL Server.
    /// </summary>
    public static IServiceCollection AddScrapingOrchestrationInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<ScrapingOrchestrationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddSingleton<IJobScheduler, InMemoryJobScheduler>();
        services.AddScoped<ISearchSessionService, SearchSessionService>();

        return services;
    }

    /// <summary>
    /// Adds ScrapingOrchestration service infrastructure dependencies with in-memory database.
    /// Useful for testing and development scenarios.
    /// </summary>
    public static IServiceCollection AddScrapingOrchestrationInfrastructureInMemory(
        this IServiceCollection services,
        string databaseName = "ScrapingOrchestrationDb")
    {
        // In-memory database
        services.AddDbContext<ScrapingOrchestrationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Services
        services.AddSingleton<IJobScheduler, InMemoryJobScheduler>();
        services.AddScoped<ISearchSessionService, SearchSessionService>();

        return services;
    }
}
