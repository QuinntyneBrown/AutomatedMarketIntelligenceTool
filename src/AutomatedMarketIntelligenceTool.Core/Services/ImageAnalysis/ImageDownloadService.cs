using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

/// <summary>
/// Implementation of the image download service.
/// </summary>
public class ImageDownloadService : IImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloadService> _logger;
    private static readonly HashSet<string> ValidImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;
    private const int TimeoutSeconds = 30;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10MB

    public ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }

    public async Task<ImageDownloadResult> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return ImageDownloadResult.Failed(imageUrl ?? string.Empty, "Image URL is null or empty");
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return ImageDownloadResult.Failed(imageUrl, "Invalid URL format");
        }

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Downloading image from {Url} (attempt {Attempt}/{MaxRetries})",
                    imageUrl, attempt, MaxRetries);

                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.Headers.Add("Accept", "image/*");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    if (attempt == MaxRetries)
                    {
                        _logger.LogWarning("Failed to download image from {Url}: {Error}", imageUrl, errorMsg);
                        return ImageDownloadResult.Failed(imageUrl, errorMsg);
                    }
                    await Task.Delay(RetryDelayMs * attempt, cancellationToken);
                    continue;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && !ValidImageContentTypes.Contains(contentType))
                {
                    _logger.LogWarning("Invalid content type {ContentType} for image from {Url}", contentType, imageUrl);
                    return ImageDownloadResult.Failed(imageUrl, $"Invalid content type: {contentType}");
                }

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength > MaxImageSizeBytes)
                {
                    _logger.LogWarning("Image too large ({Size} bytes) from {Url}", contentLength, imageUrl);
                    return ImageDownloadResult.Failed(imageUrl, $"Image too large: {contentLength} bytes");
                }

                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                if (data.Length == 0)
                {
                    return ImageDownloadResult.Failed(imageUrl, "Downloaded image is empty");
                }

                _logger.LogDebug("Successfully downloaded image from {Url} ({Size} bytes)", imageUrl, data.Length);
                return ImageDownloadResult.Succeeded(imageUrl, data, contentType);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (attempt == MaxRetries)
                {
                    _logger.LogWarning(ex, "Failed to download image from {Url} after {Attempts} attempts",
                        imageUrl, MaxRetries);
                    return ImageDownloadResult.Failed(imageUrl, ex.Message);
                }
                await Task.Delay(RetryDelayMs * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error downloading image from {Url}", imageUrl);
                return ImageDownloadResult.Failed(imageUrl, ex.Message);
            }
        }

        return ImageDownloadResult.Failed(imageUrl, "Failed after maximum retries");
    }

    public async Task<IReadOnlyList<ImageDownloadResult>> DownloadMultipleAsync(
        IEnumerable<string> imageUrls,
        int maxImages = 3,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImageDownloadResult>();
        var urls = imageUrls?.Take(maxImages).ToList() ?? new List<string>();

        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await DownloadAsync(url, cancellationToken);
            results.Add(result);
        }

        return results.AsReadOnly();
    }
}
