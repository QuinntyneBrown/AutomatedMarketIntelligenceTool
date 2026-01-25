using Alert.Core.Interfaces;
using Alert.Infrastructure.Data;
using Alert.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAlertInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AlertDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAlertService, AlertService>();
        return services;
    }
}
