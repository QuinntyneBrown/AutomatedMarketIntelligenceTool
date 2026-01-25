using Dashboard.Core.Interfaces;
using Dashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

/// <summary>
/// Extension methods for configuring Dashboard service dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Dashboard service infrastructure dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDashboardInfrastructure(this IServiceCollection services)
    {
        // Register dashboard service
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
