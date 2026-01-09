namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Service for providing auto-complete suggestions for search parameters.
/// </summary>
public interface IAutoCompleteService
{
    /// <summary>
    /// Gets distinct vehicle makes from the database, optionally filtered by prefix.
    /// </summary>
    Task<IReadOnlyList<string>> GetMakeSuggestionsAsync(
        Guid tenantId,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct vehicle models from the database, optionally filtered by make and prefix.
    /// </summary>
    Task<IReadOnlyList<string>> GetModelSuggestionsAsync(
        Guid tenantId,
        string? make = null,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct cities from the database, optionally filtered by prefix.
    /// </summary>
    Task<IReadOnlyList<string>> GetCitySuggestionsAsync(
        Guid tenantId,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the year range from existing listings.
    /// </summary>
    Task<(int MinYear, int MaxYear)> GetYearRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the price range from existing listings.
    /// </summary>
    Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
