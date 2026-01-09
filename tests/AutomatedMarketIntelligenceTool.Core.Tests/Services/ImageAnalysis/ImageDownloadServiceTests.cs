using AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.ImageAnalysis;

public class ImageDownloadServiceTests
{
    private readonly ImageDownloadService _service;

    public ImageDownloadServiceTests()
    {
        var httpClient = new HttpClient();
        _service = new ImageDownloadService(httpClient, NullLogger<ImageDownloadService>.Instance);
    }

    [Fact]
    public async Task DownloadAsync_WithNullUrl_ShouldReturnFailedResult()
    {
        // Act
        var result = await _service.DownloadAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyUrl_ShouldReturnFailedResult()
    {
        // Act
        var result = await _service.DownloadAsync(string.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_WithWhitespaceUrl_ShouldReturnFailedResult()
    {
        // Act
        var result = await _service.DownloadAsync("   ");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidUrl_ShouldReturnFailedResult()
    {
        // Act
        var result = await _service.DownloadAsync("not-a-valid-url");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("not-a-valid-url", result.Url);
        Assert.Contains("Invalid URL", result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_WithNonExistentDomain_ShouldReturnFailedResult()
    {
        // Act
        var result = await _service.DownloadAsync("https://this-domain-does-not-exist-12345.com/image.jpg");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadMultipleAsync_WithEmptyList_ShouldReturnEmptyResults()
    {
        // Act
        var results = await _service.DownloadMultipleAsync(Array.Empty<string>());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task DownloadMultipleAsync_WithNullList_ShouldReturnEmptyResults()
    {
        // Act
        var results = await _service.DownloadMultipleAsync(null!);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task DownloadMultipleAsync_ShouldRespectMaxImages()
    {
        // Arrange
        var urls = new[]
        {
            "https://example.com/1.jpg",
            "https://example.com/2.jpg",
            "https://example.com/3.jpg",
            "https://example.com/4.jpg",
            "https://example.com/5.jpg"
        };

        // Act
        var results = await _service.DownloadMultipleAsync(urls, maxImages: 2);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task DownloadMultipleAsync_WithDefaultMaxImages_ShouldDownloadThree()
    {
        // Arrange
        var urls = new[]
        {
            "https://example.com/1.jpg",
            "https://example.com/2.jpg",
            "https://example.com/3.jpg",
            "https://example.com/4.jpg",
            "https://example.com/5.jpg"
        };

        // Act
        var results = await _service.DownloadMultipleAsync(urls);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ImageDownloadResult_Succeeded_ShouldHaveCorrectProperties()
    {
        // Arrange
        var url = "https://example.com/test.jpg";
        var data = new byte[] { 1, 2, 3 };
        var contentType = "image/jpeg";

        // Act
        var result = ImageDownloadResult.Succeeded(url, data, contentType);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(url, result.Url);
        Assert.Equal(data, result.Data);
        Assert.Equal(contentType, result.ContentType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ImageDownloadResult_Failed_ShouldHaveCorrectProperties()
    {
        // Arrange
        var url = "https://example.com/test.jpg";
        var errorMessage = "Connection failed";

        // Act
        var result = ImageDownloadResult.Failed(url, errorMessage);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(url, result.Url);
        Assert.Null(result.Data);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_WithCancellationRequested_ShouldThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.DownloadAsync("https://example.com/image.jpg", cts.Token));
    }
}
