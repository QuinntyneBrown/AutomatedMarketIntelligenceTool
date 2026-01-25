using Dealer.Core.Entities;

namespace Dealer.Core.Interfaces;

public interface IDealerService
{
    Task<Entities.Dealer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Dealer?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Dealer>> SearchAsync(DealerSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<Entities.Dealer> CreateAsync(DealerData data, CancellationToken cancellationToken = default);
    Task<Entities.Dealer> UpdateAsync(Guid id, DealerData data, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Entities.Dealer> UpdateReliabilityScoreAsync(Guid id, decimal newScore, string? reason = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Dealer>> GetByProvinceAsync(string province, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Dealer>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
}

public sealed record DealerSearchCriteria
{
    public string? Name { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public decimal? MinReliabilityScore { get; init; }
    public int? MinListingCount { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

public sealed record DealerData
{
    public string Name { get; init; } = string.Empty;
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
}
