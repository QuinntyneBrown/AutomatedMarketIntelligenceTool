using System.Text;
using AutomatedMarketIntelligenceTool.Core.Utilities;

namespace AutomatedMarketIntelligenceTool.Cli.Formatters;

/// <summary>
/// Formats output as CSV.
/// </summary>
public class CsvFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public void Format<T>(IEnumerable<T> data) where T : class
    {
        var dataList = data.ToList();
        
        if (!dataList.Any())
        {
            return;
        }

        var properties = typeof(T).GetProperties();
        var csv = new StringBuilder();
        
        // Add header row
        csv.AppendLine(string.Join(",", properties.Select(p => StringUtilities.EscapeCsvValue(p.Name))));
        
        // Add data rows
        foreach (var item in dataList)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return StringUtilities.EscapeCsvValue(value?.ToString() ?? string.Empty);
            });
            
            csv.AppendLine(string.Join(",", values));
        }
        
        Console.Write(csv.ToString());
    }
}
