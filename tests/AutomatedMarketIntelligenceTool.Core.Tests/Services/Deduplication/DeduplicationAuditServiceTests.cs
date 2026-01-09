using AutomatedMarketIntelligenceTool.Core.Models.DeduplicationAuditAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Deduplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Deduplication;

public class DeduplicationAuditServiceTests
{
    private readonly DeduplicationTestContext _context;
    private readonly DeduplicationAuditService _service;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public DeduplicationAuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<DeduplicationTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DeduplicationTestContext(options);
        _service = new DeduplicationAuditService(_context, NullLogger<DeduplicationAuditService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationAuditService(null!, NullLogger<DeduplicationAuditService>.Instance));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationAuditService(_context, null!));
    }

    [Fact]
    public async Task RecordAutomaticDecisionAsync_CreatesAuditEntry()
    {
        // Arrange
        var listing1Id = Guid.NewGuid();
        var listing2Id = Guid.NewGuid();

        // Act
        var result = await _service.RecordAutomaticDecisionAsync(
            _testTenantId,
            listing1Id,
            listing2Id,
            AuditDecision.Duplicate,
            AuditReason.VinMatch,
            100m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTenantId, result.TenantId);
        Assert.Equal(listing1Id, result.Listing1Id);
        Assert.Equal(listing2Id, result.Listing2Id);
        Assert.Equal(AuditDecision.Duplicate, result.Decision);
        Assert.Equal(AuditReason.VinMatch, result.Reason);
        Assert.Equal(100m, result.ConfidenceScore);
        Assert.True(result.WasAutomatic);
        Assert.False(result.ManualOverride);
    }

    [Fact]
    public async Task RecordAutomaticDecisionAsync_WithNewListing_SetsNullListing2Id()
    {
        // Act
        var result = await _service.RecordAutomaticDecisionAsync(
            _testTenantId,
            Guid.NewGuid(),
            null,
            AuditDecision.NewListing,
            AuditReason.NoMatch);

        // Assert
        Assert.Null(result.Listing2Id);
        Assert.Equal(AuditDecision.NewListing, result.Decision);
    }

    [Fact]
    public async Task RecordAutomaticDecisionAsync_WithFuzzyMatch_StoresFuzzyMatchDetails()
    {
        // Arrange
        var fuzzyDetails = @"{""MakeModelScore"":95,""YearScore"":100}";

        // Act
        var result = await _service.RecordAutomaticDecisionAsync(
            _testTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            AuditDecision.Duplicate,
            AuditReason.FuzzyMatch,
            87.5m,
            fuzzyDetails);

        // Assert
        Assert.Equal(fuzzyDetails, result.FuzzyMatchDetailsJson);
    }

    [Fact]
    public async Task RecordManualOverrideAsync_CreatesOverrideEntry()
    {
        // Arrange
        var originalEntry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 70m);

        // Act
        var result = await _service.RecordManualOverrideAsync(
            _testTenantId,
            originalEntry.Listing1Id,
            null,
            AuditDecision.NewListing,
            AuditReason.ManualReview,
            "False positive - different vehicles",
            originalEntry.AuditEntryId,
            "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AuditDecision.NewListing, result.Decision);
        Assert.Equal(AuditReason.ManualReview, result.Reason);
        Assert.False(result.WasAutomatic);
        Assert.True(result.ManualOverride);
        Assert.Equal("False positive - different vehicles", result.OverrideReason);
        Assert.Equal(originalEntry.AuditEntryId.Value, result.OriginalAuditEntryId);
        Assert.Equal("testuser", result.CreatedBy);
    }

    [Fact]
    public async Task RecordManualOverrideAsync_WithEmptyOverrideReason_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RecordManualOverrideAsync(
                _testTenantId, Guid.NewGuid(), null,
                AuditDecision.NewListing, AuditReason.ManualReview,
                "", Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task RecordManualOverrideAsync_WithEmptyCreatedBy_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RecordManualOverrideAsync(
                _testTenantId, Guid.NewGuid(), null,
                AuditDecision.NewListing, AuditReason.ManualReview,
                "reason", Guid.NewGuid(), ""));
    }

    [Fact]
    public async Task GetAuditEntriesForListingAsync_ReturnsEntriesForListing()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        await _service.RecordAutomaticDecisionAsync(_testTenantId, listingId, null, AuditDecision.NewListing, AuditReason.NoMatch);
        await _service.RecordAutomaticDecisionAsync(_testTenantId, Guid.NewGuid(), listingId, AuditDecision.Duplicate, AuditReason.VinMatch);
        await _service.RecordAutomaticDecisionAsync(_testTenantId, Guid.NewGuid(), Guid.NewGuid(), AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.GetAuditEntriesForListingAsync(_testTenantId, listingId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task QueryAuditEntriesAsync_WithNoFilter_ReturnsAllEntries()
    {
        // Arrange
        await CreateTestAuditEntries();

        // Act
        var result = await _service.QueryAuditEntriesAsync(_testTenantId, new AuditQueryFilter());

        // Assert
        Assert.True(result.TotalCount > 0);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task QueryAuditEntriesAsync_WithDecisionFilter_ReturnsFilteredEntries()
    {
        // Arrange
        await CreateTestAuditEntries();

        // Act
        var result = await _service.QueryAuditEntriesAsync(_testTenantId, new AuditQueryFilter
        {
            Decision = AuditDecision.Duplicate
        });

        // Assert
        Assert.All(result.Items, e => Assert.Equal(AuditDecision.Duplicate, e.Decision));
    }

    [Fact]
    public async Task QueryAuditEntriesAsync_WithDateFilter_ReturnsFilteredEntries()
    {
        // Arrange
        await CreateTestAuditEntries();
        var fromDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _service.QueryAuditEntriesAsync(_testTenantId, new AuditQueryFilter
        {
            FromDate = fromDate
        });

        // Assert
        Assert.All(result.Items, e => Assert.True(e.CreatedAt >= fromDate));
    }

    [Fact]
    public async Task QueryAuditEntriesAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _service.RecordAutomaticDecisionAsync(_testTenantId, Guid.NewGuid(), null,
                AuditDecision.NewListing, AuditReason.NoMatch);
        }

        // Act
        var result = await _service.QueryAuditEntriesAsync(_testTenantId, new AuditQueryFilter
        {
            Skip = 2,
            Take = 3
        });

        // Assert
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, result.Skip);
        Assert.Equal(3, result.Take);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task MarkAsFalsePositiveAsync_WithExistingEntry_ReturnsTrue()
    {
        // Arrange
        var entry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 75m);

        // Act
        var result = await _service.MarkAsFalsePositiveAsync(_testTenantId, entry.AuditEntryId);

        // Assert
        Assert.True(result);

        var updated = await _service.GetByIdAsync(_testTenantId, entry.AuditEntryId);
        Assert.NotNull(updated);
        Assert.True(updated.IsFalsePositive);
    }

    [Fact]
    public async Task MarkAsFalsePositiveAsync_WithNonExistingEntry_ReturnsFalse()
    {
        // Act
        var result = await _service.MarkAsFalsePositiveAsync(_testTenantId, Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MarkAsFalseNegativeAsync_WithExistingEntry_ReturnsTrue()
    {
        // Arrange
        var entry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.MarkAsFalseNegativeAsync(_testTenantId, entry.AuditEntryId);

        // Assert
        Assert.True(result);

        var updated = await _service.GetByIdAsync(_testTenantId, entry.AuditEntryId);
        Assert.NotNull(updated);
        Assert.True(updated.IsFalseNegative);
    }

    [Fact]
    public async Task ClearErrorFlagsAsync_WithMarkedEntry_ClearsFlags()
    {
        // Arrange
        var entry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 75m);
        await _service.MarkAsFalsePositiveAsync(_testTenantId, entry.AuditEntryId);

        // Act
        var result = await _service.ClearErrorFlagsAsync(_testTenantId, entry.AuditEntryId);

        // Assert
        Assert.True(result);

        var updated = await _service.GetByIdAsync(_testTenantId, entry.AuditEntryId);
        Assert.NotNull(updated);
        Assert.False(updated.IsFalsePositive);
        Assert.False(updated.IsFalseNegative);
    }

    [Fact]
    public async Task GetFalsePositiveStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        // Create duplicates (true positives and false positives)
        var dup1 = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        var dup2 = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 85m);
        await _service.MarkAsFalsePositiveAsync(_testTenantId, dup2.AuditEntryId);

        // Create new listings (true negatives and false negatives)
        var new1 = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);
        var new2 = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);
        await _service.MarkAsFalseNegativeAsync(_testTenantId, new2.AuditEntryId);

        // Act
        var stats = await _service.GetFalsePositiveStatsAsync(_testTenantId);

        // Assert
        Assert.Equal(4, stats.TotalDecisions);
        Assert.Equal(1, stats.FalsePositiveCount);
        Assert.Equal(1, stats.FalseNegativeCount);
        Assert.Equal(1, stats.TruePositiveCount);
        Assert.Equal(1, stats.TrueNegativeCount);
        Assert.True(stats.DecisionBreakdown.ContainsKey(AuditDecision.Duplicate));
        Assert.True(stats.DecisionBreakdown.ContainsKey(AuditDecision.NewListing));
    }

    [Fact]
    public async Task GetFalsePositiveStatsAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        await CreateTestAuditEntries();
        var fromDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var stats = await _service.GetFalsePositiveStatsAsync(_testTenantId, fromDate);

        // Assert
        Assert.True(stats.TotalDecisions > 0);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsEntry()
    {
        // Arrange
        var entry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.GetByIdAsync(_testTenantId, entry.AuditEntryId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entry.AuditEntryId.Value, result.AuditEntryId.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(_testTenantId, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithWrongTenant_ReturnsNull()
    {
        // Arrange
        var entry = await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid(), entry.AuditEntryId);

        // Assert
        Assert.Null(result);
    }

    private async Task CreateTestAuditEntries()
    {
        await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.VinMatch);
        await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.Duplicate, AuditReason.FuzzyMatch, 87m);
        await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), null,
            AuditDecision.NewListing, AuditReason.NoMatch);
        await _service.RecordAutomaticDecisionAsync(
            _testTenantId, Guid.NewGuid(), Guid.NewGuid(),
            AuditDecision.NearMatch, AuditReason.FuzzyMatch, 65m);
    }
}
