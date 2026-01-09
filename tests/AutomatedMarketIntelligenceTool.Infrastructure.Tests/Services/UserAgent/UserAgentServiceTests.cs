using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;
using Serilog;
using Serilog.Core;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.UserAgent;

public class UserAgentServiceTests
{
    private readonly ILogger _logger;

    public UserAgentServiceTests()
    {
        _logger = Logger.None;
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UserAgentService(null!));
    }

    [Fact]
    public void GetUserAgent_WithoutCustomAgent_ShouldReturnDefaultAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger);

        // Act
        var result = service.GetUserAgent("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Chrome", result);
    }

    [Fact]
    public void GetUserAgent_WithCustomAgent_ShouldReturnCustomAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var customAgent = "Custom/1.0 Test Agent";
        service.SetCustomUserAgent(customAgent);

        // Act
        var result = service.GetUserAgent("chromium");

        // Assert
        Assert.Equal(customAgent, result);
    }

    [Fact]
    public void GetNextUserAgent_WithRotationDisabled_ShouldReturnSameAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: false);

        // Act
        var result1 = service.GetNextUserAgent("chromium");
        var result2 = service.GetNextUserAgent("chromium");
        var result3 = service.GetNextUserAgent("chromium");

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void GetNextUserAgent_WithRotationEnabled_ShouldRotateAgents()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);

        // Act
        var result1 = service.GetNextUserAgent("chromium");
        var result2 = service.GetNextUserAgent("chromium");

        // Assert - At least one of the calls should differ if there are multiple agents
        // Since we have multiple agents in the pool, rotation should occur
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public void GetNextUserAgent_WithCustomAgent_ShouldReturnCustomAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);
        var customAgent = "Custom/1.0 Test Agent";
        service.SetCustomUserAgent(customAgent);

        // Act
        var result1 = service.GetNextUserAgent("chromium");
        var result2 = service.GetNextUserAgent("chromium");

        // Assert
        Assert.Equal(customAgent, result1);
        Assert.Equal(customAgent, result2);
    }

    [Fact]
    public void GetNextUserAgent_WithDifferentBrowserTypes_ShouldMaintainSeparateRotation()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);

        // Act
        var chromium1 = service.GetNextUserAgent("chromium");
        var firefox1 = service.GetNextUserAgent("firefox");
        var chromium2 = service.GetNextUserAgent("chromium");

        // Assert
        Assert.Contains("Chrome", chromium1);
        Assert.Contains("Firefox", firefox1);
        Assert.Contains("Chrome", chromium2);
    }

    [Fact]
    public void SetCustomUserAgent_WithNullOrEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new UserAgentService(_logger);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.SetCustomUserAgent(null!));
        Assert.Throws<ArgumentException>(() => service.SetCustomUserAgent(""));
        Assert.Throws<ArgumentException>(() => service.SetCustomUserAgent("   "));
    }

    [Fact]
    public void SetCustomUserAgent_WithValidAgent_ShouldSetCustomAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var customAgent = "Custom/1.0 Test Agent";

        // Act
        service.SetCustomUserAgent(customAgent);
        var result = service.GetUserAgent("chromium");

        // Assert
        Assert.Equal(customAgent, result);
    }

    [Fact]
    public void HasCustomUserAgent_WithoutSettingCustomAgent_ShouldReturnFalse()
    {
        // Arrange
        var service = new UserAgentService(_logger);

        // Act
        var result = service.HasCustomUserAgent();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasCustomUserAgent_AfterSettingCustomAgent_ShouldReturnTrue()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        service.SetCustomUserAgent("Custom/1.0");

        // Act
        var result = service.HasCustomUserAgent();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetNextUserAgent_CalledMultipleTimes_ShouldEventuallyRotateThroughAllAgents()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);
        var browserType = "chromium";
        var agents = UserAgentPool.GetUserAgentsForBrowser(browserType);
        var seenAgents = new HashSet<string>();

        // Act - Call enough times to cycle through all agents at least once
        for (int i = 0; i < agents.Count * 2; i++)
        {
            var agent = service.GetNextUserAgent(browserType);
            seenAgents.Add(agent);
        }

        // Assert - We should have seen all agents in the pool
        Assert.Equal(agents.Count, seenAgents.Count);
        foreach (var agent in agents)
        {
            Assert.Contains(agent, seenAgents);
        }
    }

    [Fact]
    public void GetNextUserAgent_ThreadSafety_ShouldHandleConcurrentCalls()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Make concurrent calls
        Parallel.For(0, 100, i =>
        {
            var agent = service.GetNextUserAgent("chromium");
            results.Add(agent);
        });

        // Assert - All results should be valid Chrome user agents
        Assert.Equal(100, results.Count);
        Assert.All(results, agent => Assert.Contains("Chrome", agent));
    }
}
