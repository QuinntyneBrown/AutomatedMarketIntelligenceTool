using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;

/// <summary>
/// Calculates perceptual hashes (pHash) for images using DCT-based algorithm.
/// Perceptual hashes allow for similarity comparison between images that may have
/// minor differences (resize, compression, slight color changes).
/// </summary>
public class PerceptualHashCalculator
{
    private const int HashSize = 8;       // 8x8 = 64 bits
    private const int DctSize = 32;       // 32x32 DCT input
    private const int DefaultThreshold = 10;  // Default Hamming distance threshold

    // Pre-computed DCT coefficients for performance
    private readonly double[,] _dctCoefficients;

    public PerceptualHashCalculator()
    {
        _dctCoefficients = ComputeDctCoefficients();
    }

    /// <summary>
    /// Calculates a perceptual hash for the given image data.
    /// </summary>
    /// <param name="imageData">Raw image data as byte array.</param>
    /// <returns>A 64-bit perceptual hash.</returns>
    public ulong CalculateHash(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
        }

        using var image = Image.Load<Rgba32>(imageData);
        return CalculateHashFromImage(image);
    }

    /// <summary>
    /// Calculates a perceptual hash from a stream.
    /// </summary>
    /// <param name="imageStream">Stream containing image data.</param>
    /// <returns>A 64-bit perceptual hash.</returns>
    public ulong CalculateHash(Stream imageStream)
    {
        if (imageStream == null)
        {
            throw new ArgumentNullException(nameof(imageStream));
        }

        using var image = Image.Load<Rgba32>(imageStream);
        return CalculateHashFromImage(image);
    }

    private ulong CalculateHashFromImage(Image<Rgba32> image)
    {
        // Step 1: Convert to grayscale and resize to 32x32
        image.Mutate(x => x
            .Grayscale()
            .Resize(DctSize, DctSize));

        // Step 2: Extract pixel values as grayscale intensities
        var pixels = new double[DctSize, DctSize];
        for (int y = 0; y < DctSize; y++)
        {
            for (int x = 0; x < DctSize; x++)
            {
                pixels[y, x] = image[x, y].R;  // Already grayscale, so R=G=B
            }
        }

        // Step 3: Apply 2D DCT transform
        var dct = ApplyDct(pixels);

        // Step 4: Extract low-frequency 8x8 block (excluding DC component [0,0])
        var subset = new double[HashSize * HashSize];
        int index = 0;
        for (int y = 0; y < HashSize; y++)
        {
            for (int x = 0; x < HashSize; x++)
            {
                // Skip DC component and use next values
                if (y == 0 && x == 0)
                {
                    subset[index++] = dct[0, 1];  // Use [0,1] instead of [0,0]
                }
                else
                {
                    subset[index++] = dct[y, x];
                }
            }
        }

        // Step 5: Calculate median
        var sorted = subset.OrderBy(v => v).ToArray();
        double median = sorted.Length % 2 == 0
            ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0
            : sorted[sorted.Length / 2];

        // Step 6: Generate 64-bit hash based on median comparison
        ulong hash = 0;
        for (int i = 0; i < 64; i++)
        {
            if (subset[i] > median)
            {
                hash |= (1UL << i);
            }
        }

        return hash;
    }

    /// <summary>
    /// Pre-computes DCT coefficients for efficient transformation.
    /// </summary>
    private static double[,] ComputeDctCoefficients()
    {
        var coefficients = new double[DctSize, DctSize];
        double factor = Math.PI / DctSize;

        for (int i = 0; i < DctSize; i++)
        {
            for (int j = 0; j < DctSize; j++)
            {
                coefficients[i, j] = Math.Cos(factor * (j + 0.5) * i);
            }
        }

        return coefficients;
    }

    /// <summary>
    /// Applies 2D DCT (Discrete Cosine Transform) to the pixel matrix.
    /// </summary>
    private double[,] ApplyDct(double[,] pixels)
    {
        var result = new double[DctSize, DctSize];

        // Apply 1D DCT on rows
        var temp = new double[DctSize, DctSize];
        for (int y = 0; y < DctSize; y++)
        {
            for (int u = 0; u < DctSize; u++)
            {
                double sum = 0;
                for (int x = 0; x < DctSize; x++)
                {
                    sum += pixels[y, x] * _dctCoefficients[u, x];
                }
                temp[y, u] = sum;
            }
        }

        // Apply 1D DCT on columns
        for (int u = 0; u < DctSize; u++)
        {
            for (int v = 0; v < DctSize; v++)
            {
                double sum = 0;
                for (int y = 0; y < DctSize; y++)
                {
                    sum += temp[y, u] * _dctCoefficients[v, y];
                }

                // Apply normalization
                double cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                double cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                result[v, u] = 0.25 * cu * cv * sum;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates the Hamming distance between two perceptual hashes.
    /// Lower distance means more similar images.
    /// </summary>
    /// <param name="hash1">First hash.</param>
    /// <param name="hash2">Second hash.</param>
    /// <returns>Number of differing bits (0-64).</returns>
    public int HammingDistance(ulong hash1, ulong hash2)
    {
        var xor = hash1 ^ hash2;
        return BitOperations.PopCount(xor);
    }

    /// <summary>
    /// Determines if two images are similar based on their perceptual hashes.
    /// </summary>
    /// <param name="hash1">First hash.</param>
    /// <param name="hash2">Second hash.</param>
    /// <param name="threshold">Maximum Hamming distance to consider similar (default 10).</param>
    /// <returns>True if images are similar.</returns>
    public bool IsSimilar(ulong hash1, ulong hash2, int threshold = DefaultThreshold)
    {
        return HammingDistance(hash1, hash2) <= threshold;
    }

    /// <summary>
    /// Calculates similarity percentage between two hashes.
    /// </summary>
    /// <param name="hash1">First hash.</param>
    /// <param name="hash2">Second hash.</param>
    /// <returns>Similarity as percentage (0-100).</returns>
    public double SimilarityPercentage(ulong hash1, ulong hash2)
    {
        int distance = HammingDistance(hash1, hash2);
        return (64.0 - distance) / 64.0 * 100.0;
    }

    /// <summary>
    /// Converts a hash to a hexadecimal string representation.
    /// </summary>
    public static string HashToString(ulong hash)
    {
        return hash.ToString("X16");
    }

    /// <summary>
    /// Parses a hexadecimal string to a hash value.
    /// </summary>
    public static ulong StringToHash(string hashString)
    {
        if (string.IsNullOrWhiteSpace(hashString))
        {
            throw new ArgumentException("Hash string cannot be null or empty", nameof(hashString));
        }

        return ulong.Parse(hashString, System.Globalization.NumberStyles.HexNumber);
    }
}
