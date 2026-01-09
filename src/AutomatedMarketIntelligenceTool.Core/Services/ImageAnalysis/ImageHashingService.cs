using System.Text.Json;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

/// <summary>
/// Implementation of the image hashing service for duplicate detection.
/// </summary>
public class ImageHashingService : IImageHashingService
{
    private readonly IImageDownloadService _downloadService;
    private readonly PerceptualHashCalculator _hashCalculator;
    private readonly ILogger<ImageHashingService> _logger;

    private const int DefaultMaxImages = 3;
    private const int SimilarityThreshold = 10;  // Hamming distance threshold
    private const double MajorityThresholdRatio = 0.5;  // At least half must match

    public ImageHashingService(
        IImageDownloadService downloadService,
        PerceptualHashCalculator hashCalculator,
        ILogger<ImageHashingService> logger)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _hashCalculator = hashCalculator ?? throw new ArgumentNullException(nameof(hashCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageHashResult> ComputeHashesAsync(
        IEnumerable<string> imageUrls,
        int maxImages = DefaultMaxImages,
        CancellationToken cancellationToken = default)
    {
        var urls = imageUrls?.Take(maxImages).ToList() ?? new List<string>();
        if (urls.Count == 0)
        {
            _logger.LogDebug("No image URLs provided for hashing");
            return ImageHashResult.Empty();
        }

        var hashes = new List<ulong>();
        int failed = 0;

        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var downloadResult = await _downloadService.DownloadAsync(url, cancellationToken);
                if (downloadResult.Success && downloadResult.Data != null)
                {
                    var hash = _hashCalculator.CalculateHash(downloadResult.Data);
                    hashes.Add(hash);
                    _logger.LogDebug("Computed hash {Hash:X16} for image {Url}", hash, url);
                }
                else
                {
                    failed++;
                    _logger.LogWarning("Failed to download image {Url}: {Error}",
                        url, downloadResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to compute hash for image {Url}", url);
            }
        }

        _logger.LogInformation("Computed {Count} hashes from {Total} images ({Failed} failed)",
            hashes.Count, urls.Count, failed);

        return new ImageHashResult
        {
            Hashes = hashes.AsReadOnly(),
            SuccessfulCount = hashes.Count,
            FailedCount = failed
        };
    }

    public async Task<ImageMatchResult> FindImageMatchesAsync(
        IEnumerable<string> imageUrls,
        IEnumerable<Listing> candidates,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Compute hashes for input images
        var hashResult = await ComputeHashesAsync(imageUrls, DefaultMaxImages, cancellationToken);
        if (!hashResult.HasHashes)
        {
            _logger.LogDebug("No hashes computed for input images, returning NoImages result");
            return ImageMatchResult.NoImages();
        }

        var inputHashes = hashResult.Hashes.ToList();
        _logger.LogDebug("Computed {Count} hashes for input images", inputHashes.Count);

        // Step 2: Compare against candidates
        Listing? bestMatch = null;
        int bestMatchCount = 0;
        double bestSimilarity = 0;

        var candidateList = candidates.ToList();
        foreach (var candidate in candidateList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(candidate.ImageHashes))
            {
                continue;
            }

            try
            {
                var candidateHashes = DeserializeHashes(candidate.ImageHashes);
                if (candidateHashes.Length == 0)
                {
                    continue;
                }

                var comparison = CompareHashes(inputHashes, candidateHashes);

                if (comparison.MatchingCount > bestMatchCount ||
                    (comparison.MatchingCount == bestMatchCount && comparison.AverageSimilarity > bestSimilarity))
                {
                    bestMatchCount = comparison.MatchingCount;
                    bestSimilarity = comparison.AverageSimilarity;
                    bestMatch = candidate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse image hashes for listing {ListingId}",
                    candidate.ListingId.Value);
            }
        }

        // Step 3: Determine if we have a valid match (majority rule)
        var majorityThreshold = (int)Math.Ceiling(inputHashes.Count * MajorityThresholdRatio);
        if (bestMatchCount >= majorityThreshold && bestMatch != null)
        {
            _logger.LogInformation(
                "Found image match: {MatchingCount}/{TotalCount} images matched with {Similarity:F1}% similarity for listing {ListingId}",
                bestMatchCount, inputHashes.Count, bestSimilarity, bestMatch.ListingId.Value);

            return ImageMatchResult.Match(
                bestMatch,
                bestMatchCount,
                inputHashes.Count,
                bestSimilarity);
        }

        _logger.LogDebug("No image match found (best: {BestCount}/{Total} images)",
            bestMatchCount, inputHashes.Count);
        return ImageMatchResult.NoMatch(inputHashes.Count);
    }

    public ImageComparisonResult CompareHashes(IEnumerable<ulong> hashes1, IEnumerable<ulong> hashes2)
    {
        var list1 = hashes1.ToList();
        var list2 = hashes2.ToList();

        if (list1.Count == 0 || list2.Count == 0)
        {
            return new ImageComparisonResult
            {
                MatchingCount = 0,
                FirstSetCount = list1.Count,
                SecondSetCount = list2.Count,
                IsMajorityMatch = false,
                AverageSimilarity = 0,
                BestMatchSimilarity = 0
            };
        }

        int matchingCount = 0;
        double totalSimilarity = 0;
        double bestSimilarity = 0;

        foreach (var hash1 in list1)
        {
            double bestForThisHash = 0;
            bool foundMatch = false;

            foreach (var hash2 in list2)
            {
                var similarity = _hashCalculator.SimilarityPercentage(hash1, hash2);
                if (similarity > bestForThisHash)
                {
                    bestForThisHash = similarity;
                }

                if (_hashCalculator.IsSimilar(hash1, hash2, SimilarityThreshold))
                {
                    foundMatch = true;
                }
            }

            if (foundMatch)
            {
                matchingCount++;
            }

            totalSimilarity += bestForThisHash;
            if (bestForThisHash > bestSimilarity)
            {
                bestSimilarity = bestForThisHash;
            }
        }

        var averageSimilarity = list1.Count > 0 ? totalSimilarity / list1.Count : 0;
        var majorityThreshold = (int)Math.Ceiling(list1.Count * MajorityThresholdRatio);

        return new ImageComparisonResult
        {
            MatchingCount = matchingCount,
            FirstSetCount = list1.Count,
            SecondSetCount = list2.Count,
            IsMajorityMatch = matchingCount >= majorityThreshold,
            AverageSimilarity = averageSimilarity,
            BestMatchSimilarity = bestSimilarity
        };
    }

    private static ulong[] DeserializeHashes(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ulong>();
        }

        try
        {
            return JsonSerializer.Deserialize<ulong[]>(json) ?? Array.Empty<ulong>();
        }
        catch
        {
            return Array.Empty<ulong>();
        }
    }

    /// <summary>
    /// Serializes image hashes to JSON for storage.
    /// </summary>
    public static string SerializeHashes(IEnumerable<ulong> hashes)
    {
        return JsonSerializer.Serialize(hashes.ToArray());
    }
}
