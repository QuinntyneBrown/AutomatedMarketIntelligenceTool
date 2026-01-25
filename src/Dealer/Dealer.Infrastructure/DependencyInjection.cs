using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dealer.Core.Interfaces;
using Dealer.Infrastructure.Data;
using Dealer.Infrastructure.Services;

namespace Dealer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDealerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DealerDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDealerService, DealerService>();
        return services;
    }
}
