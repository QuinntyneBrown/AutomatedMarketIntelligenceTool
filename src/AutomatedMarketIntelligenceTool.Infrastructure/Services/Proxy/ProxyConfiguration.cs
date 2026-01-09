namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.Proxy;

public class ProxyConfiguration
{
    public bool Enabled { get; set; }
    public string? Address { get; set; }
    public ProxyType Type { get; set; } = ProxyType.Http;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public enum ProxyType
{
    Http,
    Https,
    Socks5
}
