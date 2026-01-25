using Microsoft.EntityFrameworkCore;
using Search.Core.Entities;
using Search.Core.Interfaces;
using Search.Infrastructure.Data;

namespace Search.Infrastructure.Services;

/// <summary>
/// Implementation of the search profile service.
/// </summary>
public sealed class SearchProfileService : ISearchProfileService
{
    private readonly SearchDbContext _context;

    public SearchProfileService(SearchDbContext context)
    {
        _context = context;
    }

    public async Task<SearchProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SearchProfiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SearchProfiles
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchProfile>> GetActiveProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SearchProfiles
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SearchProfile> CreateAsync(string name, SearchCriteria criteria, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var profile = SearchProfile.Create(name, criteria, userId);
        _context.SearchProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<SearchProfile> UpdateAsync(Guid id, string? name = null, SearchCriteria? criteria = null, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Search profile {id} not found");

        profile.Update(name, criteria);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Search profile {id} not found");

        _context.SearchProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Search profile {id} not found");

        profile.Activate();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Search profile {id} not found");

        profile.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
