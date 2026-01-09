using AutomatedMarketIntelligenceTool.Cli.Configuration;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Configuration;

public class ConfigurationManagerTests
{
    private readonly string _testConfigPath;

    public ConfigurationManagerTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.json");
    }

    [Fact]
    public void LoadSettings_WithNoFile_ReturnsDefaultSettings()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        // Act
        var settings = manager.LoadSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.Database.Provider.Should().Be("SQLite");
        settings.Scraping.DefaultDelayMs.Should().Be(3000);
    }

    [Fact]
    public void SaveSettings_CreatesConfigFile()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        var settings = new AppSettings
        {
            Database = new DatabaseSettings { Provider = "SQLServer" }
        };

        try
        {
            // Act
            manager.SaveSettings(settings);

            // Assert
            File.Exists(_testConfigPath).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void SaveAndLoadSettings_PersistsData()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        var originalSettings = new AppSettings
        {
            Database = new DatabaseSettings { Provider = "SQLServer" },
            Scraping = new ScrapingSettings { DefaultDelayMs = 5000 }
        };

        try
        {
            // Act
            manager.SaveSettings(originalSettings);
            var loadedSettings = manager.LoadSettings();

            // Assert
            loadedSettings.Database.Provider.Should().Be("SQLServer");
            loadedSettings.Scraping.DefaultDelayMs.Should().Be(5000);
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void GetValue_ReturnsCorrectValue()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        var settings = new AppSettings
        {
            Database = new DatabaseSettings { Provider = "SQLServer" }
        };

        try
        {
            manager.SaveSettings(settings);

            // Act
            var value = manager.GetValue("Database:Provider");

            // Assert
            value.Should().Be("SQLServer");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void GetValue_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var value = manager.GetValue("Invalid:Key");

            // Assert
            value.Should().BeNull();
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void SetValue_UpdatesConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var result = manager.SetValue("Database:Provider", "SQLServer");
            var value = manager.GetValue("Database:Provider");

            // Assert
            result.Should().BeTrue();
            value.Should().Be("SQLServer");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void SetValue_WithIntValue_UpdatesConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var result = manager.SetValue("Scraping:DefaultDelayMs", "5000");
            var value = manager.GetValue("Scraping:DefaultDelayMs");

            // Assert
            result.Should().BeTrue();
            value.Should().Be("5000");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void SetValue_WithBoolValue_UpdatesConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var result = manager.SetValue("Output:ColorEnabled", "false");
            var value = manager.GetValue("Output:ColorEnabled");

            // Assert
            result.Should().BeTrue();
            value.Should().Be("False");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void SetValue_WithInvalidKey_ReturnsFalse()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var result = manager.SetValue("Invalid:Key", "value");

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void GetAllSettings_ReturnsAllConfigurationKeys()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        try
        {
            // Act
            var allSettings = manager.GetAllSettings();

            // Assert
            allSettings.Should().ContainKey("Database:Provider");
            allSettings.Should().ContainKey("Scraping:DefaultDelayMs");
            allSettings.Should().ContainKey("Search:DefaultRadius");
            allSettings.Should().ContainKey("Output:ColorEnabled");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultSettings()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        manager.SetValue("Database:Provider", "SQLServer");

        try
        {
            // Act
            manager.ResetToDefaults();
            var value = manager.GetValue("Database:Provider");

            // Assert
            value.Should().Be("SQLite");
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }

    [Fact]
    public void GetConfigFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        // Act
        var path = manager.GetConfigFilePath();

        // Assert
        path.Should().Be(_testConfigPath);
    }
}
