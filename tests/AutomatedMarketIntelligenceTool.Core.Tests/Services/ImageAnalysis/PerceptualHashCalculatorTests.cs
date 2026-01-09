using AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.ImageAnalysis;

public class PerceptualHashCalculatorTests
{
    private readonly PerceptualHashCalculator _calculator;

    public PerceptualHashCalculatorTests()
    {
        _calculator = new PerceptualHashCalculator();
    }

    [Fact]
    public void CalculateHash_WithValidImage_ShouldReturnNonZeroHash()
    {
        // Arrange
        var imageData = CreateTestImage(256, 256, 128); // Gray image

        // Act
        var hash = _calculator.CalculateHash(imageData);

        // Assert
        Assert.NotEqual(0UL, hash);
    }

    [Fact]
    public void CalculateHash_WithSameImage_ShouldReturnSameHash()
    {
        // Arrange
        var imageData1 = CreateTestImage(256, 256, 100);
        var imageData2 = CreateTestImage(256, 256, 100);

        // Act
        var hash1 = _calculator.CalculateHash(imageData1);
        var hash2 = _calculator.CalculateHash(imageData2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void CalculateHash_WithDifferentImages_ShouldReturnDifferentHashes()
    {
        // Arrange
        var blackImage = CreateTestImage(256, 256, 0);
        var whiteImage = CreateTestImage(256, 256, 255);

        // Act
        var hash1 = _calculator.CalculateHash(blackImage);
        var hash2 = _calculator.CalculateHash(whiteImage);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void CalculateHash_WithNullData_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _calculator.CalculateHash(null!));
    }

    [Fact]
    public void CalculateHash_WithEmptyData_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _calculator.CalculateHash(Array.Empty<byte>()));
    }

    [Fact]
    public void HammingDistance_WithIdenticalHashes_ShouldReturnZero()
    {
        // Arrange
        ulong hash = 0xABCDEF0123456789;

        // Act
        var distance = _calculator.HammingDistance(hash, hash);

        // Assert
        Assert.Equal(0, distance);
    }

    [Fact]
    public void HammingDistance_WithCompletelyDifferentHashes_ShouldReturn64()
    {
        // Arrange
        ulong hash1 = 0xFFFFFFFFFFFFFFFF; // All 1s
        ulong hash2 = 0x0000000000000000; // All 0s

        // Act
        var distance = _calculator.HammingDistance(hash1, hash2);

        // Assert
        Assert.Equal(64, distance);
    }

    [Fact]
    public void HammingDistance_WithOneBitDifferent_ShouldReturnOne()
    {
        // Arrange
        ulong hash1 = 0x0000000000000001;
        ulong hash2 = 0x0000000000000000;

        // Act
        var distance = _calculator.HammingDistance(hash1, hash2);

        // Assert
        Assert.Equal(1, distance);
    }

    [Fact]
    public void IsSimilar_WithIdenticalHashes_ShouldReturnTrue()
    {
        // Arrange
        ulong hash = 0xABCDEF0123456789;

        // Act
        var isSimilar = _calculator.IsSimilar(hash, hash);

        // Assert
        Assert.True(isSimilar);
    }

    [Fact]
    public void IsSimilar_WithDistanceBelowThreshold_ShouldReturnTrue()
    {
        // Arrange
        ulong hash1 = 0x0000000000000000;
        ulong hash2 = 0x00000000000000FF; // 8 bits different

        // Act
        var isSimilar = _calculator.IsSimilar(hash1, hash2, threshold: 10);

        // Assert
        Assert.True(isSimilar);
    }

    [Fact]
    public void IsSimilar_WithDistanceAboveThreshold_ShouldReturnFalse()
    {
        // Arrange
        ulong hash1 = 0x0000000000000000;
        ulong hash2 = 0xFFFFFFFFFFFFFFFF; // 64 bits different

        // Act
        var isSimilar = _calculator.IsSimilar(hash1, hash2, threshold: 10);

        // Assert
        Assert.False(isSimilar);
    }

    [Fact]
    public void IsSimilar_WithDefaultThreshold_ShouldUse10()
    {
        // Arrange - 10 bits different
        ulong hash1 = 0x0000000000000000;
        ulong hash2 = 0x00000000000003FF; // 10 bits set

        // Act
        var isSimilar = _calculator.IsSimilar(hash1, hash2);

        // Assert
        Assert.True(isSimilar); // Exactly at threshold
    }

    [Fact]
    public void SimilarityPercentage_WithIdenticalHashes_ShouldReturn100()
    {
        // Arrange
        ulong hash = 0xABCDEF0123456789;

        // Act
        var similarity = _calculator.SimilarityPercentage(hash, hash);

        // Assert
        Assert.Equal(100.0, similarity);
    }

    [Fact]
    public void SimilarityPercentage_WithCompletelyDifferent_ShouldReturnZero()
    {
        // Arrange
        ulong hash1 = 0xFFFFFFFFFFFFFFFF;
        ulong hash2 = 0x0000000000000000;

        // Act
        var similarity = _calculator.SimilarityPercentage(hash1, hash2);

        // Assert
        Assert.Equal(0.0, similarity);
    }

    [Fact]
    public void SimilarityPercentage_WithHalfDifferent_ShouldReturn50()
    {
        // Arrange - 32 bits different
        ulong hash1 = 0xFFFFFFFF00000000;
        ulong hash2 = 0x0000000000000000;

        // Act
        var similarity = _calculator.SimilarityPercentage(hash1, hash2);

        // Assert
        Assert.Equal(50.0, similarity);
    }

    [Fact]
    public void HashToString_ShouldReturnHexString()
    {
        // Arrange
        ulong hash = 0xABCDEF0123456789;

        // Act
        var str = PerceptualHashCalculator.HashToString(hash);

        // Assert
        Assert.Equal("ABCDEF0123456789", str);
    }

    [Fact]
    public void HashToString_WithZero_ShouldReturnPaddedZeros()
    {
        // Arrange
        ulong hash = 0;

        // Act
        var str = PerceptualHashCalculator.HashToString(hash);

        // Assert
        Assert.Equal("0000000000000000", str);
    }

    [Fact]
    public void StringToHash_ShouldParseHexString()
    {
        // Arrange
        var str = "ABCDEF0123456789";

        // Act
        var hash = PerceptualHashCalculator.StringToHash(str);

        // Assert
        Assert.Equal(0xABCDEF0123456789UL, hash);
    }

    [Fact]
    public void StringToHash_WithLowercase_ShouldParseCorrectly()
    {
        // Arrange
        var str = "abcdef0123456789";

        // Act
        var hash = PerceptualHashCalculator.StringToHash(str);

        // Assert
        Assert.Equal(0xABCDEF0123456789UL, hash);
    }

    [Fact]
    public void StringToHash_WithNullOrEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PerceptualHashCalculator.StringToHash(null!));
        Assert.Throws<ArgumentException>(() => PerceptualHashCalculator.StringToHash(string.Empty));
    }

    [Fact]
    public void HashToString_StringToHash_ShouldRoundtrip()
    {
        // Arrange
        ulong originalHash = 0x123456789ABCDEF0;

        // Act
        var str = PerceptualHashCalculator.HashToString(originalHash);
        var parsedHash = PerceptualHashCalculator.StringToHash(str);

        // Assert
        Assert.Equal(originalHash, parsedHash);
    }

    // Helper method to create a simple grayscale test image as PNG
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
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
