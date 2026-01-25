using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Image.Infrastructure.Hashing;

/// <summary>
/// Calculates perceptual hashes (pHash) for images using the DCT-based algorithm.
/// </summary>
public sealed class PerceptualHashCalculator
{
    private const int HashSize = 8;
    private const int HighFrequencyFactor = 4;

    /// <summary>
    /// Calculates the perceptual hash of an image.
    /// </summary>
    public string CalculateHash(byte[] imageBytes)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
        return CalculateHashInternal(image);
    }

    /// <summary>
    /// Calculates the perceptual hash of an image from a stream.
    /// </summary>
    public string CalculateHash(Stream imageStream)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageStream);
        return CalculateHashInternal(image);
    }

    private string CalculateHashInternal(Image<Rgba32> image)
    {
        var size = HashSize * HighFrequencyFactor;

        // Resize to 32x32
        image.Mutate(x => x
            .Resize(size, size)
            .Grayscale());

        // Get pixel values
        var pixels = new double[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixel = image[x, y];
                pixels[y, x] = pixel.R;
            }
        }

        // Apply DCT
        var dctCoefficients = ApplyDCT(pixels, size);

        // Extract top-left 8x8 (excluding first coefficient)
        var lowFrequencyCoefficients = new double[HashSize * HashSize - 1];
        var index = 0;
        for (var y = 0; y < HashSize; y++)
        {
            for (var x = 0; x < HashSize; x++)
            {
                if (y == 0 && x == 0) continue; // Skip DC coefficient
                lowFrequencyCoefficients[index++] = dctCoefficients[y, x];
            }
        }

        // Calculate median
        var sorted = lowFrequencyCoefficients.OrderBy(x => x).ToArray();
        var median = sorted[sorted.Length / 2];

        // Build hash
        var hash = 0UL;
        index = 0;
        for (var y = 0; y < HashSize; y++)
        {
            for (var x = 0; x < HashSize; x++)
            {
                if (y == 0 && x == 0) continue;
                if (dctCoefficients[y, x] > median)
                {
                    hash |= 1UL << index;
                }
                index++;
            }
        }

        return hash.ToString("X16");
    }

    private static double[,] ApplyDCT(double[,] input, int size)
    {
        var output = new double[size, size];
        var coefficient = Math.PI / size;

        for (var u = 0; u < size; u++)
        {
            for (var v = 0; v < size; v++)
            {
                var sum = 0.0;
                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        sum += input[i, j] *
                               Math.Cos(coefficient * (i + 0.5) * u) *
                               Math.Cos(coefficient * (j + 0.5) * v);
                    }
                }

                var cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                var cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                output[u, v] = 0.25 * cu * cv * sum;
            }
        }

        return output;
    }

    /// <summary>
    /// Calculates the Hamming distance between two hashes.
    /// </summary>
    public int HammingDistance(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return 64;

        if (!ulong.TryParse(hash1, System.Globalization.NumberStyles.HexNumber, null, out var h1) ||
            !ulong.TryParse(hash2, System.Globalization.NumberStyles.HexNumber, null, out var h2))
            return 64;

        var xor = h1 ^ h2;
        var count = 0;
        while (xor != 0)
        {
            count++;
            xor &= xor - 1;
        }

        return count;
    }

    /// <summary>
    /// Calculates similarity between two hashes (0-1, where 1 is identical).
    /// </summary>
    public double CalculateSimilarity(string hash1, string hash2)
    {
        var distance = HammingDistance(hash1, hash2);
        return 1.0 - (distance / 64.0);
    }
}
