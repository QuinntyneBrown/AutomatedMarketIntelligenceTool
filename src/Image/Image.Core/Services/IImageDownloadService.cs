namespace Image.Core.Services;

/// <summary>
/// Service for downloading images from URLs.
/// </summary>
public interface IImageDownloadService
{
    /// <summary>
    /// Downloads an image from the specified URL.
    /// </summary>
    Task<byte[]?> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads multiple images concurrently.
    /// </summary>
    Task<Dictionary<string, byte[]?>> DownloadManyAsync(IEnumerable<string> imageUrls, CancellationToken cancellationToken = default);
}
