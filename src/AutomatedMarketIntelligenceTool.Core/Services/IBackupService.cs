namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for backing up and restoring the database.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup of the database.
    /// </summary>
    Task<BackupResult> BackupAsync(string? outputPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a database from a backup file.
    /// </summary>
    Task<RestoreResult> RestoreAsync(string backupPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available backup files in the default backup directory.
    /// </summary>
    Task<List<BackupFileInfo>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old backup files according to retention policy.
    /// </summary>
    Task<int> CleanupOldBackupsAsync(int retentionCount = 5, CancellationToken cancellationToken = default);
}

public class BackupResult
{
    public bool IsSuccess { get; set; }
    public string? BackupFilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

public class BackupFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string FileSizeMB => $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
}
