using AutomatedMarketIntelligenceTool.Infrastructure.Services.Browser;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;
using Microsoft.Playwright;
using Moq;
using Serilog;
using Serilog.Core;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Browser;

public class BrowserContextFactoryTests
{
    private readonly ILogger _logger;
    private readonly Mock<IPlaywright> _playwrightMock;
    private readonly Mock<IProxyService> _proxyServiceMock;
    private readonly Mock<IUserAgentService> _userAgentServiceMock;
    private readonly Mock<IBrowserType> _browserTypeMock;
    private readonly Mock<IBrowser> _browserMock;
    private readonly Mock<IBrowserContext> _contextMock;

    public BrowserContextFactoryTests()
    {
        _logger = Logger.None;
        _playwrightMock = new Mock<IPlaywright>();
        _proxyServiceMock = new Mock<IProxyService>();
        _userAgentServiceMock = new Mock<IUserAgentService>();
        _browserTypeMock = new Mock<IBrowserType>();
        _browserMock = new Mock<IBrowser>();
        _contextMock = new Mock<IBrowserContext>();
    }

    [Fact]
    public void Constructor_WithNullPlaywright_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BrowserContextFactory(null!, _proxyServiceMock.Object, _userAgentServiceMock.Object, _logger));
    }

    [Fact]
    public void Constructor_WithNullProxyService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BrowserContextFactory(_playwrightMock.Object, null!, _userAgentServiceMock.Object, _logger));
    }

    [Fact]
    public void Constructor_WithNullUserAgentService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BrowserContextFactory(_playwrightMock.Object, _proxyServiceMock.Object, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BrowserContextFactory(_playwrightMock.Object, _proxyServiceMock.Object, _userAgentServiceMock.Object, null!));
    }

    [Fact]
    public async Task CreateContextAsync_WithChromium_ShouldCreateChromiumContext()
    {
        // Arrange
        SetupMocks("chromium", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("chromium");

        // Assert
        Assert.NotNull(result);
        _playwrightMock.Verify(p => p.Chromium, Times.Once);
        _userAgentServiceMock.Verify(u => u.GetNextUserAgent("chromium"), Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithFirefox_ShouldCreateFirefoxContext()
    {
        // Arrange
        SetupMocks("firefox", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("firefox");

        // Assert
        Assert.NotNull(result);
        _playwrightMock.Verify(p => p.Firefox, Times.Once);
        _userAgentServiceMock.Verify(u => u.GetNextUserAgent("firefox"), Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithWebKit_ShouldCreateWebKitContext()
    {
        // Arrange
        SetupMocks("webkit", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("webkit");

        // Assert
        Assert.NotNull(result);
        _playwrightMock.Verify(p => p.Webkit, Times.Once);
        _userAgentServiceMock.Verify(u => u.GetNextUserAgent("webkit"), Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithNullBrowserType_ShouldDefaultToChromium()
    {
        // Arrange
        SetupMocks("chromium", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync(null!);

        // Assert
        Assert.NotNull(result);
        _playwrightMock.Verify(p => p.Chromium, Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithEmptyBrowserType_ShouldDefaultToChromium()
    {
        // Arrange
        SetupMocks("chromium", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("");

        // Assert
        Assert.NotNull(result);
        _playwrightMock.Verify(p => p.Chromium, Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithProxy_ShouldApplyProxyConfiguration()
    {
        // Arrange
        var proxy = new Microsoft.Playwright.Proxy
        {
            Server = "http://proxy:8080"
        };
        SetupMocks("chromium", proxy);
        
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("chromium");

        // Assert
        Assert.NotNull(result);
        _proxyServiceMock.Verify(p => p.GetPlaywrightProxy(), Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithoutProxy_ShouldNotApplyProxy()
    {
        // Arrange
        SetupMocks("chromium", null);
        
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("chromium");

        // Assert
        Assert.NotNull(result);
        _proxyServiceMock.Verify(p => p.GetPlaywrightProxy(), Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithHeadlessFalse_ShouldLaunchHeadedBrowser()
    {
        // Arrange
        SetupMocks("chromium", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("chromium", headless: false);

        // Assert
        Assert.NotNull(result);
        _browserTypeMock.Verify(
            bt => bt.LaunchAsync(It.Is<BrowserTypeLaunchOptions>(o => o.Headless == false)),
            Times.Once);
    }

    [Fact]
    public async Task CreateContextAsync_WithHeadlessTrue_ShouldLaunchHeadlessBrowser()
    {
        // Arrange
        SetupMocks("chromium", null);
        var factory = new BrowserContextFactory(
            _playwrightMock.Object,
            _proxyServiceMock.Object,
            _userAgentServiceMock.Object,
            _logger);

        // Act
        var result = await factory.CreateContextAsync("chromium", headless: true);

        // Assert
        Assert.NotNull(result);
        _browserTypeMock.Verify(
            bt => bt.LaunchAsync(It.Is<BrowserTypeLaunchOptions>(o => o.Headless == true)),
            Times.Once);
    }

    private void SetupMocks(string browserType, Microsoft.Playwright.Proxy? proxy)
    {
        // Setup user agent
        _userAgentServiceMock
            .Setup(u => u.GetNextUserAgent(It.IsAny<string>()))
            .Returns("Mozilla/5.0 Test Agent");

        // Setup proxy
        _proxyServiceMock
            .Setup(p => p.GetPlaywrightProxy())
            .Returns(proxy);

        // Setup browser launch
        _browserTypeMock
            .Setup(bt => bt.LaunchAsync(It.IsAny<BrowserTypeLaunchOptions>()))
            .ReturnsAsync(_browserMock.Object);

        // Setup browser context creation
        _browserMock
            .Setup(b => b.NewContextAsync(It.IsAny<BrowserNewContextOptions>()))
            .ReturnsAsync(_contextMock.Object);

        // Setup playwright browser types
        _playwrightMock.Setup(p => p.Chromium).Returns(_browserTypeMock.Object);
        _playwrightMock.Setup(p => p.Firefox).Returns(_browserTypeMock.Object);
        _playwrightMock.Setup(p => p.Webkit).Returns(_browserTypeMock.Object);
    }
}
