using Image.Core.Services;
using Image.Core.ValueObjects;
using Image.Infrastructure.Hashing;
using Microsoft.Extensions.Logging;

namespace Image.Infrastructure.Services;

/// <summary>
/// Service for calculating and comparing perceptual hashes of images.
/// </summary>
public sealed class ImageHashingService : IImageHashingService
{
    private readonly IImageDownloadService _downloadService;
    private readonly PerceptualHashCalculator _hashCalculator;
    private readonly ILogger<ImageHashingService> _logger;

    public ImageHashingService(
        IImageDownloadService downloadService,
        PerceptualHashCalculator hashCalculator,
        ILogger<ImageHashingService> logger)
    {
        _downloadService = downloadService;
        _hashCalculator = hashCalculator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> CalculateHashAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            _logger.LogWarning("Empty image URL provided for hashing");
            return null;
        }

        try
        {
            var imageBytes = await _downloadService.DownloadAsync(imageUrl, cancellationToken);
            if (imageBytes == null)
            {
                _logger.LogWarning("Failed to download image for hashing: {Url}", imageUrl);
                return null;
            }

            return await CalculateHashFromBytesAsync(imageBytes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating hash for image: {Url}", imageUrl);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<string?> CalculateHashFromBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            _logger.LogWarning("Empty image bytes provided for hashing");
            return Task.FromResult<string?>(null);
        }

        try
        {
            var hash = _hashCalculator.CalculateHash(imageBytes);
            _logger.LogDebug("Calculated hash {Hash} for image ({Bytes} bytes)", hash, imageBytes.Length);
            return Task.FromResult<string?>(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating hash from image bytes");
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public double CompareHashes(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return 0;

        var imageHash1 = new ImageHash(hash1);
        var imageHash2 = new ImageHash(hash2);

        if (!imageHash1.IsValid || !imageHash2.IsValid)
        {
            return _hashCalculator.CalculateSimilarity(hash1, hash2);
        }

        return imageHash1.CompareTo(imageHash2);
    }

    /// <inheritdoc />
    public bool AreSimilar(string hash1, string hash2, double threshold = 0.9)
    {
        var similarity = CompareHashes(hash1, hash2);
        return similarity >= threshold;
    }
}
