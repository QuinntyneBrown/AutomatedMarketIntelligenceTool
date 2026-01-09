using AutomatedMarketIntelligenceTool.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Backup;

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly string _connectionString;
    private readonly string _defaultBackupDirectory;

    public BackupService(
        IConfiguration configuration,
        ILogger<BackupService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string not found");

        // Default backup directory in user's local app data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _defaultBackupDirectory = Path.Combine(appDataPath, "AutomatedMarketIntelligenceTool", "backups");
        
        // Ensure backup directory exists
        Directory.CreateDirectory(_defaultBackupDirectory);
    }

    public async Task<BackupResult> BackupAsync(string? outputPath = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new BackupResult();

        try
        {
            // Determine output path
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                outputPath = Path.Combine(_defaultBackupDirectory, $"backup_{timestamp}.db");
            }
            else
            {
                // Ensure directory exists for custom path
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            _logger.LogInformation("Starting database backup to: {OutputPath}", outputPath);

            // Extract database file path from connection string
            var dbFilePath = ExtractDatabasePath(_connectionString);
            
            if (string.IsNullOrEmpty(dbFilePath))
            {
                result.ErrorMessage = "Could not determine database file path from connection string";
                return result;
            }

            if (!File.Exists(dbFilePath))
            {
                result.ErrorMessage = $"Database file not found: {dbFilePath}";
                return result;
            }

            // Perform the backup (simple file copy for SQLite)
            await Task.Run(() => File.Copy(dbFilePath, outputPath, overwrite: true), cancellationToken);

            // Verify backup was created
            if (!File.Exists(outputPath))
            {
                result.ErrorMessage = "Backup file was not created successfully";
                return result;
            }

            var fileInfo = new FileInfo(outputPath);
            
            result.IsSuccess = true;
            result.BackupFilePath = outputPath;
            result.FileSizeBytes = fileInfo.Length;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Database backup completed successfully. Size: {SizeMB:F2} MB, Duration: {Duration}",
                fileInfo.Length / (1024.0 * 1024.0), result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database backup");
            result.ErrorMessage = ex.Message;
            result.IsSuccess = false;
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<RestoreResult> RestoreAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new RestoreResult();

        try
        {
            if (!File.Exists(backupPath))
            {
                result.ErrorMessage = $"Backup file not found: {backupPath}";
                return result;
            }

            _logger.LogInformation("Starting database restore from: {BackupPath}", backupPath);

            // Extract database file path from connection string
            var dbFilePath = ExtractDatabasePath(_connectionString);
            
            if (string.IsNullOrEmpty(dbFilePath))
            {
                result.ErrorMessage = "Could not determine database file path from connection string";
                return result;
            }

            // Create backup of current database before restore
            if (File.Exists(dbFilePath))
            {
                var preRestoreBackup = $"{dbFilePath}.prerestore_{DateTime.Now:yyyyMMddHHmmss}";
                _logger.LogInformation("Creating pre-restore backup: {PreRestoreBackup}", preRestoreBackup);
                await Task.Run(() => File.Copy(dbFilePath, preRestoreBackup, overwrite: true), cancellationToken);
            }

            // Ensure database directory exists
            var dbDirectory = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrEmpty(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            // Perform the restore (overwrite existing database)
            await Task.Run(() => File.Copy(backupPath, dbFilePath, overwrite: true), cancellationToken);

            result.IsSuccess = true;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Database restore completed successfully. Duration: {Duration}", result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring database from backup");
            result.ErrorMessage = ex.Message;
            result.IsSuccess = false;
        }

        result.Duration = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<List<BackupFileInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backups = new List<BackupFileInfo>();

        try
        {
            if (!Directory.Exists(_defaultBackupDirectory))
            {
                return backups;
            }

            var backupFiles = Directory.GetFiles(_defaultBackupDirectory, "backup_*.db")
                .OrderByDescending(f => File.GetCreationTime(f));

            await Task.Run(() =>
            {
                foreach (var filePath in backupFiles)
                {
                    var fileInfo = new FileInfo(filePath);
                    backups.Add(new BackupFileInfo
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        FileSizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime
                    });
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup files");
        }

        return backups;
    }

    public async Task<int> CleanupOldBackupsAsync(int retentionCount = 5, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;

        try
        {
            if (!Directory.Exists(_defaultBackupDirectory))
            {
                return deletedCount;
            }

            var backupFiles = Directory.GetFiles(_defaultBackupDirectory, "backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            // Keep only the most recent backups according to retention policy
            var filesToDelete = backupFiles.Skip(retentionCount);

            await Task.Run(() =>
            {
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        deletedCount++;
                        _logger.LogInformation("Deleted old backup: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete backup file: {FileName}", file.Name);
                    }
                }
            }, cancellationToken);

            _logger.LogInformation("Cleaned up {DeletedCount} old backup files", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups");
        }

        return deletedCount;
    }

    private string? ExtractDatabasePath(string connectionString)
    {
        // Parse SQLite connection string to extract Data Source
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        return builder.DataSource;
    }
}
