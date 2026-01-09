using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.E2E;

/// <summary>
/// End-to-end tests that test the CLI as a subprocess.
/// These tests verify that the CLI application works correctly when invoked from the command line.
/// </summary>
public class CliEndToEndTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _originalDirectory;

    public CliEndToEndTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"cli-e2e-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        Directory.SetCurrentDirectory(_testDirectory);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public async Task Help_ShouldShowCommands()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("search");
        output.Should().Contain("scrape");
        output.Should().Contain("list");
        output.Should().Contain("export");
        output.Should().Contain("config");
        output.Should().Contain("profile");
        output.Should().Contain("status");
        output.Should().Contain("backup");
        output.Should().Contain("restore");
        output.Should().Contain("completion");
    }

    [Fact]
    public async Task Version_ShouldShowVersion()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("--version");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("3.0.0");
    }

    [Fact]
    public async Task Completion_Bash_ShouldGenerateScript()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("completion", "--shell", "bash");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("_car_search_completions");
        output.Should().Contain("complete -F");
        output.Should().Contain("search");
        output.Should().Contain("scrape");
    }

    [Fact]
    public async Task Completion_Zsh_ShouldGenerateScript()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("completion", "--shell", "zsh");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("#compdef car-search");
        output.Should().Contain("_car_search");
        output.Should().Contain("Search for car listings");
    }

    [Fact]
    public async Task Completion_PowerShell_ShouldGenerateScript()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("completion", "--shell", "powershell");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Register-ArgumentCompleter");
        output.Should().Contain("CompletionResult");
    }

    [Fact]
    public async Task Completion_Fish_ShouldGenerateScript()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("completion", "--shell", "fish");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("complete -c car-search");
        output.Should().Contain("__fish_use_subcommand");
    }

    [Fact]
    public async Task Completion_ToFile_ShouldWriteFile()
    {
        // Arrange
        var outputFile = Path.Combine(_testDirectory, "completion.sh");

        // Act
        var (exitCode, output, _) = await RunCliAsync("completion", "--shell", "bash", "--output", outputFile);

        // Assert
        exitCode.Should().Be(0);
        File.Exists(outputFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputFile);
        content.Should().Contain("_car_search_completions");
    }

    [Fact]
    public async Task Completion_InvalidShell_ShouldReturnError()
    {
        // Act
        var (exitCode, _, error) = await RunCliAsync("completion", "--shell", "invalid");

        // Assert
        exitCode.Should().Be(ExitCodes.ValidationError);
    }

    [Fact]
    public async Task Config_List_ShouldWork()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("config", "list");

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Config_Path_ShouldShowPath()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("config", "path");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("config");
    }

    [Fact]
    public async Task Backup_List_ShouldWork()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("backup", "--list");

        // Assert
        // Should succeed even if no backups exist
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Search_WithoutTenant_ShouldShowHelp()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("search", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("--tenant");
        output.Should().Contain("--make");
        output.Should().Contain("--model");
    }

    [Fact]
    public async Task Profile_WithoutAction_ShouldShowHelp()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("profile", "--help");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("save");
        output.Should().Contain("load");
        output.Should().Contain("list");
        output.Should().Contain("delete");
    }

    [Fact]
    public async Task Restore_WithoutFile_ShouldShowError()
    {
        // Act
        var (exitCode, _, _) = await RunCliAsync("restore");

        // Assert
        // Should fail because no backup file is specified
        exitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Import_WithoutFile_ShouldShowError()
    {
        // Act
        var (exitCode, _, _) = await RunCliAsync("import");

        // Assert
        // Should fail because no file is specified
        exitCode.Should().NotBe(0);
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunCliAsync(params string[] args)
    {
        var projectDir = FindProjectDirectory();
        var cliProjectPath = Path.Combine(projectDir, "src", "AutomatedMarketIntelligenceTool.Cli");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{cliProjectPath}\" --no-build -- {string.Join(" ", args.Select(EscapeArgument))}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdout.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Timeout after 60 seconds
        var completed = await Task.Run(() => process.WaitForExit(60000));

        if (!completed)
        {
            process.Kill();
            throw new TimeoutException("CLI process timed out after 60 seconds");
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static string FindProjectDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Look for the solution file to find the project root
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir, "AutomatedMarketIntelligenceTool.sln")))
            {
                return currentDir;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // Fallback: check common paths based on OS
        var possiblePaths = new List<string>
        {
            Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..")
        };

        // Add OS-specific paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            possiblePaths.Add(@"c:\projects\AutomatedMarketIntelligenceTool");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            possiblePaths.Add("/home/user/AutomatedMarketIntelligenceTool");
            possiblePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AutomatedMarketIntelligenceTool"));
        }

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "AutomatedMarketIntelligenceTool.sln")))
            {
                return Path.GetFullPath(path);
            }
        }

        throw new InvalidOperationException("Could not find project directory");
    }

    private static string EscapeArgument(string arg)
    {
        if (arg.Contains(' '))
        {
            return $"\"{arg}\"";
        }

        return arg;
    }
}
