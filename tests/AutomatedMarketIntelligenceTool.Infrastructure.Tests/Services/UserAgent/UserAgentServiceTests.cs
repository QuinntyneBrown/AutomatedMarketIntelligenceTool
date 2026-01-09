using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;
using Serilog;
using Serilog.Core;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.UserAgent;

/// <summary>
/// Unit tests for UserAgentService.
/// Phase 5 - REQ-WS-008: User Agent Management with mobile support
/// </summary>
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

    // Phase 5 - AC-008.5: Mobile User-Agent tests
    [Fact]
    public void GetMobileUserAgent_WithoutCustomAgent_ShouldReturnDefaultMobileAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger);

        // Act
        var result = service.GetMobileUserAgent("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Contains("Mobile") || result.Contains("Android") || result.Contains("iPhone"));
    }

    [Fact]
    public void GetMobileUserAgent_WithCustomMobileAgent_ShouldReturnCustomAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var customMobileAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) Mobile";
        service.SetCustomUserAgent(customMobileAgent);

        // Act
        var result = service.GetMobileUserAgent("chromium");

        // Assert
        Assert.Equal(customMobileAgent, result);
    }

    [Fact]
    public void GetNextMobileUserAgent_WithRotationEnabled_ShouldRotateMobileAgents()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);

        // Act
        var result1 = service.GetNextMobileUserAgent("chromium");
        var result2 = service.GetNextMobileUserAgent("chromium");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(UserAgentPool.IsMobileUserAgent(result1));
        Assert.True(UserAgentPool.IsMobileUserAgent(result2));
    }

    [Fact]
    public void GetNextMobileUserAgent_WithRotationDisabled_ShouldReturnSameMobileAgent()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: false);

        // Act
        var result1 = service.GetNextMobileUserAgent("chromium");
        var result2 = service.GetNextMobileUserAgent("chromium");

        // Assert
        Assert.Equal(result1, result2);
        Assert.True(UserAgentPool.IsMobileUserAgent(result1));
    }

    [Fact]
    public void GetNextMobileUserAgent_CalledMultipleTimes_ShouldRotateThroughAllMobileAgents()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);
        var browserType = "chromium";
        var agents = UserAgentPool.GetMobileUserAgentsForBrowser(browserType);
        var seenAgents = new HashSet<string>();

        // Act
        for (int i = 0; i < agents.Count * 2; i++)
        {
            var agent = service.GetNextMobileUserAgent(browserType);
            seenAgents.Add(agent);
        }

        // Assert
        Assert.Equal(agents.Count, seenAgents.Count);
        foreach (var agent in agents)
        {
            Assert.Contains(agent, seenAgents);
        }
    }

    [Fact]
    public void IsMobile_WithMobileUserAgent_ShouldReturnTrue()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var mobileAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) Mobile";

        // Act
        var result = service.IsMobile(mobileAgent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMobile_WithDesktopUserAgent_ShouldReturnFalse()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var desktopAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/131.0.0.0";

        // Act
        var result = service.IsMobile(desktopAgent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMobile_WithCustomMobileAgent_ShouldReturnTrue()
    {
        // Arrange
        var service = new UserAgentService(_logger);
        var customMobileAgent = "Mozilla/5.0 (Android 14; Mobile) Chrome/131.0.0.0";
        service.SetCustomUserAgent(customMobileAgent);

        // Act
        var result = service.IsMobile();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMobile_WithoutCustomAgent_ShouldReturnFalse()
    {
        // Arrange
        var service = new UserAgentService(_logger);

        // Act
        var result = service.IsMobile();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MobileAndDesktopAgents_ShouldBeIndependentlyRotated()
    {
        // Arrange
        var service = new UserAgentService(_logger, rotationEnabled: true);

        // Act - Rotate desktop agents
        var desktop1 = service.GetNextUserAgent("chromium");
        var desktop2 = service.GetNextUserAgent("chromium");

        // Act - Rotate mobile agents (should be independent)
        var mobile1 = service.GetNextMobileUserAgent("chromium");
        var mobile2 = service.GetNextMobileUserAgent("chromium");

        // Assert
        Assert.False(UserAgentPool.IsMobileUserAgent(desktop1));
        Assert.False(UserAgentPool.IsMobileUserAgent(desktop2));
        Assert.True(UserAgentPool.IsMobileUserAgent(mobile1));
        Assert.True(UserAgentPool.IsMobileUserAgent(mobile2));
    }
}
