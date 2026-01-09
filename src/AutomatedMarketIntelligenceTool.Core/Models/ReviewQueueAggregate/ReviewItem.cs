using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;

/// <summary>
/// Represents a near-match pair that requires manual review.
/// </summary>
public class ReviewItem
{
    private readonly List<object> _domainEvents = new();

    public ReviewItemId ReviewItemId { get; private set; }
    public Guid TenantId { get; private set; }
    public ListingId Listing1Id { get; private set; }
    public ListingId Listing2Id { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public MatchMethod MatchMethod { get; private set; }
    public string? FieldScores { get; private set; }  // JSON serialized FuzzyMatchDetails
    public ReviewItemStatus Status { get; private set; }
    public ResolutionDecision Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private ReviewItem()
    {
        ReviewItemId = ReviewItemId.Create();
        Listing1Id = new ListingId(Guid.Empty);
        Listing2Id = new ListingId(Guid.Empty);
    }

    public static ReviewItem Create(
        Guid tenantId,
        ListingId listing1Id,
        ListingId listing2Id,
        decimal confidenceScore,
        MatchMethod matchMethod,
        string? fieldScores = null)
    {
        if (listing1Id.Value == listing2Id.Value)
        {
            throw new ArgumentException("Listing IDs must be different");
        }

        var reviewItem = new ReviewItem
        {
            ReviewItemId = ReviewItemId.Create(),
            TenantId = tenantId,
            Listing1Id = listing1Id,
            Listing2Id = listing2Id,
            ConfidenceScore = confidenceScore,
            MatchMethod = matchMethod,
            FieldScores = fieldScores,
            Status = ReviewItemStatus.Pending,
            Resolution = ResolutionDecision.None,
            CreatedAt = DateTime.UtcNow
        };

        return reviewItem;
    }

    public void Resolve(ResolutionDecision decision, string? resolvedBy = null, string? notes = null)
    {
        if (Status != ReviewItemStatus.Pending)
        {
            throw new InvalidOperationException("Cannot resolve a review item that is not pending");
        }

        if (decision == ResolutionDecision.None)
        {
            throw new ArgumentException("Must provide a resolution decision", nameof(decision));
        }

        Status = ReviewItemStatus.Resolved;
        Resolution = decision;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        Notes = notes;
    }

    public void Dismiss(string? reason = null)
    {
        if (Status != ReviewItemStatus.Pending)
        {
            throw new InvalidOperationException("Cannot dismiss a review item that is not pending");
        }

        Status = ReviewItemStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
        Notes = reason;
    }

    public void AddNote(string note)
    {
        Notes = string.IsNullOrEmpty(Notes) ? note : $"{Notes}\n{note}";
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
