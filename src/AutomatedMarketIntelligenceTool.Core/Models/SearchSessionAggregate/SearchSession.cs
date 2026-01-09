namespace AutomatedMarketIntelligenceTool.Core.Models.SearchSessionAggregate;

public class SearchSession
{
    public SearchSessionId SearchSessionId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public SearchSessionStatus Status { get; private set; }
    public string SearchParameters { get; private set; }
    public int TotalListingsFound { get; private set; }
    public int NewListingsCount { get; private set; }
    public int PriceChangesCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SearchSession()
    {
        SearchSessionId = SearchSessionId.Create();
        SearchParameters = string.Empty;
    }

    public static SearchSession Create(
        Guid tenantId,
        string searchParameters)
    {
        return new SearchSession
        {
            SearchSessionId = SearchSessionId.Create(),
            TenantId = tenantId,
            StartTime = DateTime.UtcNow,
            Status = SearchSessionStatus.Running,
            SearchParameters = searchParameters,
            TotalListingsFound = 0,
            NewListingsCount = 0,
            PriceChangesCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(int totalListingsFound, int newListingsCount, int priceChangesCount)
    {
        Status = SearchSessionStatus.Completed;
        EndTime = DateTime.UtcNow;
        TotalListingsFound = totalListingsFound;
        NewListingsCount = newListingsCount;
        PriceChangesCount = priceChangesCount;
    }

    public void Fail(string errorMessage)
    {
        Status = SearchSessionStatus.Failed;
        EndTime = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}
