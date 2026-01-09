using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.UserAgent;

/// <summary>
/// Unit tests for UserAgentPool.
/// Phase 5 - REQ-WS-008: User Agent Management with mobile support
/// </summary>
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

    // Phase 5 - AC-008.5: Mobile User-Agents tests
    [Fact]
    public void GetMobileUserAgentsForBrowser_WithChromium_ShouldReturnMobileChromiumAgents()
    {
        // Act
        var result = UserAgentPool.GetMobileUserAgentsForBrowser("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => 
            Assert.True(agent.Contains("Mobile") || agent.Contains("Android") || agent.Contains("iPhone"),
                $"Expected mobile user agent but got: {agent}"));
    }

    [Fact]
    public void GetMobileUserAgentsForBrowser_WithFirefox_ShouldReturnMobileFirefoxAgents()
    {
        // Act
        var result = UserAgentPool.GetMobileUserAgentsForBrowser("firefox");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => Assert.Contains("Mobile", agent));
    }

    [Fact]
    public void GetMobileUserAgentsForBrowser_WithWebKit_ShouldReturnMobileWebKitAgents()
    {
        // Act
        var result = UserAgentPool.GetMobileUserAgentsForBrowser("webkit");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, agent => 
            Assert.True(agent.Contains("iPhone") || agent.Contains("iPad"),
                $"Expected mobile webkit user agent but got: {agent}"));
    }

    [Fact]
    public void GetDefaultMobileUserAgent_WithChromium_ShouldReturnMobileAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultMobileUserAgent("chromium");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Contains("Mobile") || result.Contains("Android") || result.Contains("iPhone"));
    }

    [Fact]
    public void GetDefaultMobileUserAgent_WithFirefox_ShouldReturnMobileAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultMobileUserAgent("firefox");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Mobile", result);
    }

    [Fact]
    public void GetDefaultMobileUserAgent_WithWebKit_ShouldReturnMobileAgent()
    {
        // Act
        var result = UserAgentPool.GetDefaultMobileUserAgent("webkit");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Contains("iPhone") || result.Contains("iPad"));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36", true)]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1", true)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1", true)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", false)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", false)]
    public void IsMobileUserAgent_ShouldCorrectlyIdentifyMobileAgents(string userAgent, bool expectedIsMobile)
    {
        // Act
        var result = UserAgentPool.IsMobileUserAgent(userAgent);

        // Assert
        Assert.Equal(expectedIsMobile, result);
    }

    [Fact]
    public void IsMobileUserAgent_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = UserAgentPool.IsMobileUserAgent(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMobileUserAgent_WithEmptyString_ShouldReturnFalse()
    {
        // Act
        var result = UserAgentPool.IsMobileUserAgent("");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    [InlineData("webkit")]
    public void GetMobileUserAgentsForBrowser_ShouldReturnMultipleAgents(string browserType)
    {
        // Act
        var result = UserAgentPool.GetMobileUserAgentsForBrowser(browserType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 3, "Should have at least 3 mobile user agents per browser");
    }

    [Fact]
    public void MobileAndDesktopAgents_ShouldBeDifferent()
    {
        // Act
        var desktopChromium = UserAgentPool.GetDefaultUserAgent("chromium");
        var mobileChromium = UserAgentPool.GetDefaultMobileUserAgent("chromium");

        // Assert
        Assert.NotEqual(desktopChromium, mobileChromium);
        Assert.False(UserAgentPool.IsMobileUserAgent(desktopChromium));
        Assert.True(UserAgentPool.IsMobileUserAgent(mobileChromium));
    }
}
