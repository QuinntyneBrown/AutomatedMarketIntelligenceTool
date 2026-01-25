using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Core.Interfaces;
using Reporting.Infrastructure.Data;
using Reporting.Infrastructure.Services;

namespace Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Services
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IScheduledReportService, ScheduledReportService>();

        return services;
    }
}
