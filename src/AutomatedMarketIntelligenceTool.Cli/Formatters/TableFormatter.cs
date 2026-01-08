using Spectre.Console;

namespace AutomatedMarketIntelligenceTool.Cli.Formatters;

/// <summary>
/// Formats output as a table using Spectre.Console.
/// </summary>
public class TableFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public void Format<T>(IEnumerable<T> data) where T : class
    {
        var dataList = data.ToList();
        
        if (!dataList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No results found.[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        
        // Get properties from the type
        var properties = typeof(T).GetProperties();
        
        // Add columns
        foreach (var prop in properties)
        {
            table.AddColumn(new TableColumn($"[bold]{prop.Name}[/]").Centered());
        }
        
        // Add rows
        foreach (var item in dataList)
        {
            var values = properties.Select(p => 
            {
                var value = p.GetValue(item);
                return value?.ToString() ?? string.Empty;
            }).ToArray();
            
            table.AddRow(values);
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[green]Total results: {dataList.Count}[/]");
    }
}
