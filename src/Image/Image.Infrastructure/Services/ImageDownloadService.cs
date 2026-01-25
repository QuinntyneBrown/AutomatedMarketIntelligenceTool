using Image.Core.Services;
using Microsoft.Extensions.Logging;

namespace Image.Infrastructure.Services;

/// <summary>
/// Service for downloading images from URLs.
/// </summary>
public sealed class ImageDownloadService : IImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloadService> _logger;
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    public ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            _logger.LogWarning("Empty image URL provided");
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", imageUrl, response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && !SupportedContentTypes.Contains(contentType))
            {
                _logger.LogWarning("Unsupported content type {ContentType} for {Url}", contentType, imageUrl);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (bytes.Length == 0)
            {
                _logger.LogWarning("Empty response from {Url}", imageUrl);
                return null;
            }

            _logger.LogDebug("Downloaded {Bytes} bytes from {Url}", bytes.Length, imageUrl);
            return bytes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error downloading image from {Url}", imageUrl);
            return null;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout downloading image from {Url}", imageUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading image from {Url}", imageUrl);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, byte[]?>> DownloadManyAsync(IEnumerable<string> imageUrls, CancellationToken cancellationToken = default)
    {
        var urls = imageUrls.ToList();
        var results = new Dictionary<string, byte[]?>(urls.Count);

        var tasks = urls.Select(async url =>
        {
            var bytes = await DownloadAsync(url, cancellationToken);
            return (url, bytes);
        });

        var completed = await Task.WhenAll(tasks);

        foreach (var (url, bytes) in completed)
        {
            results[url] = bytes;
        }

        _logger.LogInformation("Downloaded {SuccessCount}/{TotalCount} images",
            results.Count(r => r.Value != null), urls.Count);

        return results;
    }
}
