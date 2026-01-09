using AutomatedMarketIntelligenceTool.Core.Models.DealerAggregate;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Models;

public class DealerAggregateTests
{
    private readonly Guid _testTenantId = Guid.NewGuid();

    [Fact]
    public void Create_InitializesWithDefaultAnalytics()
    {
        // Act
        var dealer = Dealer.Create(_testTenantId, "Test Dealer", "City", "CA");

        // Assert
        Assert.Null(dealer.ReliabilityScore);
        Assert.Null(dealer.AvgDaysOnMarket);
        Assert.Equal(0, dealer.TotalListingsHistorical);
        Assert.False(dealer.FrequentRelisterFlag);
        Assert.Null(dealer.LastAnalyzedAt);
    }

    [Fact]
    public void UpdateAnalytics_SetsAllFields()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(85.5m, 45, 100, true);

        // Assert
        Assert.Equal(85.5m, dealer.ReliabilityScore);
        Assert.Equal(45, dealer.AvgDaysOnMarket);
        Assert.Equal(100, dealer.TotalListingsHistorical);
        Assert.True(dealer.FrequentRelisterFlag);
        Assert.NotNull(dealer.LastAnalyzedAt);
        Assert.True(dealer.LastAnalyzedAt.Value <= DateTime.UtcNow);
    }

    [Fact]
    public void UpdateAnalytics_UpdatesLastAnalyzedAt()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");
        var firstTime = DateTime.UtcNow;
        dealer.UpdateAnalytics(80m, 40, 50, false);
        var firstAnalyzedAt = dealer.LastAnalyzedAt;

        // Act
        Thread.Sleep(10); // Small delay to ensure time difference
        dealer.UpdateAnalytics(85m, 42, 55, false);
        var secondAnalyzedAt = dealer.LastAnalyzedAt;

        // Assert
        Assert.NotNull(firstAnalyzedAt);
        Assert.NotNull(secondAnalyzedAt);
        Assert.True(secondAnalyzedAt > firstAnalyzedAt);
    }

    [Fact]
    public void UpdateAnalytics_CanUpdateMultipleTimes()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(70m, 30, 10, false);
        dealer.UpdateAnalytics(80m, 35, 20, false);
        dealer.UpdateAnalytics(90m, 25, 30, true);

        // Assert
        Assert.Equal(90m, dealer.ReliabilityScore);
        Assert.Equal(25, dealer.AvgDaysOnMarket);
        Assert.Equal(30, dealer.TotalListingsHistorical);
        Assert.True(dealer.FrequentRelisterFlag);
    }

    [Fact]
    public void ClearAnalytics_ResetsAllFields()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");
        dealer.UpdateAnalytics(85m, 45, 100, true);

        // Act
        dealer.ClearAnalytics();

        // Assert
        Assert.Null(dealer.ReliabilityScore);
        Assert.Null(dealer.AvgDaysOnMarket);
        Assert.Equal(0, dealer.TotalListingsHistorical);
        Assert.False(dealer.FrequentRelisterFlag);
        Assert.Null(dealer.LastAnalyzedAt);
    }

    [Fact]
    public void ClearAnalytics_CanBeCalledMultipleTimes()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");
        dealer.UpdateAnalytics(85m, 45, 100, true);

        // Act
        dealer.ClearAnalytics();
        dealer.ClearAnalytics(); // Should not throw

        // Assert
        Assert.Null(dealer.ReliabilityScore);
    }

    [Fact]
    public void ClearAnalytics_OnNewDealer_DoesNotThrow()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act & Assert - should not throw
        dealer.ClearAnalytics();
        Assert.Null(dealer.ReliabilityScore);
    }

    [Fact]
    public void UpdateAnalytics_AcceptsZeroValues()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(0m, 0, 0, false);

        // Assert
        Assert.Equal(0m, dealer.ReliabilityScore);
        Assert.Equal(0, dealer.AvgDaysOnMarket);
        Assert.Equal(0, dealer.TotalListingsHistorical);
        Assert.False(dealer.FrequentRelisterFlag);
    }

    [Fact]
    public void UpdateAnalytics_AcceptsMaximumValues()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(100m, int.MaxValue, int.MaxValue, true);

        // Assert
        Assert.Equal(100m, dealer.ReliabilityScore);
        Assert.Equal(int.MaxValue, dealer.AvgDaysOnMarket);
        Assert.Equal(int.MaxValue, dealer.TotalListingsHistorical);
        Assert.True(dealer.FrequentRelisterFlag);
    }

    [Fact]
    public void UpdateAnalytics_PreservesOtherProperties()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Original Name", "Original City", "CA", "555-1234");

        // Act
        dealer.UpdateAnalytics(85m, 45, 100, true);

        // Assert - original properties unchanged
        Assert.Equal("Original Name", dealer.Name);
        Assert.Equal("Original City", dealer.City);
        Assert.Equal("CA", dealer.State);
        Assert.Equal("555-1234", dealer.Phone);
    }

    [Fact]
    public void UpdateAnalytics_ToggleFrequentRelisterFlag()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(85m, 45, 100, true);
        Assert.True(dealer.FrequentRelisterFlag);

        dealer.UpdateAnalytics(90m, 40, 105, false);

        // Assert
        Assert.False(dealer.FrequentRelisterFlag);
    }

    [Fact]
    public void UpdateAnalytics_WithDecimalReliabilityScore()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");

        // Act
        dealer.UpdateAnalytics(87.53m, 42, 89, false);

        // Assert
        Assert.Equal(87.53m, dealer.ReliabilityScore);
    }

    [Fact]
    public void Analytics_IndependentAcrossDealers()
    {
        // Arrange
        var dealer1 = Dealer.Create(_testTenantId, "Dealer One");
        var dealer2 = Dealer.Create(_testTenantId, "Dealer Two");

        // Act
        dealer1.UpdateAnalytics(70m, 30, 50, true);
        dealer2.UpdateAnalytics(90m, 20, 100, false);

        // Assert
        Assert.Equal(70m, dealer1.ReliabilityScore);
        Assert.Equal(90m, dealer2.ReliabilityScore);
        Assert.True(dealer1.FrequentRelisterFlag);
        Assert.False(dealer2.FrequentRelisterFlag);
    }

    [Fact]
    public void ClearAnalytics_AfterUpdate_AllowsNewUpdate()
    {
        // Arrange
        var dealer = Dealer.Create(_testTenantId, "Test Dealer");
        dealer.UpdateAnalytics(85m, 45, 100, true);
        dealer.ClearAnalytics();

        // Act
        dealer.UpdateAnalytics(95m, 25, 150, false);

        // Assert
        Assert.Equal(95m, dealer.ReliabilityScore);
        Assert.Equal(25, dealer.AvgDaysOnMarket);
        Assert.Equal(150, dealer.TotalListingsHistorical);
        Assert.False(dealer.FrequentRelisterFlag);
        Assert.NotNull(dealer.LastAnalyzedAt);
    }
}
