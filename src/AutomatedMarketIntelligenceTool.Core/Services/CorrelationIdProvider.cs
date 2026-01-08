namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Thread-safe provider for correlation IDs.
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string CorrelationId => CurrentCorrelationId.Value ?? Guid.NewGuid().ToString();

    public void SetCorrelationId(string correlationId)
    {
        CurrentCorrelationId.Value = correlationId;
    }
}
