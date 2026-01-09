using AutomatedMarketIntelligenceTool.Infrastructure.Services.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Infrastructure.Tests.Services.Headers;

/// <summary>
/// Unit tests for HeaderConfigurationService.
/// Phase 5 - REQ-WS-013: Request Header Configuration Tests
/// </summary>
public class HeaderConfigurationServiceTests
{
    private readonly ILogger<HeaderConfigurationService> _logger;

    public HeaderConfigurationServiceTests()
    {
        _logger = new NullLogger<HeaderConfigurationService>();
    }

    [Fact]
    public void GetDefaultHeaders_ShouldReturnStandardHeaders()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act
        var headers = service.GetDefaultHeaders();

        // Assert
        Assert.NotNull(headers);
        Assert.Contains("Accept", headers.Keys);
        Assert.Contains("Accept-Language", headers.Keys);
        Assert.Contains("Accept-Encoding", headers.Keys);
        Assert.Contains("Cache-Control", headers.Keys);
        Assert.Contains("Pragma", headers.Keys);
    }

    [Fact]
    public void GetDefaultHeaders_ShouldHaveCorrectAcceptHeader()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act
        var headers = service.GetDefaultHeaders();

        // Assert
        Assert.Contains("text/html", headers["Accept"]);
        Assert.Contains("application/xhtml+xml", headers["Accept"]);
        Assert.Contains("image/webp", headers["Accept"]);
    }

    [Fact]
    public void GetDefaultHeaders_WithDoNotTrackEnabled_ShouldIncludeDNTHeader()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        service.SetDoNotTrack(true);

        // Act
        var headers = service.GetDefaultHeaders();

        // Assert
        Assert.Contains("DNT", headers.Keys);
        Assert.Equal("1", headers["DNT"]);
    }

    [Fact]
    public void GetDefaultHeaders_WithDoNotTrackDisabled_ShouldNotIncludeDNTHeader()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        service.SetDoNotTrack(false);

        // Act
        var headers = service.GetDefaultHeaders();

        // Assert
        Assert.DoesNotContain("DNT", headers.Keys);
    }

    [Fact]
    public void SetCustomHeaders_ShouldStoreCustomHeaders()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var customHeaders = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue",
            ["Authorization"] = "Bearer token123"
        };

        // Act
        service.SetCustomHeaders(customHeaders);
        var headers = service.GetHeaders();

        // Assert
        Assert.Contains("X-Custom-Header", headers.Keys);
        Assert.Equal("CustomValue", headers["X-Custom-Header"]);
        Assert.Contains("Authorization", headers.Keys);
        Assert.Equal("Bearer token123", headers["Authorization"]);
    }

    [Fact]
    public void SetCustomHeaders_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.SetCustomHeaders(null!));
    }

    [Fact]
    public void ClearCustomHeaders_ShouldRemoveAllCustomHeaders()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var customHeaders = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue"
        };
        service.SetCustomHeaders(customHeaders);

        // Act
        service.ClearCustomHeaders();
        var headers = service.GetHeaders();

        // Assert
        Assert.DoesNotContain("X-Custom-Header", headers.Keys);
    }

    [Fact]
    public void GetHeaders_WithMethodCustomHeaders_ShouldMergeWithDefaults()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var methodHeaders = new Dictionary<string, string>
        {
            ["X-Request-ID"] = "req-123"
        };

        // Act
        var headers = service.GetHeaders(methodHeaders);

        // Assert
        Assert.Contains("Accept", headers.Keys);
        Assert.Contains("X-Request-ID", headers.Keys);
        Assert.Equal("req-123", headers["X-Request-ID"]);
    }

    [Fact]
    public void GetHeaders_WithMethodCustomHeaders_ShouldOverrideStoredCustomHeaders()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        service.SetCustomHeaders(new Dictionary<string, string>
        {
            ["X-Test"] = "Value1"
        });
        var methodHeaders = new Dictionary<string, string>
        {
            ["X-Test"] = "Value2"
        };

        // Act
        var headers = service.GetHeaders(methodHeaders);

        // Assert
        Assert.Equal("Value2", headers["X-Test"]);
    }

    [Fact]
    public void SetDoNotTrack_WithTrue_ShouldEnableDNT()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act
        service.SetDoNotTrack(true);

        // Assert
        Assert.True(service.GetDoNotTrack());
    }

    [Fact]
    public void SetDoNotTrack_WithFalse_ShouldDisableDNT()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        service.SetDoNotTrack(true);

        // Act
        service.SetDoNotTrack(false);

        // Assert
        Assert.False(service.GetDoNotTrack());
    }

    [Fact]
    public void SetAcceptLanguage_ShouldUpdateAcceptLanguageHeader()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var expectedLanguage = "fr-CA,fr;q=0.9";

        // Act
        service.SetAcceptLanguage(expectedLanguage);

        // Assert
        Assert.Equal(expectedLanguage, service.GetAcceptLanguage());
        var headers = service.GetDefaultHeaders();
        Assert.Equal(expectedLanguage, headers["Accept-Language"]);
    }

    [Fact]
    public void SetAcceptLanguage_WithNullOrEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.SetAcceptLanguage(null!));
        Assert.Throws<ArgumentException>(() => service.SetAcceptLanguage(""));
        Assert.Throws<ArgumentException>(() => service.SetAcceptLanguage("   "));
    }

    [Fact]
    public void GetAcceptLanguage_ShouldReturnDefaultValue()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act
        var language = service.GetAcceptLanguage();

        // Assert
        Assert.Equal("en-CA,en;q=0.9", language);
    }

    [Fact]
    public void SetReferer_ShouldStoreRefererValue()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var expectedReferer = "https://example.com/page1";

        // Act
        service.SetReferer(expectedReferer);
        var headers = service.GetHeadersWithReferer();

        // Assert
        Assert.Contains("Referer", headers.Keys);
        Assert.Equal(expectedReferer, headers["Referer"]);
    }

    [Fact]
    public void SetReferer_WithNull_ShouldNotIncludeRefererInHeaders()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        service.SetReferer("https://example.com");

        // Act
        service.SetReferer(null);
        var headers = service.GetHeadersWithReferer();

        // Assert
        Assert.DoesNotContain("Referer", headers.Keys);
    }

    [Fact]
    public void GetHeadersWithReferer_WithoutRefererSet_ShouldReturnHeadersWithoutReferer()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);

        // Act
        var headers = service.GetHeadersWithReferer();

        // Assert
        Assert.DoesNotContain("Referer", headers.Keys);
    }

    [Fact]
    public void HeaderKeys_ShouldBeCaseInsensitive()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var customHeaders = new Dictionary<string, string>
        {
            ["x-custom-header"] = "value1"
        };
        service.SetCustomHeaders(customHeaders);

        // Act
        var headers = service.GetHeaders();

        // Assert
        Assert.True(headers.ContainsKey("X-Custom-Header"));
        Assert.True(headers.ContainsKey("x-custom-header"));
        Assert.True(headers.ContainsKey("X-CUSTOM-HEADER"));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HeaderConfigurationService(null!));
    }

    [Fact]
    public void GetHeaders_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new HeaderConfigurationService(_logger);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var headers = service.GetHeaders();
                Assert.NotNull(headers);
            }));

            tasks.Add(Task.Run(() =>
            {
                service.SetCustomHeaders(new Dictionary<string, string>
                {
                    ["X-Thread-Test"] = $"Value{i}"
                });
            }));
        }

        // Assert
        Task.WaitAll(tasks.ToArray());
        // If no exception thrown, thread safety is maintained
    }
}
