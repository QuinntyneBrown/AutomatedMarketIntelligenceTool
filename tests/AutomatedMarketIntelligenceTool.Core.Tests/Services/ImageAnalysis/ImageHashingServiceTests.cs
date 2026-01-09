using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.ImageAnalysis;

public class ImageHashingServiceTests
{
    private readonly Mock<IImageDownloadService> _mockDownloadService;
    private readonly PerceptualHashCalculator _hashCalculator;
    private readonly ImageHashingService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ImageHashingServiceTests()
    {
        _mockDownloadService = new Mock<IImageDownloadService>();
        _hashCalculator = new PerceptualHashCalculator();
        _service = new ImageHashingService(
            _mockDownloadService.Object,
            _hashCalculator,
            NullLogger<ImageHashingService>.Instance);
    }

    [Fact]
    public async Task ComputeHashesAsync_WithNoUrls_ShouldReturnEmptyResult()
    {
        // Act
        var result = await _service.ComputeHashesAsync(Array.Empty<string>());

        // Assert
        Assert.False(result.HasHashes);
        Assert.Empty(result.Hashes);
        Assert.Equal(0, result.SuccessfulCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task ComputeHashesAsync_WithNullUrls_ShouldReturnEmptyResult()
    {
        // Act
        var result = await _service.ComputeHashesAsync(null!);

        // Assert
        Assert.False(result.HasHashes);
        Assert.Empty(result.Hashes);
    }

    [Fact]
    public async Task ComputeHashesAsync_WithSuccessfulDownloads_ShouldReturnHashes()
    {
        // Arrange
        var urls = new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" };
        var imageData = CreateTestImage(256, 256, 128);

        _mockDownloadService
            .Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageDownloadResult.Succeeded("url", imageData, "image/jpeg"));

        // Act
        var result = await _service.ComputeHashesAsync(urls);

        // Assert
        Assert.True(result.HasHashes);
        Assert.Equal(2, result.Hashes.Count);
        Assert.Equal(2, result.SuccessfulCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task ComputeHashesAsync_WithFailedDownloads_ShouldCountFailures()
    {
        // Arrange
        var urls = new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" };

        _mockDownloadService
            .Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageDownloadResult.Failed("url", "Download failed"));

        // Act
        var result = await _service.ComputeHashesAsync(urls);

        // Assert
        Assert.False(result.HasHashes);
        Assert.Empty(result.Hashes);
        Assert.Equal(0, result.SuccessfulCount);
        Assert.Equal(2, result.FailedCount);
    }

    [Fact]
    public async Task ComputeHashesAsync_WithMixedResults_ShouldCountCorrectly()
    {
        // Arrange
        var urls = new[] { "https://example.com/1.jpg", "https://example.com/2.jpg", "https://example.com/3.jpg" };
        var imageData = CreateTestImage(256, 256, 128);

        var callCount = 0;
        _mockDownloadService
            .Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount % 2 == 0
                    ? ImageDownloadResult.Failed("url", "Error")
                    : ImageDownloadResult.Succeeded("url", imageData, "image/jpeg");
            });

        // Act
        var result = await _service.ComputeHashesAsync(urls);

        // Assert
        Assert.True(result.HasHashes);
        Assert.Equal(2, result.SuccessfulCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task ComputeHashesAsync_ShouldRespectMaxImages()
    {
        // Arrange
        var urls = new[] { "url1", "url2", "url3", "url4", "url5" };
        var imageData = CreateTestImage(256, 256, 128);

        _mockDownloadService
            .Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageDownloadResult.Succeeded("url", imageData, "image/jpeg"));

        // Act
        var result = await _service.ComputeHashesAsync(urls, maxImages: 2);

        // Assert
        Assert.Equal(2, result.Hashes.Count);
    }

    [Fact]
    public async Task FindImageMatchesAsync_WithNoInputImages_ShouldReturnNoImages()
    {
        // Arrange
        var candidates = new List<Listing>();

        // Act
        var result = await _service.FindImageMatchesAsync(Array.Empty<string>(), candidates);

        // Assert
        Assert.False(result.HasMatch);
        Assert.Equal(0, result.TotalImageCount);
    }

    [Fact]
    public async Task FindImageMatchesAsync_WithNoCandidates_ShouldReturnNoMatch()
    {
        // Arrange
        var urls = new[] { "https://example.com/1.jpg" };
        var imageData = CreateTestImage(256, 256, 128);

        _mockDownloadService
            .Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageDownloadResult.Succeeded("url", imageData, "image/jpeg"));

        // Act
        var result = await _service.FindImageMatchesAsync(urls, Array.Empty<Listing>());

        // Assert
        Assert.False(result.HasMatch);
    }

    [Fact]
    public void CompareHashes_WithEmptyFirstSet_ShouldReturnZeroMatch()
    {
        // Act
        var result = _service.CompareHashes(Array.Empty<ulong>(), new ulong[] { 1, 2, 3 });

        // Assert
        Assert.Equal(0, result.MatchingCount);
        Assert.Equal(0, result.FirstSetCount);
        Assert.Equal(3, result.SecondSetCount);
        Assert.False(result.IsMajorityMatch);
    }

