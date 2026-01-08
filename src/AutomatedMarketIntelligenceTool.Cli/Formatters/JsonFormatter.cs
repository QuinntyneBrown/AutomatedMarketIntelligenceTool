using System.Text.Json;

namespace AutomatedMarketIntelligenceTool.Cli.Formatters;

/// <summary>
/// Formats output as JSON.
/// </summary>
public class JsonFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public void Format<T>(IEnumerable<T> data) where T : class
    {
        var json = JsonSerializer.Serialize(data, Options);
        Console.WriteLine(json);
    }
}
