using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WatchList.Core.Interfaces;
using WatchList.Infrastructure.Data;
using WatchList.Infrastructure.Services;

namespace WatchList.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWatchListInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WatchListDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IWatchListService, WatchListService>();
        return services;
    }
}
