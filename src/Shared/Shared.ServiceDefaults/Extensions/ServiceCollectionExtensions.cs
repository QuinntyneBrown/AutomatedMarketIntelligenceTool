using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Messaging;
using Shared.Messaging.Correlation;
using Shared.Messaging.RabbitMQ;

namespace Shared.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring common service defaults.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds common service defaults including health checks, messaging, and correlation.
    /// </summary>
    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());

        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ messaging services.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, Action<RabbitMQOptions>? configure = null)
    {
        var options = new RabbitMQOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<IMessageBus>());
        services.AddSingleton<IEventSubscriber>(sp => sp.GetRequiredService<IMessageBus>());

        return services;
    }
}
