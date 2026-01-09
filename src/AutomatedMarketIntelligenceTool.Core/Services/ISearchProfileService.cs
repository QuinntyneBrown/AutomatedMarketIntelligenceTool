using AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface ISearchProfileService
{
    Task<SearchProfile> SaveProfileAsync(
        Guid tenantId,
        string name,
        string searchParametersJson,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<SearchProfile?> LoadProfileAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default);

    Task<List<SearchProfile>> ListProfilesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteProfileAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> ProfileExistsAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default);
}
