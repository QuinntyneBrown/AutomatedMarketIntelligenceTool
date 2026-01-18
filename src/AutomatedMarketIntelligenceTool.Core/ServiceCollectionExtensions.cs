using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using AutomatedMarketIntelligenceTool.Core.Services.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace AutomatedMarketIntelligenceTool.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // These are typically implemented in Infrastructure, but we register interfaces here
        // for DI container awareness. Actual implementations come from AddInfrastructureServices.
        return services;
    }
}
