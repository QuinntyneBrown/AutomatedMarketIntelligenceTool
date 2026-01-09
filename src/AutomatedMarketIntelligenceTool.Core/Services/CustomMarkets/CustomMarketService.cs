using AutomatedMarketIntelligenceTool.Core.Models.CustomMarketAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.CustomMarkets;

/// <summary>
/// Service for managing custom market regions.
/// </summary>
public class CustomMarketService : ICustomMarketService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<CustomMarketService> _logger;

    public CustomMarketService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<CustomMarketService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CustomMarket> CreateMarketAsync(
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
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate name
        var existing = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.Name == name.Trim(),
                cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"A custom market with name '{name}' already exists");
        }

        var market = CustomMarket.Create(
            tenantId,
            name,
            postalCodes,
            description,
            provinces,
            centerLatitude,
            centerLongitude,
            radiusKm,
            priority,
            createdBy);

        _context.CustomMarkets.Add(market);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created custom market {MarketId} '{MarketName}' for tenant {TenantId}",
            market.CustomMarketId.Value, market.Name, tenantId);

        return market;
    }

    public async Task<CustomMarket?> GetMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.CustomMarketId == marketId,
                cancellationToken);
    }

    public async Task<CustomMarket?> GetMarketByNameAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.Name == name.Trim(),
                cancellationToken);
    }

    public async Task<IReadOnlyList<CustomMarket>> GetAllMarketsAsync(
        Guid tenantId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CustomMarkets
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == tenantId);

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomMarket> UpdateMarketAsync(
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
        CancellationToken cancellationToken = default)
    {
        var market = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.CustomMarketId == marketId,
                cancellationToken);

        if (market == null)
        {
            throw new InvalidOperationException($"Custom market {marketId} not found");
        }

        // Check for duplicate name (excluding current market)
        var duplicate = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.Name == name.Trim() && m.CustomMarketId != marketId,
                cancellationToken);

        if (duplicate != null)
        {
            throw new InvalidOperationException($"A custom market with name '{name}' already exists");
        }

        market.Update(
            name,
            postalCodes,
            description,
            provinces,
            centerLatitude,
            centerLongitude,
            radiusKm,
            priority,
            updatedBy);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated custom market {MarketId} '{MarketName}' for tenant {TenantId}",
            market.CustomMarketId.Value, market.Name, tenantId);

        return market;
    }

    public async Task<bool> ActivateMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var market = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.CustomMarketId == marketId,
                cancellationToken);

        if (market == null)
            return false;

        market.Activate(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Activated custom market {MarketId} '{MarketName}' for tenant {TenantId}",
            market.CustomMarketId.Value, market.Name, tenantId);

        return true;
    }

    public async Task<bool> DeactivateMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var market = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.CustomMarketId == marketId,
                cancellationToken);

        if (market == null)
            return false;

        market.Deactivate(updatedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deactivated custom market {MarketId} '{MarketName}' for tenant {TenantId}",
            market.CustomMarketId.Value, market.Name, tenantId);

        return true;
    }

    public async Task<bool> DeleteMarketAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default)
    {
        var market = await _context.CustomMarkets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.CustomMarketId == marketId,
                cancellationToken);

        if (market == null)
            return false;

        _context.CustomMarkets.Remove(market);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted custom market {MarketId} '{MarketName}' for tenant {TenantId}",
            market.CustomMarketId.Value, market.Name, tenantId);

        return true;
    }

    public async Task<IReadOnlyList<string>> GetMarketPostalCodesAsync(
        Guid tenantId,
        CustomMarketId marketId,
        CancellationToken cancellationToken = default)
    {
        var market = await GetMarketAsync(tenantId, marketId, cancellationToken);

        if (market == null)
            return Array.Empty<string>();

        return market.GetPostalCodeList();
    }
}
