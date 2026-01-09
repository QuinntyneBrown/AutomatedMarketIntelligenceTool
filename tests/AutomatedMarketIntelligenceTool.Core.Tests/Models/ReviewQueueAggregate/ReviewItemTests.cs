using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ReviewQueueAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models.ReviewQueueAggregate;

public class ReviewItemTests
{
    private readonly Guid _testTenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldCreateReviewItem()
    {
        // Arrange
        var listing1Id = ListingId.Create();
        var listing2Id = ListingId.Create();

        // Act
        var reviewItem = ReviewItem.Create(
            _testTenantId,
            listing1Id,
            listing2Id,
            75.5m,
            MatchMethod.Fuzzy,
            "{\"MakeModelScore\": 100}");

        // Assert
        Assert.NotNull(reviewItem);
        Assert.NotEqual(Guid.Empty, reviewItem.ReviewItemId.Value);
        Assert.Equal(_testTenantId, reviewItem.TenantId);
        Assert.Equal(listing1Id, reviewItem.Listing1Id);
        Assert.Equal(listing2Id, reviewItem.Listing2Id);
        Assert.Equal(75.5m, reviewItem.ConfidenceScore);
        Assert.Equal(MatchMethod.Fuzzy, reviewItem.MatchMethod);
        Assert.Equal("{\"MakeModelScore\": 100}", reviewItem.FieldScores);
        Assert.Equal(ReviewItemStatus.Pending, reviewItem.Status);
        Assert.Equal(ResolutionDecision.None, reviewItem.Resolution);
        Assert.Null(reviewItem.ResolvedAt);
        Assert.Null(reviewItem.ResolvedBy);
        Assert.Null(reviewItem.Notes);
    }

    [Fact]
    public void Create_WithSameListingIds_ShouldThrowArgumentException()
    {
        // Arrange
        var listing1Id = ListingId.Create();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ReviewItem.Create(
                _testTenantId,
                listing1Id,
                listing1Id, // Same ID
                75.5m,
                MatchMethod.Fuzzy));
    }

    [Fact]
    public void Create_WithImageMatchMethod_ShouldSetCorrectMethod()
    {
        // Arrange
        var listing1Id = ListingId.Create();
        var listing2Id = ListingId.Create();

        // Act
        var reviewItem = ReviewItem.Create(
            _testTenantId,
            listing1Id,
            listing2Id,
            80.0m,
            MatchMethod.Image);

        // Assert
        Assert.Equal(MatchMethod.Image, reviewItem.MatchMethod);
    }

    [Fact]
    public void Resolve_WithSameVehicle_ShouldUpdateStatus()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act
        reviewItem.Resolve(ResolutionDecision.SameVehicle, "TestUser", "These are the same car");

        // Assert
        Assert.Equal(ReviewItemStatus.Resolved, reviewItem.Status);
        Assert.Equal(ResolutionDecision.SameVehicle, reviewItem.Resolution);
        Assert.NotNull(reviewItem.ResolvedAt);
        Assert.Equal("TestUser", reviewItem.ResolvedBy);
        Assert.Equal("These are the same car", reviewItem.Notes);
    }

    [Fact]
    public void Resolve_WithDifferentVehicle_ShouldUpdateStatus()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act
        reviewItem.Resolve(ResolutionDecision.DifferentVehicle);

        // Assert
        Assert.Equal(ReviewItemStatus.Resolved, reviewItem.Status);
        Assert.Equal(ResolutionDecision.DifferentVehicle, reviewItem.Resolution);
        Assert.NotNull(reviewItem.ResolvedAt);
    }

    [Fact]
    public void Resolve_WithNoneDecision_ShouldThrowArgumentException()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            reviewItem.Resolve(ResolutionDecision.None));
    }

    [Fact]
    public void Resolve_WhenAlreadyResolved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();
        reviewItem.Resolve(ResolutionDecision.SameVehicle);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            reviewItem.Resolve(ResolutionDecision.DifferentVehicle));
    }

    [Fact]
    public void Resolve_WhenDismissed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();
        reviewItem.Dismiss();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            reviewItem.Resolve(ResolutionDecision.SameVehicle));
    }

    [Fact]
    public void Dismiss_ShouldUpdateStatus()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act
        reviewItem.Dismiss("Not enough information");

        // Assert
        Assert.Equal(ReviewItemStatus.Dismissed, reviewItem.Status);
        Assert.NotNull(reviewItem.ResolvedAt);
        Assert.Equal("Not enough information", reviewItem.Notes);
    }

    [Fact]
    public void Dismiss_WhenAlreadyResolved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();
        reviewItem.Resolve(ResolutionDecision.SameVehicle);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            reviewItem.Dismiss());
    }

    [Fact]
    public void Dismiss_WhenAlreadyDismissed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();
        reviewItem.Dismiss();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            reviewItem.Dismiss());
    }

    [Fact]
    public void AddNote_ToEmptyNotes_ShouldSetNote()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act
        reviewItem.AddNote("First note");

        // Assert
        Assert.Equal("First note", reviewItem.Notes);
    }

    [Fact]
    public void AddNote_ToExistingNotes_ShouldAppendNote()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();
        reviewItem.AddNote("First note");

        // Act
        reviewItem.AddNote("Second note");

        // Assert
        Assert.Equal("First note\nSecond note", reviewItem.Notes);
    }

    [Fact]
    public void ClearDomainEvents_ShouldClearAllEvents()
    {
        // Arrange
        var reviewItem = CreateTestReviewItem();

        // Act
        reviewItem.ClearDomainEvents();

        // Assert
        Assert.Empty(reviewItem.DomainEvents);
    }

    private ReviewItem CreateTestReviewItem()
    {
        return ReviewItem.Create(
            _testTenantId,
            ListingId.Create(),
            ListingId.Create(),
            75.5m,
            MatchMethod.Fuzzy);
    }
}

