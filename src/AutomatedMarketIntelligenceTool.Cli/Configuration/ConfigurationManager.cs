using System.Text.Json;

namespace AutomatedMarketIntelligenceTool.Cli.Configuration;

/// <summary>
/// Manages application configuration storage and retrieval.
/// </summary>
public class ConfigurationManager
{
    private readonly string _configFilePath;
    private AppSettings? _cachedSettings;

    public ConfigurationManager()
    {
        var configDir = GetConfigDirectory();
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "config.json");
    }

    public ConfigurationManager(string configFilePath)
    {
        _configFilePath = configFilePath;
        var directory = Path.GetDirectoryName(configFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Gets the platform-specific configuration directory.
    /// </summary>
    private static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "car-search");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "car-search");
        }
    }

    /// <summary>
    /// Loads settings from the configuration file.
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        if (!File.Exists(_configFilePath))
        {
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            return _cachedSettings;
        }
        catch
        {
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    /// <summary>
    /// Saves settings to the configuration file.
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_configFilePath, json);
        _cachedSettings = settings;
    }

    /// <summary>
    /// Gets a configuration value by key path (e.g., "Database:Provider").
    /// </summary>
    public string? GetValue(string key)
    {
        var settings = LoadSettings();
        var parts = key.Split(':');

        if (parts.Length == 0)
        {
            return null;
        }

        object? current = settings;
        foreach (var part in parts)
        {
            if (current == null)
            {
                return null;
            }

            var property = current.GetType().GetProperty(part);
            if (property == null)
            {
                return null;
            }

            current = property.GetValue(current);
        }

        return current?.ToString();
    }

    /// <summary>
    /// Sets a configuration value by key path (e.g., "Database:Provider").
    /// </summary>
    public bool SetValue(string key, string value)
    {
        var settings = LoadSettings();
        var parts = key.Split(':');

        if (parts.Length == 0)
        {
            return false;
        }

        object? current = settings;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var property = current?.GetType().GetProperty(parts[i]);
            if (property == null)
            {
                return false;
            }

            current = property.GetValue(current);
        }

        if (current == null)
        {
            return false;
        }

        var finalProperty = current.GetType().GetProperty(parts[^1]);
        if (finalProperty == null)
        {
            return false;
        }

        // Convert value to appropriate type
        try
        {
            object? convertedValue;
            if (finalProperty.PropertyType == typeof(string))
            {
                convertedValue = value;
            }
            else if (finalProperty.PropertyType == typeof(int))
            {
                convertedValue = int.Parse(value);
            }
            else if (finalProperty.PropertyType == typeof(double))
            {
                convertedValue = double.Parse(value);
            }
            else if (finalProperty.PropertyType == typeof(bool))
            {
                convertedValue = bool.Parse(value);
            }
            else if (finalProperty.PropertyType == typeof(string[]))
            {
                convertedValue = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else
            {
                return false;
            }

            finalProperty.SetValue(current, convertedValue);
            SaveSettings(settings);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all configuration settings as a dictionary.
    /// </summary>
    public Dictionary<string, string> GetAllSettings()
    {
        var settings = LoadSettings();
        var result = new Dictionary<string, string>();

        FlattenObject("Database", settings.Database, result);
        FlattenObject("Scraping", settings.Scraping, result);
        FlattenObject("Search", settings.Search, result);
        FlattenObject("Deactivation", settings.Deactivation, result);
        FlattenObject("Output", settings.Output, result);

        return result;
    }

    private void FlattenObject(string prefix, object obj, Dictionary<string, string> result)
    {
        var properties = obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            var key = $"{prefix}:{property.Name}";

            if (value is string[] array)
            {
                result[key] = string.Join(", ", array);
            }
            else if (value != null)
            {
                result[key] = value.ToString() ?? "";
            }
        }
    }

    /// <summary>
    /// Resets configuration to default values.
    /// </summary>
    public void ResetToDefaults()
    {
        var settings = new AppSettings();
        SaveSettings(settings);
    }

    /// <summary>
    /// Gets the configuration file path.
    /// </summary>
    public string GetConfigFilePath() => _configFilePath;
}
