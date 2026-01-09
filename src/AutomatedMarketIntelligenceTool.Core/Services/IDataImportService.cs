namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for importing listing data from external sources.
/// </summary>
public interface IDataImportService
{
    /// <summary>
    /// Imports listings from a CSV file.
    /// </summary>
    Task<ImportResult> ImportFromCsvAsync(string filePath, Guid tenantId, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports listings from a JSON file.
    /// </summary>
    Task<ImportResult> ImportFromJsonAsync(string filePath, Guid tenantId, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates import file without importing.
    /// </summary>
    Task<ValidationResult> ValidateImportFileAsync(string filePath, ImportFormat format, CancellationToken cancellationToken = default);
}

public enum ImportFormat
{
    Csv,
    Json
}

public class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public bool IsDryRun { get; set; }
    public TimeSpan Duration { get; set; }

    public bool IsSuccess => ErrorCount == 0;
}

public class ImportError
{
    public int LineNumber { get; set; }
    public string? RowData { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int EstimatedRowCount { get; set; }
}