public class ReviewItemIdTests
{
    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var id1 = ReviewItemId.Create();
        var id2 = ReviewItemId.Create();

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1.Value);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldReturnValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new ReviewItemId(guid);

        // Act
        Guid result = id;

        // Assert
        Assert.Equal(guid, result);
    }

    [Fact]
    public void ImplicitConversion_FromGuid_ShouldCreateId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        ReviewItemId id = guid;

        // Assert
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new ReviewItemId(guid);

        // Act
        var str = id.ToString();

        // Assert
        Assert.Equal(guid.ToString(), str);
    }
}

public class ResolutionDecisionTests
{
    [Fact]
    public void None_ShouldHaveValueZero()
    {
        Assert.Equal(0, (int)ResolutionDecision.None);
    }

    [Fact]
    public void SameVehicle_ShouldHaveValueOne()
    {
        Assert.Equal(1, (int)ResolutionDecision.SameVehicle);
    }

    [Fact]
    public void DifferentVehicle_ShouldHaveValueTwo()
    {
        Assert.Equal(2, (int)ResolutionDecision.DifferentVehicle);
    }
}

public class ReviewItemStatusTests
{
    [Fact]
    public void Pending_ShouldHaveValueZero()
    {
        Assert.Equal(0, (int)ReviewItemStatus.Pending);
    }

    [Fact]
    public void Resolved_ShouldHaveValueOne()
    {
        Assert.Equal(1, (int)ReviewItemStatus.Resolved);
    }

    [Fact]
    public void Dismissed_ShouldHaveValueTwo()
    {
        Assert.Equal(2, (int)ReviewItemStatus.Dismissed);
    }
}

public class MatchMethodTests
{
    [Fact]
    public void Fuzzy_ShouldHaveValueOne()
    {
        Assert.Equal(1, (int)MatchMethod.Fuzzy);
    }

    [Fact]
    public void Image_ShouldHaveValueTwo()
    {
        Assert.Equal(2, (int)MatchMethod.Image);
    }

    [Fact]
    public void Combined_ShouldHaveValueThree()
    {
        Assert.Equal(3, (int)MatchMethod.Combined);
    }
}
