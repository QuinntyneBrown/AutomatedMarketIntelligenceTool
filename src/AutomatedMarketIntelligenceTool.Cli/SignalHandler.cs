using System.Runtime.InteropServices;
using Serilog;
using Spectre.Console;

namespace AutomatedMarketIntelligenceTool.Cli;

/// <summary>
/// Handles system signals (SIGINT, SIGTERM) for graceful application termination.
/// </summary>
public sealed class SignalHandler : IDisposable
{
    private static SignalHandler? _instance;
    private static readonly object Lock = new();

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Func<Task>> _cleanupActions;
    private bool _isShuttingDown;
    private bool _disposed;

    private SignalHandler()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _cleanupActions = new List<Func<Task>>();

        // Register console cancel key press (Ctrl+C)
        Console.CancelKeyPress += OnCancelKeyPress;

        // Register process exit (SIGTERM on Unix, process termination on Windows)
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        // Register SIGTERM on Unix systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RegisterUnixSignals();
        }
    }

    /// <summary>
    /// Gets the singleton instance of the signal handler.
    /// </summary>
    public static SignalHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new SignalHandler();
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// Gets a cancellation token that is triggered when a shutdown signal is received.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>
    /// Gets a value indicating whether shutdown is in progress.
    /// </summary>
    public bool IsShuttingDown => _isShuttingDown;

    /// <summary>
    /// Registers a cleanup action to be executed during graceful shutdown.
    /// </summary>
    /// <param name="cleanupAction">The async cleanup action to execute.</param>
    public void RegisterCleanupAction(Func<Task> cleanupAction)
    {
        ArgumentNullException.ThrowIfNull(cleanupAction);

        lock (_cleanupActions)
        {
            _cleanupActions.Add(cleanupAction);
        }
    }

    /// <summary>
    /// Registers a synchronous cleanup action to be executed during graceful shutdown.
    /// </summary>
    /// <param name="cleanupAction">The cleanup action to execute.</param>
    public void RegisterCleanupAction(Action cleanupAction)
    {
        ArgumentNullException.ThrowIfNull(cleanupAction);

        RegisterCleanupAction(() =>
        {
            cleanupAction();
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Initiates graceful shutdown.
    /// </summary>
    public async Task InitiateShutdownAsync()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;

        Log.Information("Shutdown signal received. Initiating graceful shutdown...");

        try
        {
            // Cancel any ongoing operations
            await _cancellationTokenSource.CancelAsync();

            // Execute cleanup actions in reverse order (LIFO)
            List<Func<Task>> actionsToExecute;
            lock (_cleanupActions)
            {
                actionsToExecute = new List<Func<Task>>(_cleanupActions);
                actionsToExecute.Reverse();
            }

            var timeout = TimeSpan.FromSeconds(10);
            using var timeoutCts = new CancellationTokenSource(timeout);

            foreach (var action in actionsToExecute)
            {
                try
                {
                    var task = action();
                    await task.WaitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Cleanup action timed out");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during cleanup action");
                }
            }

            Log.Information("Graceful shutdown completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during graceful shutdown");
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // Prevent immediate termination
        e.Cancel = true;

        if (_isShuttingDown)
        {
            // Second Ctrl+C - force immediate exit
            AnsiConsole.MarkupLine("\n[yellow]Forced shutdown initiated...[/]");
            Environment.Exit(130); // 128 + SIGINT
        }

        AnsiConsole.MarkupLine("\n[yellow]Shutdown requested. Press Ctrl+C again to force quit.[/]");

        // Initiate graceful shutdown (fire and forget for console handler)
        _ = Task.Run(async () =>
        {
            await InitiateShutdownAsync();
        });
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        // Synchronous shutdown on process exit
        InitiateShutdownAsync().GetAwaiter().GetResult();
    }

    private void RegisterUnixSignals()
    {
        // Handle SIGTERM (kill -15)
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
        {
            context.Cancel = true;
            Log.Information("SIGTERM received");
            _ = Task.Run(async () => await InitiateShutdownAsync());
        });

        // Handle SIGQUIT (Ctrl+\)
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, context =>
        {
            context.Cancel = true;
            Log.Information("SIGQUIT received");
            _ = Task.Run(async () => await InitiateShutdownAsync());
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        _cancellationTokenSource.Dispose();

        _disposed = true;
    }
}
