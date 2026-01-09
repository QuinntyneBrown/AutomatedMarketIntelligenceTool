using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Backup;

public class BackupServiceTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly string _testDirectory;
    private readonly string _testDbPath;
    private readonly string _backupDirectory;

    public BackupServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BackupServiceTests_" + Guid.NewGuid());
        _backupDirectory = Path.Combine(_testDirectory, "backups");
        _testDbPath = Path.Combine(_testDirectory, "test.db");
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_backupDirectory);

        // Create a test database file
        File.WriteAllText(_testDbPath, "Test database content");

        // Create actual configuration instead of mocking
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}"
        });
        _configuration = configBuilder.Build() as IConfiguration ?? throw new InvalidOperationException();

        _logger = new LoggerFactory().CreateLogger<BackupService>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BackupService(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BackupService(_configuration, null!));
    }

    [Fact]
    public async Task BackupAsync_WithValidDatabase_ShouldCreateBackup()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        var outputPath = Path.Combine(_testDirectory, "backup_test.db");

        // Act
        var result = await service.BackupAsync(outputPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.BackupFilePath);
        Assert.Equal(outputPath, result.BackupFilePath);
        Assert.True(File.Exists(outputPath));
        Assert.True(result.FileSizeBytes > 0);
    }

    [Fact]
    public async Task BackupAsync_WithoutOutputPath_ShouldUseDefaultLocation()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);

        // Act
        var result = await service.BackupAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.BackupFilePath);
        Assert.Contains("backup_", result.BackupFilePath);
        Assert.True(File.Exists(result.BackupFilePath!));
        
        // Clean up default backup
        if (File.Exists(result.BackupFilePath))
        {
            File.Delete(result.BackupFilePath);
        }
    }

    [Fact]
    public async Task BackupAsync_WithNonExistentDatabase_ShouldReturnError()
    {
        // Arrange
        var nonExistentDbPath = Path.Combine(_testDirectory, "nonexistent.db");
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={nonExistentDbPath}"
        });
        var config = configBuilder.Build();

        var service = new BackupService(config, _logger);
        var outputPath = Path.Combine(_testDirectory, "backup.db");

        // Act
        var result = await service.BackupAsync(outputPath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RestoreAsync_WithValidBackup_ShouldRestoreDatabase()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        var backupPath = Path.Combine(_testDirectory, "backup.db");
        
        // Create a backup file
        File.WriteAllText(backupPath, "Backup content");

        // Act
        var result = await service.RestoreAsync(backupPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        
        // Verify database was restored
        var dbContent = await File.ReadAllTextAsync(_testDbPath);
        Assert.Equal("Backup content", dbContent);
    }

    [Fact]
    public async Task RestoreAsync_WithNonExistentBackup_ShouldReturnError()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        var nonExistentBackup = Path.Combine(_testDirectory, "nonexistent.db");

        // Act
        var result = await service.RestoreAsync(nonExistentBackup);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RestoreAsync_ShouldCreatePreRestoreBackup()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        var backupPath = Path.Combine(_testDirectory, "backup.db");
        
        // Create a backup file
        File.WriteAllText(backupPath, "Backup content");
        
        // Ensure original database exists
        File.WriteAllText(_testDbPath, "Original content");

        // Act
        var result = await service.RestoreAsync(backupPath);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify pre-restore backup was created
        var preRestoreFiles = Directory.GetFiles(_testDirectory, "*.prerestore_*");
        Assert.NotEmpty(preRestoreFiles);
    }

    [Fact]
    public async Task ListBackupsAsync_WithNoBackups_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);

        // Act
        var result = await service.ListBackupsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListBackupsAsync_WithBackups_ShouldReturnBackupList()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        
        // Create some backup files (will use default location)
        var backup1 = await service.BackupAsync();
        await Task.Delay(100); // Ensure different timestamps
        var backup2 = await service.BackupAsync();

        // Act
        var result = await service.ListBackupsAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count >= 2);
        Assert.All(result, b => Assert.Contains("backup_", b.FileName));
        
        // Clean up backups
        foreach (var backup in result)
        {
            if (File.Exists(backup.FilePath))
            {
                File.Delete(backup.FilePath);
            }
        }
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithNoBackups_ShouldReturnZero()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);

        // Act
        var result = await service.CleanupOldBackupsAsync(5);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithMoreThanRetention_ShouldDeleteOldBackups()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        
        // Create multiple backups
        var backups = new List<BackupResult>();
        for (int i = 0; i < 7; i++)
        {
            var backup = await service.BackupAsync();
            backups.Add(backup);
            Assert.True(backup.IsSuccess, $"Backup {i} should succeed");
            Assert.NotNull(backup.BackupFilePath);
            Assert.True(File.Exists(backup.BackupFilePath), $"Backup file should exist at {backup.BackupFilePath}");
            await Task.Delay(100); // Ensure different timestamps
        }

        // Verify all 7 backups exist before cleanup
        var backupListBefore = await service.ListBackupsAsync();
        Assert.True(backupListBefore.Count >= 7, $"Should have at least 7 backups, found {backupListBefore.Count}");

        // Act
        var deletedCount = await service.CleanupOldBackupsAsync(retentionCount: 5);

        // Assert
        Assert.Equal(2, deletedCount);
        
        // Verify only 5 backups remain
        var remainingBackups = await service.ListBackupsAsync();
        Assert.Equal(5, remainingBackups.Count);
        
        // Clean up remaining backups
        foreach (var backup in remainingBackups)
        {
            if (File.Exists(backup.FilePath))
            {
                File.Delete(backup.FilePath);
            }
        }
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithLessThanRetention_ShouldNotDeleteAny()
    {
        // Arrange
        var service = new BackupService(_configuration, _logger);
        
        // Create fewer backups than retention
        var backup1 = await service.BackupAsync();
        var backup2 = await service.BackupAsync();

        // Act
        var deletedCount = await service.CleanupOldBackupsAsync(retentionCount: 5);

        // Assert
        Assert.Equal(0, deletedCount);
        
        // Clean up backups
        var backups = await service.ListBackupsAsync();
        foreach (var backup in backups)
        {
            if (File.Exists(backup.FilePath))
            {
                File.Delete(backup.FilePath);
            }
        }
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
