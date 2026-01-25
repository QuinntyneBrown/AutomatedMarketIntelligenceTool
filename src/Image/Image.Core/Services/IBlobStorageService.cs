namespace Image.Core.Services;

/// <summary>
/// Service for storing and retrieving image blobs.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads an image to blob storage.
    /// </summary>
    /// <param name="imageBytes">The image data.</param>
    /// <param name="fileName">The file name to use.</param>
    /// <param name="contentType">The content type of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage path of the uploaded image.</returns>
    Task<string> UploadAsync(byte[] imageBytes, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an image from blob storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The image bytes, or null if not found.</returns>
    Task<byte[]?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from blob storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an image exists in blob storage.
    /// </summary>
    /// <param name="storagePath">The storage path of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public URL for the image.
    /// </summary>
    /// <param name="storagePath">The storage path of the image.</param>
    /// <returns>A URL to access the image.</returns>
    string GetPublicUrl(string storagePath);
}
