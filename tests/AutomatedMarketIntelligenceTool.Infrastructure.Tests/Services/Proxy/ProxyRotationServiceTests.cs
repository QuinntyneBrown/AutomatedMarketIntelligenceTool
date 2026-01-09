using AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Proxy;

public class ProxyRotationServiceTests
{
    private readonly Mock<ILogger<ProxyRotationService>> _loggerMock;
    private readonly ProxyRotationService _service;
    private readonly string _testProxyFilePath;

    public ProxyRotationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProxyRotationService>>();
        _service = new ProxyRotationService(_loggerMock.Object);
        _testProxyFilePath = Path.Combine(Path.GetTempPath(), $"test_proxies_{Guid.NewGuid()}.txt");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProxyRotationService(null!));
    }

    [Fact]
    public async Task LoadProxiesFromFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.LoadProxiesFromFileAsync(string.Empty));
    }

    [Fact]
    public async Task LoadProxiesFromFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_file.txt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _service.LoadProxiesFromFileAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadProxiesFromFileAsync_WithValidProxies_LoadsSuccessfully()
    {
        // Arrange
        var proxies = new[]
        {
            "http://proxy1.example.com:8080",
            "http://proxy2.example.com:8080",
            "http://proxy3.example.com:8080"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);

        try
        {
            // Act
            await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

            // Assert
            Assert.Equal(3, _service.GetAvailableProxyCount());
            Assert.True(_service.IsEnabled());
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task LoadProxiesFromFileAsync_WithCommentsAndEmptyLines_IgnoresThem()
    {
        // Arrange
        var lines = new[]
        {
            "# This is a comment",
            "http://proxy1.example.com:8080",
            "",
            "http://proxy2.example.com:8080",
            "   ",
            "# Another comment"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, lines);

        try
        {
            // Act
            await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

            // Assert
            Assert.Equal(2, _service.GetAvailableProxyCount());
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task LoadProxiesFromFileAsync_WithAuthentication_ParsesCorrectly()
    {
        // Arrange
        var proxies = new[]
        {
            "http://user:pass@proxy.example.com:8080",
            "user2:pass2@proxy2.example.com:8080"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);

        try
        {
            // Act
            await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

            // Assert
            Assert.Equal(2, _service.GetAvailableProxyCount());
            var proxy = _service.GetNextProxy();
            Assert.NotNull(proxy);
            Assert.NotNull(proxy.Username);
            Assert.NotNull(proxy.Password);
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task GetNextProxy_WithNoProxiesLoaded_ReturnsNull()
    {
        // Act
        var proxy = _service.GetNextProxy();

        // Assert
        Assert.Null(proxy);
    }

    [Fact]
    public async Task GetNextProxy_WithProxiesLoaded_ReturnsProxy()
    {
        // Arrange
        var proxies = new[] { "http://proxy.example.com:8080" };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act
            var proxy = _service.GetNextProxy();

            // Assert
            Assert.NotNull(proxy);
            Assert.Contains("proxy.example.com", proxy.Server);
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task GetNextProxy_RotatesThroughProxies()
    {
        // Arrange
        var proxies = new[]
        {
            "http://proxy1.example.com:8080",
            "http://proxy2.example.com:8080",
            "http://proxy3.example.com:8080"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act
            var proxy1 = _service.GetNextProxy();
            var proxy2 = _service.GetNextProxy();
            var proxy3 = _service.GetNextProxy();
            var proxy4 = _service.GetNextProxy(); // Should rotate back to first

            // Assert
            Assert.NotNull(proxy1);
            Assert.NotNull(proxy2);
            Assert.NotNull(proxy3);
            Assert.NotNull(proxy4);
            
            // Verify rotation
            Assert.NotEqual(proxy1.Server, proxy2.Server);
            Assert.NotEqual(proxy2.Server, proxy3.Server);
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task MarkProxyAsFailed_RemovesProxyFromRotation()
    {
        // Arrange
        var proxies = new[]
        {
            "http://proxy1.example.com:8080",
            "http://proxy2.example.com:8080"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act
            var proxy1 = _service.GetNextProxy();
            Assert.NotNull(proxy1);
            
            _service.MarkProxyAsFailed(proxy1.Server);

            // Assert
            Assert.Equal(1, _service.GetAvailableProxyCount());
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task MarkProxyAsFailed_WhenAllProxiesFail_GetNextProxyReturnsNull()
    {
        // Arrange
        var proxies = new[] { "http://proxy.example.com:8080" };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act
            var proxy = _service.GetNextProxy();
            Assert.NotNull(proxy);
            
            _service.MarkProxyAsFailed(proxy.Server);
            var nextProxy = _service.GetNextProxy();

            // Assert
            Assert.Null(nextProxy);
            Assert.Equal(0, _service.GetAvailableProxyCount());
            Assert.False(_service.IsEnabled());
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public void IsEnabled_WithNoProxies_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_service.IsEnabled());
    }

    [Fact]
    public async Task IsEnabled_WithProxiesLoaded_ReturnsTrue()
    {
        // Arrange
        var proxies = new[] { "http://proxy.example.com:8080" };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act & Assert
            Assert.True(_service.IsEnabled());
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }

    [Fact]
    public async Task GetAvailableProxyCount_ReturnsCorrectCount()
    {
        // Arrange
        var proxies = new[]
        {
            "http://proxy1.example.com:8080",
            "http://proxy2.example.com:8080",
            "http://proxy3.example.com:8080"
        };
        await File.WriteAllLinesAsync(_testProxyFilePath, proxies);
        await _service.LoadProxiesFromFileAsync(_testProxyFilePath);

        try
        {
            // Act
            var count = _service.GetAvailableProxyCount();

            // Assert
            Assert.Equal(3, count);
        }
        finally
        {
            File.Delete(_testProxyFilePath);
        }
    }
}
