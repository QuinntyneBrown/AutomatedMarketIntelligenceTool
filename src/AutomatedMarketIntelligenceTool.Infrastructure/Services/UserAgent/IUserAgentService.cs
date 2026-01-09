namespace AutomatedMarketIntelligenceTool.Infrastructure.Services.UserAgent;

public interface IUserAgentService
{
    string GetUserAgent(string browserType);
    string GetNextUserAgent(string browserType);
    void SetCustomUserAgent(string userAgent);
    bool HasCustomUserAgent();
}
