namespace AutomatedMarketIntelligenceTool.Core.Utilities;

/// <summary>
/// Provides common string manipulation utilities used across the application.
/// </summary>
public static class StringUtilities
{
    /// <summary>
    /// Escapes a string value for safe inclusion in a CSV file.
    /// Values containing commas, quotes, newlines, or carriage returns are quoted,
    /// with internal quotes doubled.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value suitable for CSV output.</returns>
    public static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Escape values that contain commas, quotes, newlines, or carriage returns
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Sanitizes a string for use as a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <returns>A sanitized file name with invalid characters replaced.</returns>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
