using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Configuration.Core.Interfaces;
using Configuration.Infrastructure.Data;
using Configuration.Infrastructure.Services;

namespace Configuration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ConfigurationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IConfigurationService, ConfigurationService>();
        return services;
    }
}
