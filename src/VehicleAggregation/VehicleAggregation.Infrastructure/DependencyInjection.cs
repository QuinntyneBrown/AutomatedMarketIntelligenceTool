using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleAggregation.Core.Interfaces;
using VehicleAggregation.Infrastructure.Data;
using VehicleAggregation.Infrastructure.Services;

namespace VehicleAggregation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVehicleAggregationInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VehicleDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IVehicleService, VehicleService>();
        return services;
    }
}
