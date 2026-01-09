namespace AutomatedMarketIntelligenceTool.Core.Models.SearchProfileAggregate;

/// <summary>
/// Represents a saved search configuration that can be reused.
/// </summary>
public class SearchProfile
{
    public SearchProfileId SearchProfileId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string SearchParametersJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private SearchProfile()
    {
        SearchProfileId = SearchProfileId.Create();
        Name = string.Empty;
        SearchParametersJson = string.Empty;
    }

    public static SearchProfile Create(
        Guid tenantId,
        string name,
        string searchParametersJson,
        string? description = null)
    {
        var profile = new SearchProfile
        {
            SearchProfileId = SearchProfileId.Create(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            SearchParametersJson = searchParametersJson,
            CreatedAt = DateTime.UtcNow
        };

        return profile;
    }

    public void UpdateSearchParameters(string searchParametersJson, string? description = null)
    {
        SearchParametersJson = searchParametersJson;
        if (description != null)
        {
            Description = description;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
