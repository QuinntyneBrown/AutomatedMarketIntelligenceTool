using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Core.Interfaces;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Services;

namespace Notification.Infrastructure;

/// <summary>
/// Extension methods for configuring Notification service dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Notification service infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // HTTP Client for webhooks
        services.AddHttpClient("Webhooks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "NotificationService/1.0");
        });

        // Services
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
