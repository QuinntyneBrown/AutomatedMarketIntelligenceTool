using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public class SearchProfileService : ISearchProfileService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<SearchProfileService> _logger;

    public SearchProfileService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<SearchProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchProfile> SaveProfileAsync(
        Guid tenantId,
        string name,
        string searchParametersJson,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving search profile {ProfileName} for tenant {TenantId}", name, tenantId);

        // Check if profile already exists
        var existing = await _context.SearchProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == name, cancellationToken);

        if (existing != null)
        {
            // Update existing profile
            existing.UpdateSearchParameters(searchParametersJson, description);
            _logger.LogInformation("Updated existing search profile {ProfileName}", name);
        }
        else
        {
            // Create new profile
            var profile = SearchProfile.Create(tenantId, name, searchParametersJson, description);
            await _context.SearchProfiles.AddAsync(profile, cancellationToken);
            _logger.LogInformation("Created new search profile {ProfileName}", name);
            existing = profile;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<SearchProfile?> LoadProfileAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading search profile {ProfileName} for tenant {TenantId}", name, tenantId);

        var profile = await _context.SearchProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == name, cancellationToken);

        if (profile != null)
        {
            profile.RecordUsage();
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Loaded and recorded usage for profile {ProfileName}", name);
        }
        else
        {
            _logger.LogWarning("Profile {ProfileName} not found for tenant {TenantId}", name, tenantId);
        }

        return profile;
    }

    public async Task<List<SearchProfile>> ListProfilesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing all profiles for tenant {TenantId}", tenantId);

        var profiles = await _context.SearchProfiles
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} profiles for tenant {TenantId}", profiles.Count, tenantId);
        return profiles;
    }

    public async Task<bool> DeleteProfileAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting search profile {ProfileName} for tenant {TenantId}", name, tenantId);

        var profile = await _context.SearchProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == name, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Profile {ProfileName} not found for deletion", name);
            return false;
        }

        _context.SearchProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted profile {ProfileName}", name);
        return true;
    }

    public async Task<bool> ProfileExistsAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.SearchProfiles
            .AnyAsync(p => p.TenantId == tenantId && p.Name == name, cancellationToken);
    }
}
