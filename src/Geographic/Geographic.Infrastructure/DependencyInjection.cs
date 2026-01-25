using Geographic.Core.Interfaces;
using Geographic.Infrastructure.Data;
using Geographic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Geographic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGeographicInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GeographicDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICustomMarketService, CustomMarketService>();
        services.AddSingleton<IGeoDistanceCalculator, GeoDistanceCalculator>();

        return services;
    }
}
