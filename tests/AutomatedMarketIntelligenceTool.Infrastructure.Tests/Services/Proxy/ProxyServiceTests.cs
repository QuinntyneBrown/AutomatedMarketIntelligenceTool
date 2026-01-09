using AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;
using Serilog;
using Serilog.Core;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Proxy;

public class ProxyServiceTests
{
    private readonly ILogger _logger;

    public ProxyServiceTests()
    {
        _logger = Logger.None;
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProxyService(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new ProxyConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProxyService(config, null!));
    }

    [Fact]
    public void GetConfiguration_ShouldReturnConfiguration()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetConfiguration();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(config.Enabled, result.Enabled);
        Assert.Equal(config.Address, result.Address);
    }

    [Fact]
    public void IsEnabled_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = false,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.IsEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsEnabled_WhenEnabledWithAddress_ShouldReturnTrue()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.IsEnabled();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEnabled_WhenEnabledWithoutAddress_ShouldReturnFalse()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = null
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.IsEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetPlaywrightProxy_WhenDisabled_ShouldReturnNull()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = false,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetPlaywrightProxy();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlaywrightProxy_WhenEnabledWithoutAddress_ShouldReturnNull()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = ""
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetPlaywrightProxy();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlaywrightProxy_WhenEnabledWithAddress_ShouldReturnProxy()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetPlaywrightProxy();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://proxy:8080", result.Server);
        Assert.Null(result.Username);
        Assert.Null(result.Password);
    }

    [Fact]
    public void GetPlaywrightProxy_WhenEnabledWithAuthentication_ShouldReturnProxyWithCredentials()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "http://proxy:8080",
            Username = "testuser",
            Password = "testpass"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetPlaywrightProxy();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://proxy:8080", result.Server);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("testpass", result.Password);
    }

    [Fact]
    public void GetPlaywrightProxy_WhenEnabledWithSocks5_ShouldReturnProxy()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "socks5://proxy:1080",
            Type = ProxyType.Socks5
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result = service.GetPlaywrightProxy();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("socks5://proxy:1080", result.Server);
    }

    [Fact]
    public void GetPlaywrightProxy_CalledMultipleTimes_ShouldReturnSameConfiguration()
    {
        // Arrange
        var config = new ProxyConfiguration
        {
            Enabled = true,
            Address = "http://proxy:8080"
        };
        var service = new ProxyService(config, _logger);

        // Act
        var result1 = service.GetPlaywrightProxy();
        var result2 = service.GetPlaywrightProxy();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Server, result2.Server);
    }
}
