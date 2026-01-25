using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Search.Core.Interfaces;
using Search.Infrastructure.Data;
using Search.Infrastructure.Services;

namespace Search.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SearchDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ISearchProfileService, SearchProfileService>();
        return services;
    }
}
