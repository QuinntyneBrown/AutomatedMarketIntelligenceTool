using AutomatedMarketIntelligenceTool.Cli;
using FluentAssertions;
using Serilog.Events;

namespace AutomatedMarketIntelligenceTool.Cli.Tests;

public class VerbosityLevelTests
{
    [Fact]
    public void VerbosityLevel_HasCorrectValues()
    {
        // Assert
        ((int)VerbosityLevel.Quiet).Should().Be(-1);
        ((int)VerbosityLevel.Normal).Should().Be(0);
        ((int)VerbosityLevel.Verbose).Should().Be(1);
        ((int)VerbosityLevel.Debug).Should().Be(2);
        ((int)VerbosityLevel.Trace).Should().Be(3);
    }

    [Theory]
    [InlineData(new string[] { }, VerbosityLevel.Normal)]
    [InlineData(new[] { "search", "-m", "Toyota" }, VerbosityLevel.Normal)]
    [InlineData(new[] { "-v" }, VerbosityLevel.Verbose)]
    [InlineData(new[] { "--verbose" }, VerbosityLevel.Verbose)]
    [InlineData(new[] { "search", "-v", "-m", "Toyota" }, VerbosityLevel.Verbose)]
    [InlineData(new[] { "-vv" }, VerbosityLevel.Debug)]
    [InlineData(new[] { "-v", "-v" }, VerbosityLevel.Debug)]
    [InlineData(new[] { "-vvv" }, VerbosityLevel.Trace)]
    [InlineData(new[] { "-v", "-v", "-v" }, VerbosityLevel.Trace)]
    [InlineData(new[] { "-vvvv" }, VerbosityLevel.Trace)] // More than 3 v's still maps to Trace
    [InlineData(new[] { "-q" }, VerbosityLevel.Quiet)]
    [InlineData(new[] { "--quiet" }, VerbosityLevel.Quiet)]
    [InlineData(new[] { "search", "-q", "-m", "Toyota" }, VerbosityLevel.Quiet)]
    public void ParseFromArgs_ReturnsCorrectLevel(string[] args, VerbosityLevel expected)
    {
        // Act
        var result = VerbosityHelper.ParseFromArgs(args);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(VerbosityLevel.Quiet, LogEventLevel.Error)]
    [InlineData(VerbosityLevel.Normal, LogEventLevel.Information)]
    [InlineData(VerbosityLevel.Verbose, LogEventLevel.Information)]
    [InlineData(VerbosityLevel.Debug, LogEventLevel.Debug)]
    [InlineData(VerbosityLevel.Trace, LogEventLevel.Verbose)]
    public void ToLogEventLevel_ReturnsCorrectLevel(VerbosityLevel input, LogEventLevel expected)
    {
        // Act
        var result = VerbosityHelper.ToLogEventLevel(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(VerbosityLevel.Quiet, false)]
    [InlineData(VerbosityLevel.Normal, false)]
    [InlineData(VerbosityLevel.Verbose, true)]
    [InlineData(VerbosityLevel.Debug, true)]
    [InlineData(VerbosityLevel.Trace, true)]
    public void ShouldShowVerbose_ReturnsCorrectValue(VerbosityLevel level, bool expected)
    {
        // Act
        var result = VerbosityHelper.ShouldShowVerbose(level);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(VerbosityLevel.Quiet, false)]
    [InlineData(VerbosityLevel.Normal, false)]
    [InlineData(VerbosityLevel.Verbose, false)]
    [InlineData(VerbosityLevel.Debug, true)]
    [InlineData(VerbosityLevel.Trace, true)]
    public void ShouldShowDebug_ReturnsCorrectValue(VerbosityLevel level, bool expected)
    {
        // Act
        var result = VerbosityHelper.ShouldShowDebug(level);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(VerbosityLevel.Quiet, false)]
    [InlineData(VerbosityLevel.Normal, false)]
    [InlineData(VerbosityLevel.Verbose, false)]
    [InlineData(VerbosityLevel.Debug, false)]
    [InlineData(VerbosityLevel.Trace, true)]
    public void ShouldShowTrace_ReturnsCorrectValue(VerbosityLevel level, bool expected)
    {
        // Act
        var result = VerbosityHelper.ShouldShowTrace(level);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseFromArgs_QuietTakesPrecedenceOverVerbose()
    {
        // Arrange - both quiet and verbose flags
        var args = new[] { "-q", "-v" };

        // Act
        var result = VerbosityHelper.ParseFromArgs(args);

        // Assert - quiet should take precedence
        result.Should().Be(VerbosityLevel.Quiet);
    }

    [Fact]
    public void ParseFromArgs_HandlesEmptyArgs()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = VerbosityHelper.ParseFromArgs(args);

        // Assert
        result.Should().Be(VerbosityLevel.Normal);
    }

    [Fact]
    public void ParseFromArgs_IgnoresUnrelatedArgs()
    {
        // Arrange
        var args = new[] { "search", "--make", "Toyota", "--model", "Camry" };

        // Act
        var result = VerbosityHelper.ParseFromArgs(args);

        // Assert
        result.Should().Be(VerbosityLevel.Normal);
    }
}
