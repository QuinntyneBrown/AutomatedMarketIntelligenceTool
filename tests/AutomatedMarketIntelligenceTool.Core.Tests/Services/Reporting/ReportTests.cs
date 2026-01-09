using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using Microsoft.EntityFrameworkCore;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Reporting;

public class ReportTests
{
    [Fact]
    public void Create_ShouldCreateReportWithCorrectProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var name = "Test Report";
        var format = ReportFormat.Html;
        var searchCriteria = "{\"make\":\"Toyota\"}";

        // Act
        var report = Report.Create(tenantId, name, format, searchCriteria);

        // Assert
        Assert.NotNull(report);
        Assert.NotEqual(Guid.Empty, report.ReportId.Value);
        Assert.Equal(tenantId, report.TenantId);
        Assert.Equal(name, report.Name);
        Assert.Equal(format, report.Format);
        Assert.Equal(searchCriteria, report.SearchCriteriaJson);
        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Null(report.FilePath);
        Assert.Null(report.FileSize);
        Assert.Null(report.CompletedAt);
        Assert.Null(report.ErrorMessage);
    }

    [Fact]
    public void MarkAsGenerating_ShouldUpdateStatus()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), "Test", ReportFormat.Html);

        // Act
        report.MarkAsGenerating();

        // Assert
        Assert.Equal(ReportStatus.Generating, report.Status);
    }

    [Fact]
    public void MarkAsComplete_ShouldUpdateStatusAndFileInfo()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), "Test", ReportFormat.Html);
        var filePath = "/path/to/report.html";
        var fileSize = 1024L;

        // Act
        report.MarkAsComplete(filePath, fileSize);

        // Assert
        Assert.Equal(ReportStatus.Complete, report.Status);
        Assert.Equal(filePath, report.FilePath);
        Assert.Equal(fileSize, report.FileSize);
        Assert.NotNull(report.CompletedAt);
        Assert.Null(report.ErrorMessage);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndErrorMessage()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), "Test", ReportFormat.Html);
        var errorMessage = "Test error";

        // Act
        report.MarkAsFailed(errorMessage);

        // Assert
        Assert.Equal(ReportStatus.Failed, report.Status);
        Assert.Equal(errorMessage, report.ErrorMessage);
        Assert.NotNull(report.CompletedAt);
    }

    [Theory]
    [InlineData(ReportFormat.Html)]
    [InlineData(ReportFormat.Pdf)]
    [InlineData(ReportFormat.Excel)]
    public void Create_ShouldSupportAllFormats(ReportFormat format)
    {
        // Arrange & Act
        var report = Report.Create(Guid.NewGuid(), "Test", format);

        // Assert
        Assert.Equal(format, report.Format);
    }
}
