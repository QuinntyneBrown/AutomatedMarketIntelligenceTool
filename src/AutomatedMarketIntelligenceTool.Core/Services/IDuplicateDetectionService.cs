namespace AutomatedMarketIntelligenceTool.Core.Services;

public interface IDuplicateDetectionService
{
    Task<DuplicateCheckResult> CheckForDuplicateAsync(
        ScrapedListingInfo scrapedListing,
        CancellationToken cancellationToken = default);
}

public class ScrapedListingInfo
{
    public required string ExternalId { get; init; }
    public required string SourceSite { get; init; }
    public string? Vin { get; init; }
    public required Guid TenantId { get; init; }
}

public class DuplicateCheckResult
{
    public bool IsDuplicate { get; init; }
    public DuplicateMatchType MatchType { get; init; }
    public Guid? ExistingListingId { get; init; }

    public static DuplicateCheckResult VinMatch(Guid existingListingId) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.VinMatch,
            ExistingListingId = existingListingId
        };

    public static DuplicateCheckResult ExternalIdMatch(Guid existingListingId) =>
        new()
        {
            IsDuplicate = true,
            MatchType = DuplicateMatchType.ExternalIdMatch,
            ExistingListingId = existingListingId
        };

    public static DuplicateCheckResult NewListing() =>
        new()
        {
            IsDuplicate = false,
            MatchType = DuplicateMatchType.None,
            ExistingListingId = null
        };
}

public enum DuplicateMatchType
{
    None,
    VinMatch,
    ExternalIdMatch
}
