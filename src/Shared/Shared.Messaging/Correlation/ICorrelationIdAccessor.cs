namespace Shared.Messaging.Correlation;

/// <summary>
/// Provides access to the current correlation ID.
/// </summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}

/// <summary>
/// Default implementation of ICorrelationIdAccessor using AsyncLocal.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}
