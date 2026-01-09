using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.UserAgent;

public class UserAgentPoolTests
{
    [Fact]
    public void GetUserAgentsForBrowser_WithChromium_ShouldReturnChromiumAgents()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => Assert.Contains("Chrome", agent));
    }

    [Fact]
    public void GetUserAgentsForBrowser_WithFirefox_ShouldReturnFirefoxAgents()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser("firefox");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => Assert.Contains("Firefox", agent));
    }

    [Fact]
    public void GetUserAgentsForBrowser_WithWebKit_ShouldReturnWebKitAgents()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser("webkit");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => Assert.Contains("Safari", agent));
    }

    [Fact]
    public void GetUserAgentsForBrowser_WithUnknownBrowser_ShouldReturnChromiumAgents()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser("unknown");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => Assert.Contains("Chrome", agent));
    }

    [Fact]
    public void GetUserAgentsForBrowser_WithNull_ShouldReturnChromiumAgents()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser(null!);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetDefaultUserAgent_WithChromium_ShouldReturnChromiumAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultUserAgent("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Chrome", result);
    }

    [Fact]
    public void GetDefaultUserAgent_WithFirefox_ShouldReturnFirefoxAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultUserAgent("firefox");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Firefox", result);
    }

    [Fact]
    public void GetDefaultUserAgent_WithWebKit_ShouldReturnWebKitAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultUserAgent("webkit");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Safari", result);
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    [InlineData("webkit")]
    public void GetUserAgentsForBrowser_ShouldReturnMultipleAgents(string browserType)
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser(browserType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 3, "Should have at least 3 user agents per browser");
    }

    [Fact]
    public void GetUserAgentsForBrowser_ShouldReturnReadOnlyList()
    {
        // Act
        var result = UserAgentPool.GetUserAgentsForBrowser("chromium");

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<string>>(result);
    }
}
