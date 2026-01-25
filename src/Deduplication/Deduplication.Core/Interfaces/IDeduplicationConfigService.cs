using Deduplication.Core.Entities;

namespace Deduplication.Core.Interfaces;

/// <summary>
/// Service for managing deduplication configuration.
/// </summary>
public interface IDeduplicationConfigService
{
    Task<DeduplicationConfig> GetActiveConfigAsync(CancellationToken cancellationToken = default);

    Task<DeduplicationConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeduplicationConfig>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DeduplicationConfig> CreateAsync(
        string name,
        double duplicateThreshold,
        double reviewThreshold,
        double vinWeight,
        double titleWeight,
        double priceWeight,
        double locationWeight,
        double imageWeight,
        CancellationToken cancellationToken = default);

    Task<DeduplicationConfig> UpdateAsync(
        Guid id,
        double duplicateThreshold,
        double reviewThreshold,
        double vinWeight,
        double titleWeight,
        double priceWeight,
        double locationWeight,
        double imageWeight,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
