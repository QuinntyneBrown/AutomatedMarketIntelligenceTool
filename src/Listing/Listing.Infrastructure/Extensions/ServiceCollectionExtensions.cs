using Listing.Core.Services;
using Listing.Infrastructure.Data;
using Listing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Listing.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Listing service dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Listing service infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddListingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<ListingDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddScoped<IListingService, ListingService>();

        return services;
    }
}
