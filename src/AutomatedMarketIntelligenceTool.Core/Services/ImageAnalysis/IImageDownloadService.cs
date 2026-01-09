namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

/// <summary>
/// Service for downloading images from URLs.
/// </summary>
public interface IImageDownloadService
{
    /// <summary>
    /// Downloads an image from the specified URL.
    /// </summary>
    /// <param name="imageUrl">The URL of the image to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw image data as a byte array.</returns>
    Task<ImageDownloadResult> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads multiple images from the specified URLs.
    /// </summary>
    /// <param name="imageUrls">The URLs of the images to download.</param>
    /// <param name="maxImages">Maximum number of images to download (default 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of download results.</returns>
    Task<IReadOnlyList<ImageDownloadResult>> DownloadMultipleAsync(
        IEnumerable<string> imageUrls,
        int maxImages = 3,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an image download operation.
/// </summary>
public class ImageDownloadResult
{
    /// <summary>
    /// Whether the download was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The URL that was downloaded.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// The raw image data (null if download failed).
    /// </summary>
    public byte[]? Data { get; init; }

    /// <summary>
    /// Error message if download failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The content type of the downloaded image.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Creates a successful download result.
    /// </summary>
    public static ImageDownloadResult Succeeded(string url, byte[] data, string? contentType = null)
    {
        return new ImageDownloadResult
        {
            Success = true,
            Url = url,
            Data = data,
            ContentType = contentType
        };
    }

    /// <summary>
    /// Creates a failed download result.
    /// </summary>
    public static ImageDownloadResult Failed(string url, string errorMessage)
    {
        return new ImageDownloadResult
        {
            Success = false,
            Url = url,
            ErrorMessage = errorMessage
        };
    }
}
