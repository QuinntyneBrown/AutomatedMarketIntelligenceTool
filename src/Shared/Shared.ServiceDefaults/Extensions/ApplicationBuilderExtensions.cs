using Microsoft.AspNetCore.Builder;
using Shared.Messaging.Correlation;
using Shared.ServiceDefaults.Middleware;

namespace Shared.ServiceDefaults.Extensions;

/// <summary>
/// Extension methods for configuring common middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds common middleware including error handling, correlation ID, and request logging.
    /// </summary>
    public static IApplicationBuilder UseServiceDefaults(this IApplicationBuilder app)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        return app;
    }
}
