using AutomatedMarketIntelligenceTool.Cli;
using FluentAssertions;

namespace AutomatedMarketIntelligenceTool.Cli.Tests;

public class SignalHandlerTests
{
    [Fact]
    public void Instance_ShouldReturnSingleton()
    {
        // Arrange & Act
        var instance1 = SignalHandler.Instance;
        var instance2 = SignalHandler.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void CancellationToken_ShouldNotBeCancelledInitially()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Assert
        handler.CancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void IsShuttingDown_ShouldBeFalseInitially()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Assert
        handler.IsShuttingDown.Should().BeFalse();
    }

    [Fact]
    public void RegisterCleanupAction_ShouldNotThrow()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Act
        var act = () => handler.RegisterCleanupAction(() => Task.CompletedTask);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterCleanupAction_Sync_ShouldNotThrow()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Act
        var act = () => handler.RegisterCleanupAction(() => { });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterCleanupAction_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Act
        var act = () => handler.RegisterCleanupAction((Func<Task>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterCleanupAction_WithNullSyncAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = SignalHandler.Instance;

        // Act
        var act = () => handler.RegisterCleanupAction((Action)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Instance_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new SignalHandler[10];
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => instances[index] = SignalHandler.Instance));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        instances.Should().AllSatisfy(instance => instance.Should().BeSameAs(instances[0]));
    }
}
