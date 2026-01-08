namespace AutomatedMarketIntelligenceTool.Cli.Tests;

/// <summary>
/// Base class for tests that need to capture console output.
/// </summary>
public abstract class ConsoleTestBase : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly StringWriter _stringWriter;

    protected ConsoleTestBase()
    {
        _originalOut = Console.Out;
        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);
    }

    protected string GetConsoleOutput()
    {
        return _stringWriter.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _stringWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