    [Fact]
    public void CompareHashes_WithEmptySecondSet_ShouldReturnZeroMatch()
    {
        // Act
        var result = _service.CompareHashes(new ulong[] { 1, 2, 3 }, Array.Empty<ulong>());

        // Assert
        Assert.Equal(0, result.MatchingCount);
        Assert.Equal(3, result.FirstSetCount);
        Assert.Equal(0, result.SecondSetCount);
        Assert.False(result.IsMajorityMatch);
    }

    [Fact]
    public void CompareHashes_WithIdenticalHashes_ShouldReturnAllMatches()
    {
        // Arrange
        var hashes = new ulong[] { 0x1234567890ABCDEF, 0xFEDCBA0987654321 };

        // Act
        var result = _service.CompareHashes(hashes, hashes);

        // Assert
        Assert.Equal(2, result.MatchingCount);
        Assert.True(result.IsMajorityMatch);
        Assert.Equal(100.0, result.BestMatchSimilarity);
    }

    [Fact]
    public void CompareHashes_WithSimilarHashes_ShouldFindMatches()
    {
        // Arrange - hashes that differ by only a few bits
        var hashes1 = new ulong[] { 0x0000000000000000 };
        var hashes2 = new ulong[] { 0x00000000000000FF }; // 8 bits different

        // Act
        var result = _service.CompareHashes(hashes1, hashes2);

        // Assert
        Assert.Equal(1, result.MatchingCount); // Within threshold of 10
        Assert.True(result.IsMajorityMatch);
    }

    [Fact]
    public void CompareHashes_WithDifferentHashes_ShouldNotMatch()
    {
        // Arrange - completely different hashes
        var hashes1 = new ulong[] { 0x0000000000000000 };
        var hashes2 = new ulong[] { 0xFFFFFFFFFFFFFFFF }; // All bits different

        // Act
        var result = _service.CompareHashes(hashes1, hashes2);

        // Assert
        Assert.Equal(0, result.MatchingCount);
        Assert.False(result.IsMajorityMatch);
        Assert.Equal(0.0, result.BestMatchSimilarity);
    }

    [Fact]
    public void SerializeHashes_ShouldReturnJsonArray()
    {
        // Arrange
        var hashes = new ulong[] { 0x1234567890ABCDEF, 0xFEDCBA0987654321 };

        // Act
        var json = ImageHashingService.SerializeHashes(hashes);

        // Assert
        Assert.Contains("1311768467294899695", json); // 0x1234567890ABCDEF as decimal
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
    }

    [Fact]
    public void ImageMatchResult_NoImages_ShouldHaveCorrectProperties()
    {
        // Act
        var result = ImageMatchResult.NoImages();

        // Assert
        Assert.False(result.HasMatch);
        Assert.Null(result.MatchedListing);
        Assert.Equal(0, result.MatchingImageCount);
        Assert.Equal(0, result.TotalImageCount);
        Assert.Equal(0, result.SimilarityScore);
    }

    [Fact]
    public void ImageMatchResult_NoMatch_ShouldHaveCorrectProperties()
    {
        // Act
        var result = ImageMatchResult.NoMatch(3);

        // Assert
        Assert.False(result.HasMatch);
        Assert.Null(result.MatchedListing);
        Assert.Equal(0, result.MatchingImageCount);
        Assert.Equal(3, result.TotalImageCount);
    }

    [Fact]
    public void ImageMatchResult_Match_ShouldHaveCorrectProperties()
    {
        // Arrange
        var listing = CreateTestListing();

        // Act
        var result = ImageMatchResult.Match(listing, 2, 3, 85.5);

        // Assert
        Assert.True(result.HasMatch);
        Assert.Same(listing, result.MatchedListing);
        Assert.Equal(2, result.MatchingImageCount);
        Assert.Equal(3, result.TotalImageCount);
        Assert.Equal(85.5, result.SimilarityScore);
    }

    [Fact]
    public void ImageHashResult_Empty_ShouldHaveCorrectProperties()
    {
        // Act
        var result = ImageHashResult.Empty();

        // Assert
        Assert.False(result.HasHashes);
        Assert.Empty(result.Hashes);
        Assert.Equal(0, result.SuccessfulCount);
        Assert.Equal(0, result.FailedCount);
    }

    private static byte[] CreateTestImage(int width, int height, byte grayValue)
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(grayValue, grayValue, grayValue, 255);
            }
        }
        using var ms = new MemoryStream();
        image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        return ms.ToArray();
    }

    private Listing CreateTestListing()
    {
        return Listing.Create(
            _testTenantId,
            "EXT-001",
            "TestSite",
            "https://test.com/1",
            "Toyota",
            "Camry",
            2020,
            25000m,
            Condition.Used);
    }
}
