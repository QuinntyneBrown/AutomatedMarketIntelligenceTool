using Image.Core.Services;
using Image.Infrastructure.Data;
using Image.Infrastructure.Hashing;
using Image.Infrastructure.Services;
using Image.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Image.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Image service dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Image service infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddImageInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string blobStoragePath,
        string blobStorageBaseUrl)
    {
        // Database
        services.AddDbContext<ImageDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Hashing
        services.AddSingleton<PerceptualHashCalculator>();

        // HTTP Client for downloading images
        services.AddHttpClient<IImageDownloadService, ImageDownloadService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ImageService/1.0");
        });

        // Services
        services.AddScoped<IImageHashingService, ImageHashingService>();

        // Blob Storage
        services.AddSingleton<IBlobStorageService>(sp =>
            new FileSystemBlobStorageService(
                blobStoragePath,
                blobStorageBaseUrl,
                sp.GetRequiredService<ILogger<FileSystemBlobStorageService>>()));

        return services;
    }
}
