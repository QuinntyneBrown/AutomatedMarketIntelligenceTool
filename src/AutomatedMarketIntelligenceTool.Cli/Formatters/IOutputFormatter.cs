namespace AutomatedMarketIntelligenceTool.Cli.Formatters;

/// <summary>
/// Interface for output formatters that display search results in different formats.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Formats and displays the given data.
    /// </summary>
    /// <typeparam name="T">The type of data to format.</typeparam>
    /// <param name="data">The data to format and display.</param>
    void Format<T>(IEnumerable<T> data) where T : class;
}
