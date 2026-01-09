using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutomatedMarketIntelligenceTool.Core.Services.Deduplication;

/// <summary>
/// Service for managing deduplication audit trail and tracking.
/// </summary>
public class DeduplicationAuditService : IDeduplicationAuditService
{
    private readonly IAutomatedMarketIntelligenceToolContext _context;
    private readonly ILogger<DeduplicationAuditService> _logger;

    public DeduplicationAuditService(
        IAutomatedMarketIntelligenceToolContext context,
        ILogger<DeduplicationAuditService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuditEntry> RecordAutomaticDecisionAsync(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision decision,
        AuditReason reason,
        decimal? confidenceScore = null,
        string? fuzzyMatchDetailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.CreateAutomatic(
            tenantId,
            listing1Id,
            listing2Id,
            decision,
            reason,
            confidenceScore,
            fuzzyMatchDetailsJson);

        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Recorded automatic deduplication decision: {Decision} ({Reason}) for listing {ListingId}",
            decision, reason, listing1Id);

        return entry;
    }

    public async Task<AuditEntry> RecordManualOverrideAsync(
        Guid tenantId,
        Guid listing1Id,
        Guid? listing2Id,
        AuditDecision newDecision,
        AuditReason reason,
        string overrideReason,
        Guid originalAuditEntryId,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(overrideReason))
            throw new ArgumentException("Override reason is required", nameof(overrideReason));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        var entry = AuditEntry.CreateManualOverride(
            tenantId,
            listing1Id,
            listing2Id,
            newDecision,
            reason,
            overrideReason,
            originalAuditEntryId,
            createdBy);

        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded manual override: {Decision} for listing {ListingId} by {CreatedBy}",
            newDecision, listing1Id, createdBy);

        return entry;
    }

    public async Task<IReadOnlyList<AuditEntry>> GetAuditEntriesForListingAsync(
        Guid tenantId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditEntries
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId &&
                       (a.Listing1Id == listingId || a.Listing2Id == listingId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditQueryResult> QueryAuditEntriesAsync(
        Guid tenantId,
        AuditQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEntries
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId);

        // Apply filters
        if (filter.Decision.HasValue)
            query = query.Where(a => a.Decision == filter.Decision.Value);

        if (filter.Reason.HasValue)
            query = query.Where(a => a.Reason == filter.Reason.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);

        if (filter.WasAutomatic.HasValue)
            query = query.Where(a => a.WasAutomatic == filter.WasAutomatic.Value);

        if (filter.HasManualOverride.HasValue)
            query = query.Where(a => a.ManualOverride == filter.HasManualOverride.Value);

        if (filter.IsFalsePositive.HasValue)
            query = query.Where(a => a.IsFalsePositive == filter.IsFalsePositive.Value);

        if (filter.IsFalseNegative.HasValue)
            query = query.Where(a => a.IsFalseNegative == filter.IsFalseNegative.Value);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = filter.SortBy switch
        {
            AuditSortField.Decision => filter.SortDescending
                ? query.OrderByDescending(a => a.Decision)
                : query.OrderBy(a => a.Decision),
            AuditSortField.ConfidenceScore => filter.SortDescending
                ? query.OrderByDescending(a => a.ConfidenceScore)
                : query.OrderBy(a => a.ConfidenceScore),
            _ => filter.SortDescending
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken);

        return new AuditQueryResult
        {
            Items = items,
            TotalCount = totalCount,
            Skip = filter.Skip,
            Take = filter.Take
        };
    }

    public async Task<bool> MarkAsFalsePositiveAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.AuditEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.AuditEntryId.Value == auditEntryId,
                cancellationToken);

        if (entry == null)
            return false;

        entry.MarkAsFalsePositive();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked audit entry {AuditEntryId} as false positive",
            auditEntryId);

        return true;
    }

    public async Task<bool> MarkAsFalseNegativeAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.AuditEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.AuditEntryId.Value == auditEntryId,
                cancellationToken);

        if (entry == null)
            return false;

        entry.MarkAsFalseNegative();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked audit entry {AuditEntryId} as false negative",
            auditEntryId);

        return true;
    }

    public async Task<bool> ClearErrorFlagsAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.AuditEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.AuditEntryId.Value == auditEntryId,
                cancellationToken);

        if (entry == null)
            return false;

        entry.ClearErrorFlags();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleared error flags from audit entry {AuditEntryId}",
            auditEntryId);

        return true;
    }

    public async Task<FalsePositiveStats> GetFalsePositiveStatsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEntries
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        var entries = await query.ToListAsync(cancellationToken);

        var falsePositives = entries.Count(e => e.IsFalsePositive);
        var falseNegatives = entries.Count(e => e.IsFalseNegative);

        // True positives: marked as duplicate and not false positive
        var truePositives = entries.Count(e =>
            e.Decision == AuditDecision.Duplicate && !e.IsFalsePositive);

        // True negatives: marked as new listing and not false negative
        var trueNegatives = entries.Count(e =>
            e.Decision == AuditDecision.NewListing && !e.IsFalseNegative);

        var decisionBreakdown = entries
            .GroupBy(e => e.Decision)
            .ToDictionary(g => g.Key, g => g.Count());

        var reasonBreakdown = entries
            .GroupBy(e => e.Reason)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FalsePositiveStats
        {
            TotalDecisions = entries.Count,
            FalsePositiveCount = falsePositives,
            FalseNegativeCount = falseNegatives,
            TruePositiveCount = truePositives,
            TrueNegativeCount = trueNegatives,
            DecisionBreakdown = decisionBreakdown,
            ReasonBreakdown = reasonBreakdown
        };
    }

    public async Task<AuditEntry?> GetByIdAsync(
        Guid tenantId,
        Guid auditEntryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.AuditEntryId.Value == auditEntryId,
                cancellationToken);
    }
}
