namespace AutomatedMarketIntelligenceTool.Cli;

/// <summary>
/// Represents verbosity levels for CLI output.
/// </summary>
public enum VerbosityLevel
{
    /// <summary>
    /// Quiet mode - only errors.
    /// </summary>
    Quiet = -1,

    /// <summary>
    /// Normal output - essential information only.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Verbose output (-v) - additional details.
    /// </summary>
    Verbose = 1,

    /// <summary>
    /// Debug output (-vv) - technical details.
    /// </summary>
    Debug = 2,

    /// <summary>
    /// Trace output (-vvv) - full request/response logging.
    /// </summary>
    Trace = 3
}

/// <summary>
/// Helper for parsing and managing verbosity levels.
/// </summary>
public static class VerbosityHelper
{
    /// <summary>
    /// Parses verbosity level from command line arguments.
    /// </summary>
    public static VerbosityLevel ParseFromArgs(string[] args)
    {
        // Check for quiet mode
        if (args.Contains("-q") || args.Contains("--quiet"))
        {
            return VerbosityLevel.Quiet;
        }

        // Count -v flags
        int verboseCount = 0;

        foreach (var arg in args)
        {
            if (arg == "-v" || arg == "--verbose")
            {
                verboseCount++;
            }
            else if (arg == "-vv")
            {
                verboseCount = 2;
            }
            else if (arg == "-vvv")
            {
                verboseCount = 3;
            }
            else if (arg.StartsWith("-v") && arg.All(c => c == 'v' || c == '-'))
            {
                // Handle cases like -vvvv
                verboseCount = Math.Max(verboseCount, arg.Count(c => c == 'v'));
            }
        }

        return verboseCount switch
        {
            0 => VerbosityLevel.Normal,
            1 => VerbosityLevel.Verbose,
            2 => VerbosityLevel.Debug,
            _ => VerbosityLevel.Trace
        };
    }

    /// <summary>
    /// Converts verbosity level to Serilog log level.
    /// </summary>
    public static Serilog.Events.LogEventLevel ToLogEventLevel(VerbosityLevel level)
    {
        return level switch
        {
            VerbosityLevel.Quiet => Serilog.Events.LogEventLevel.Error,
            VerbosityLevel.Normal => Serilog.Events.LogEventLevel.Information,
            VerbosityLevel.Verbose => Serilog.Events.LogEventLevel.Information,
            VerbosityLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            VerbosityLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }

    /// <summary>
    /// Returns true if the current level should show verbose information.
    /// </summary>
    public static bool ShouldShowVerbose(VerbosityLevel level) => level >= VerbosityLevel.Verbose;

    /// <summary>
    /// Returns true if the current level should show debug information.
    /// </summary>
    public static bool ShouldShowDebug(VerbosityLevel level) => level >= VerbosityLevel.Debug;

    /// <summary>
    /// Returns true if the current level should show trace information.
    /// </summary>
    public static bool ShouldShowTrace(VerbosityLevel level) => level >= VerbosityLevel.Trace;
}
