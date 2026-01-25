using Deduplication.Core.Entities;
using Deduplication.Core.Interfaces;
using Deduplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deduplication.Infrastructure.Services;

public sealed class DeduplicationConfigService : IDeduplicationConfigService
{
    private readonly DeduplicationDbContext _context;

    public DeduplicationConfigService(DeduplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeduplicationConfig> GetActiveConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.Configs
            .FirstOrDefaultAsync(c => c.IsActive, cancellationToken);

        if (config == null)
        {
            // Create default config if none exists
            config = DeduplicationConfig.CreateDefault();
            _context.Configs.Add(config);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return config;
    }

    public async Task<DeduplicationConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Configs
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DeduplicationConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Configs
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeduplicationConfig> CreateAsync(
        string name,
        double duplicateThreshold,
        double reviewThreshold,
        double vinWeight,
        double titleWeight,
        double priceWeight,
        double locationWeight,
        double imageWeight,
        CancellationToken cancellationToken = default)
    {
        var config = DeduplicationConfig.CreateDefault();
        config.UpdateThresholds(
            overallThreshold: duplicateThreshold,
            reviewThreshold: reviewThreshold);
        config.UpdateWeights(
            vinWeight: vinWeight,
            titleWeight: titleWeight,
            priceWeight: priceWeight,
            locationWeight: locationWeight,
            imageWeight: imageWeight);
        config.SetActive(false);

        _context.Configs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        return config;
    }

    public async Task<DeduplicationConfig> UpdateAsync(
        Guid id,
        double duplicateThreshold,
        double reviewThreshold,
        double vinWeight,
        double titleWeight,
        double priceWeight,
        double locationWeight,
        double imageWeight,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.Configs
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Config {id} not found");

        config.UpdateThresholds(
            overallThreshold: duplicateThreshold,
            reviewThreshold: reviewThreshold);
        config.UpdateWeights(
            vinWeight: vinWeight,
            titleWeight: titleWeight,
            priceWeight: priceWeight,
            locationWeight: locationWeight,
            imageWeight: imageWeight);

        await _context.SaveChangesAsync(cancellationToken);

        return config;
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Deactivate all other configs
        var allConfigs = await _context.Configs.ToListAsync(cancellationToken);
        foreach (var c in allConfigs)
        {
            c.SetActive(c.Id == id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _context.Configs
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Config {id} not found");

        config.SetActive(false);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
