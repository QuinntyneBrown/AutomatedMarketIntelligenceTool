using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

/// <summary>
/// Implementation of auto-complete service for search parameters.
/// </summary>
public class AutoCompleteService : IAutoCompleteService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<AutoCompleteService> _logger;

    public AutoCompleteService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<AutoCompleteService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<string>> GetMakeSuggestionsAsync(
        Guid tenantId,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting make suggestions for tenant {TenantId} with prefix '{Prefix}'", tenantId, prefix);

        var query = _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.Make != null);

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var lowerPrefix = prefix.ToLowerInvariant();
            query = query.Where(l => l.Make!.ToLower().StartsWith(lowerPrefix));
        }

        var makes = await query
            .Select(l => l.Make!)
            .Distinct()
            .OrderBy(m => m)
            .Take(50)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} make suggestions", makes.Count);
        return makes;
    }

    public async Task<IReadOnlyList<string>> GetModelSuggestionsAsync(
        Guid tenantId,
        string? make = null,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting model suggestions for tenant {TenantId}, make '{Make}', prefix '{Prefix}'",
            tenantId, make, prefix);

        var query = _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.Model != null);

        if (!string.IsNullOrWhiteSpace(make))
        {
            var lowerMake = make.ToLowerInvariant();
            query = query.Where(l => l.Make != null && l.Make.ToLower() == lowerMake);
        }

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var lowerPrefix = prefix.ToLowerInvariant();
            query = query.Where(l => l.Model!.ToLower().StartsWith(lowerPrefix));
        }

        var models = await query
            .Select(l => l.Model!)
            .Distinct()
            .OrderBy(m => m)
            .Take(50)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} model suggestions", models.Count);
        return models;
    }

    public async Task<IReadOnlyList<string>> GetCitySuggestionsAsync(
        Guid tenantId,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting city suggestions for tenant {TenantId} with prefix '{Prefix}'", tenantId, prefix);

        var query = _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.City != null);

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var lowerPrefix = prefix.ToLowerInvariant();
            query = query.Where(l => l.City!.ToLower().StartsWith(lowerPrefix));
        }

        var cities = await query
            .Select(l => l.City!)
            .Distinct()
            .OrderBy(c => c)
            .Take(50)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {Count} city suggestions", cities.Count);
        return cities;
    }

    public async Task<(int MinYear, int MaxYear)> GetYearRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting year range for tenant {TenantId}", tenantId);

        var query = _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.Year > 0);

        var hasData = await query.AnyAsync(cancellationToken);
        if (!hasData)
        {
            var currentYear = DateTime.UtcNow.Year;
            return (currentYear - 10, currentYear + 1);
        }

        var minYear = await query.MinAsync(l => l.Year, cancellationToken);
        var maxYear = await query.MaxAsync(l => l.Year, cancellationToken);

        _logger.LogDebug("Year range: {MinYear} - {MaxYear}", minYear, maxYear);
        return (minYear, maxYear);
    }

    public async Task<(decimal MinPrice, decimal MaxPrice)> GetPriceRangeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting price range for tenant {TenantId}", tenantId);

        var query = _context.Listings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.Price > 0);

        var hasData = await query.AnyAsync(cancellationToken);
        if (!hasData)
        {
            return (0m, 100000m);
        }

        var minPrice = await query.MinAsync(l => l.Price, cancellationToken);
        var maxPrice = await query.MaxAsync(l => l.Price, cancellationToken);

        _logger.LogDebug("Price range: {MinPrice} - {MaxPrice}", minPrice, maxPrice);
        return (minPrice, maxPrice);
    }
}
