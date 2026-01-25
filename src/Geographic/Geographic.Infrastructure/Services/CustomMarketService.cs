using Geographic.Core.Entities;
using Geographic.Core.Interfaces;
using Geographic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Geographic.Infrastructure.Services;

/// <summary>
/// Service implementation for managing custom market areas.
/// </summary>
public sealed class CustomMarketService : ICustomMarketService
{
    private readonly GeographicDbContext _context;

    public CustomMarketService(GeographicDbContext context)
    {
        _context = context;
    }

    public async Task<CustomMarket> CreateAsync(CustomMarket market, CancellationToken cancellationToken = default)
    {
        market.Id = Guid.NewGuid();
        market.CreatedAt = DateTimeOffset.UtcNow;

        _context.CustomMarkets.Add(market);
        await _context.SaveChangesAsync(cancellationToken);

        return market;
    }

    public async Task<CustomMarket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CustomMarkets
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CustomMarket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CustomMarkets
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomMarket> UpdateAsync(CustomMarket market, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CustomMarkets
            .FirstOrDefaultAsync(m => m.Id == market.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Market with ID {market.Id} not found.");

        existing.Name = market.Name;
        existing.CenterLatitude = market.CenterLatitude;
        existing.CenterLongitude = market.CenterLongitude;
        existing.RadiusKm = market.RadiusKm;
        existing.PostalCodes = market.PostalCodes;

        await _context.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var market = await _context.CustomMarkets
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (market != null)
        {
            _context.CustomMarkets.Remove(market);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<CustomMarket>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.CustomMarkets
            .Where(m => m.Name.Contains(name))
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }
}
