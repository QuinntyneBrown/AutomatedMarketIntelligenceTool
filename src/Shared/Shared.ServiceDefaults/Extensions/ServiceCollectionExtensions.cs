using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Messaging;
using Shared.Messaging.Correlation;
using Shared.Messaging.RabbitMQ;

namespace Shared.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring common service defaults including
/// OpenTelemetry, health checks, service discovery, and resilience.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds common service defaults for .NET Aspire integration including
    /// OpenTelemetry, health checks, service discovery, and HTTP resilience.
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry tracing and metrics for the application.
    /// </summary>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Adds default health checks for the application.
    /// </summary>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps default health check endpoints for the application.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health check endpoint for overall health
        app.MapHealthChecks("/health");

        // Liveness probe endpoint - only checks if the app is running
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
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
