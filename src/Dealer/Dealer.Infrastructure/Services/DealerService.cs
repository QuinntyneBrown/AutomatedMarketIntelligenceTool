using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using Dealer.Core.Events;
using Dealer.Core.Interfaces;
using Dealer.Infrastructure.Data;

namespace Dealer.Infrastructure.Services;

public sealed class DealerService : IDealerService
{
    private readonly DealerDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public DealerService(DealerDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<Core.Entities.Dealer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Dealers
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Core.Entities.Dealer?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return await _context.Dealers
            .FirstOrDefaultAsync(d => d.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<Core.Entities.Dealer>> SearchAsync(DealerSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.Dealers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Name))
            query = query.Where(d => d.Name.ToLower().Contains(criteria.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(criteria.City))
            query = query.Where(d => d.City != null && d.City.ToLower().Contains(criteria.City.ToLower()));

        if (!string.IsNullOrWhiteSpace(criteria.Province))
            query = query.Where(d => d.Province != null && d.Province.ToLower() == criteria.Province.ToLower());

        if (criteria.MinReliabilityScore.HasValue)
            query = query.Where(d => d.ReliabilityScore >= criteria.MinReliabilityScore.Value);

        if (criteria.MinListingCount.HasValue)
            query = query.Where(d => d.ListingCount >= criteria.MinListingCount.Value);

        return await query
            .OrderByDescending(d => d.ReliabilityScore)
            .ThenByDescending(d => d.ListingCount)
            .Skip(criteria.Skip)
            .Take(criteria.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Core.Entities.Dealer> CreateAsync(DealerData data, CancellationToken cancellationToken = default)
    {
        var dealer = Core.Entities.Dealer.Create(
            data.Name,
            data.Website,
            data.Phone,
            data.Address,
            data.City,
            data.Province,
            data.PostalCode);

        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new DealerCreatedEvent
        {
            DealerId = dealer.Id,
            Name = dealer.Name,
            NormalizedName = dealer.NormalizedName,
            City = dealer.City,
            Province = dealer.Province
        }, cancellationToken);

        return dealer;
    }

    public async Task<Core.Entities.Dealer> UpdateAsync(Guid id, DealerData data, CancellationToken cancellationToken = default)
    {
        var dealer = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Dealer {id} not found");

        dealer.UpdateDetails(
            data.Name,
            data.Website,
            data.Phone,
            data.Address,
            data.City,
            data.Province,
            data.PostalCode);

        await _context.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new DealerUpdatedEvent
        {
            DealerId = dealer.Id,
            Name = dealer.Name,
            Website = dealer.Website,
            Phone = dealer.Phone,
            City = dealer.City,
            Province = dealer.Province,
            ListingCount = dealer.ListingCount
        }, cancellationToken);

        return dealer;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dealer = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Dealer {id} not found");

        _context.Dealers.Remove(dealer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Core.Entities.Dealer> UpdateReliabilityScoreAsync(Guid id, decimal newScore, string? reason = null, CancellationToken cancellationToken = default)
    {
        var dealer = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Dealer {id} not found");

        var previousScore = dealer.ReliabilityScore;
        dealer.UpdateReliabilityScore(newScore);

        await _context.SaveChangesAsync(cancellationToken);

        if (previousScore != dealer.ReliabilityScore)
        {
            await _eventPublisher.PublishAsync(new ReliabilityScoreChangedEvent
            {
                DealerId = dealer.Id,
                PreviousScore = previousScore,
                NewScore = dealer.ReliabilityScore,
                Reason = reason
            }, cancellationToken);
        }

        return dealer;
    }

    public async Task<IReadOnlyList<Core.Entities.Dealer>> GetByProvinceAsync(string province, CancellationToken cancellationToken = default)
    {
        return await _context.Dealers
            .Where(d => d.Province != null && d.Province.ToLower() == province.ToLower())
            .OrderByDescending(d => d.ReliabilityScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Core.Entities.Dealer>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _context.Dealers
            .Where(d => d.City != null && d.City.ToLower() == city.ToLower())
            .OrderByDescending(d => d.ReliabilityScore)
            .ToListAsync(cancellationToken);
    }
}
