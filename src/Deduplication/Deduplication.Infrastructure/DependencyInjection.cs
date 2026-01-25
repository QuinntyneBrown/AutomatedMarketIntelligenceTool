using Deduplication.Core.Interfaces;
using Deduplication.Core.Interfaces.Calculators;
using Deduplication.Infrastructure.Calculators;
using Deduplication.Infrastructure.Data;
using Deduplication.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Deduplication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeduplicationInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<DeduplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Calculators
        services.AddSingleton<IStringDistanceCalculator, StringDistanceCalculator>();
        services.AddSingleton<INumericProximityCalculator, NumericProximityCalculator>();
        services.AddSingleton<ILocationProximityCalculator, LocationProximityCalculator>();
        services.AddSingleton<IImageHashComparer, ImageHashComparer>();

        // Services
        services.AddScoped<IFuzzyMatchingService, FuzzyMatchingService>();
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IDeduplicationConfigService, DeduplicationConfigService>();
        services.AddScoped<IDuplicateMatchRepository, DuplicateMatchRepository>();
        services.AddScoped<IAccuracyMetricsService, AccuracyMetricsService>();

        return services;
    }
}
