using Image.Core.Services;
using Microsoft.Extensions.Logging;

namespace Image.Infrastructure.Storage;

/// <summary>
/// File system-based blob storage implementation for local development.
/// </summary>
public sealed class FileSystemBlobStorageService : IBlobStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<FileSystemBlobStorageService> _logger;

    public FileSystemBlobStorageService(
        string basePath,
        string baseUrl,
        ILogger<FileSystemBlobStorageService> logger)
    {
        _basePath = basePath;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(byte[] imageBytes, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var storagePath = GenerateStoragePath(fileName);
        var fullPath = Path.Combine(_basePath, storagePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);
        _logger.LogDebug("Uploaded image to {Path}", storagePath);

        return storagePath;
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Image not found at {Path}", storagePath);
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        _logger.LogDebug("Deleted image at {Path}", storagePath);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public string GetPublicUrl(string storagePath)
    {
        return $"{_baseUrl}/{storagePath.Replace('\\', '/')}";
    }

    private static string GenerateStoragePath(string fileName)
    {
        var date = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var sanitizedName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        return Path.Combine(
            date.Year.ToString(),
            date.Month.ToString("D2"),
            date.Day.ToString("D2"),
            $"{sanitizedName}-{guid}{extension}");
    }
}
