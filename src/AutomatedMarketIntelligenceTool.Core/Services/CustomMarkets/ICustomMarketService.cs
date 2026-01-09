using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services.CustomMarkets;

/// <summary>
/// Service for managing custom market regions.
/// </summary>
public interface ICustomMarketService
{
    /// <summary>
    /// Creates a new custom market region.
    /// </summary>
    Task<CustomMarket> CreateMarketAsync(
        Guid tenantId,
        string name,
        string postalCodes,
        string? description = null,
        string? provinces = null,
        double? centerLatitude = null,
        double? centerLongitude = null,
        int? radiusKm = null,
        int priority = 100,
        string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom market by ID.
    /// </summary>
    Task<CustomMarket?> GetMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom market by name.
    /// </summary>
    Task<CustomMarket?> GetMarketByNameAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all custom markets for a tenant.
    /// </summary>
    Task<IReadOnlyList<CustomMarket>> GetAllMarketsAsync(
        Guid tenantId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing custom market.
    /// </summary>
    Task<CustomMarket> UpdateMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        string name,
        string postalCodes,
        string? description = null,
        string? provinces = null,
        double? centerLatitude = null,
        double? centerLongitude = null,
        int? radiusKm = null,
        int? priority = null,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a custom market.
    /// </summary>
    Task<bool> ActivateMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a custom market.
    /// </summary>
    Task<bool> DeactivateMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom market.
    /// </summary>
    Task<bool> DeleteMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the postal codes for a market.
    /// </summary>
    Task<IReadOnlyList<string>> GetMarketPostalCodesAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default);
}
