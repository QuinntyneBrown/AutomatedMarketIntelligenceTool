using AutomatedMarketIntelligenceTool.Cli.Interactive;
using AutomatedMarketIntelligenceTool.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AutomatedMarketIntelligenceTool.Cli.Tests.Interactive;

public class InteractiveModeTests
{
    private readonly Mock<IAutoCompleteService> _autoCompleteServiceMock;
    private readonly Mock<ILogger<InteractiveMode>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public InteractiveModeTests()
    {
        _autoCompleteServiceMock = new Mock<IAutoCompleteService>();
        _loggerMock = new Mock<ILogger<InteractiveMode>>();
    }

    [Fact]
    public void Constructor_WithNullAutoCompleteService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new InteractiveMode(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("autoCompleteService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new InteractiveMode(_autoCompleteServiceMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var mode = new InteractiveMode(_autoCompleteServiceMock.Object, _loggerMock.Object);

        // Assert
        mode.Should().NotBeNull();
    }

    [Fact]
    public void ClearPreviousSettings_ClearsStoredSettings()
    {
        // Act - should not throw
        var act = () => InteractiveMode.ClearPreviousSettings();

        // Assert
        act.Should().NotThrow();
    }
}

public class MakeModelAutoCompleteTests
{
    private readonly Mock<IAutoCompleteService> _autoCompleteServiceMock;
    private readonly MakeModelAutoComplete _autoComplete;
    private readonly Guid _tenantId = Guid.NewGuid();

    public MakeModelAutoCompleteTests()
    {
        _autoCompleteServiceMock = new Mock<IAutoCompleteService>();
        _autoComplete = new MakeModelAutoComplete(_autoCompleteServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullAutoCompleteService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MakeModelAutoComplete(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("autoCompleteService");
    }

    [Fact]
    public void Constructor_WithValidService_CreatesInstance()
    {
        // Act
        var autoComplete = new MakeModelAutoComplete(_autoCompleteServiceMock.Object);

        // Assert
        autoComplete.Should().NotBeNull();
    }
}
