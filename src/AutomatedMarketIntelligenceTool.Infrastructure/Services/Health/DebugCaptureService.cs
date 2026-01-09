using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;

/// <summary>
/// Service for capturing screenshots and HTML for debugging purposes.
/// </summary>
public interface IDebugCaptureService
{
    /// <summary>
    /// Captures a screenshot of the current page.
    /// </summary>
    Task<string?> CaptureScreenshotAsync(IPage page, string siteName, string? reason = null);

    /// <summary>
    /// Saves the HTML source of the current page.
    /// </summary>
    Task<string?> SaveHtmlAsync(IPage page, string siteName, string? reason = null);

    /// <summary>
    /// Gets or sets the directory where debug artifacts are stored.
    /// </summary>
    string DebugArtifactsPath { get; set; }
}

/// <summary>
/// Implementation of debug capture service for screenshots and HTML saving.
/// </summary>
public class DebugCaptureService : IDebugCaptureService
{
    private readonly ILogger<DebugCaptureService> _logger;
    private string _debugArtifactsPath;

    public string DebugArtifactsPath
    {
        get => _debugArtifactsPath;
        set => _debugArtifactsPath = value ?? throw new ArgumentNullException(nameof(value));
    }

    public DebugCaptureService(ILogger<DebugCaptureService> logger, string? debugArtifactsPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _debugArtifactsPath = debugArtifactsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutomatedMarketIntelligenceTool",
            "DebugArtifacts");

        // Ensure the directory exists
        Directory.CreateDirectory(_debugArtifactsPath);
    }

    public async Task<string?> CaptureScreenshotAsync(IPage page, string siteName, string? reason = null)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var reasonSuffix = reason != null ? $"-{SanitizeFileName(reason)}" : string.Empty;
            var fileName = $"screenshot-{SanitizeFileName(siteName)}-{timestamp}{reasonSuffix}.png";
            var filePath = Path.Combine(_debugArtifactsPath, fileName);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filePath,
                FullPage = true
            });

            _logger.LogInformation(
                "Screenshot captured for {SiteName}{Reason}: {FilePath}",
                siteName,
                reason != null ? $" ({reason})" : string.Empty,
                filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screenshot for {SiteName}", siteName);
            return null;
        }
    }

    public async Task<string?> SaveHtmlAsync(IPage page, string siteName, string? reason = null)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var reasonSuffix = reason != null ? $"-{SanitizeFileName(reason)}" : string.Empty;
            var fileName = $"html-{SanitizeFileName(siteName)}-{timestamp}{reasonSuffix}.html";
            var filePath = Path.Combine(_debugArtifactsPath, fileName);

            var htmlContent = await page.ContentAsync();
            await File.WriteAllTextAsync(filePath, htmlContent);

            _logger.LogInformation(
                "HTML saved for {SiteName}{Reason}: {FilePath}",
                siteName,
                reason != null ? $" ({reason})" : string.Empty,
                filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save HTML for {SiteName}", siteName);
            return null;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
