namespace Image.Core.Services;

/// <summary>
/// Service for calculating perceptual hashes of images.
/// </summary>
public interface IImageHashingService
{
    /// <summary>
    /// Calculates the perceptual hash of an image from a URL.
    /// </summary>
    Task<string?> CalculateHashAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the perceptual hash of an image from bytes.
    /// </summary>
    Task<string?> CalculateHashFromBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two image hashes and returns a similarity score (0-1).
    /// </summary>
    double CompareHashes(string hash1, string hash2);

    /// <summary>
    /// Determines if two hashes represent similar images.
    /// </summary>
    bool AreSimilar(string hash1, string hash2, double threshold = 0.9);
}
