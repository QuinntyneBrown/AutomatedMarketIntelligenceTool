namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Provides correlation ID for tracking operations across layers.
/// </summary>
public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
