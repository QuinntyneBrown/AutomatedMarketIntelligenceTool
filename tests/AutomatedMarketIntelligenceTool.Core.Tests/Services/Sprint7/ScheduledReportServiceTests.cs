using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ScheduledReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.Scheduling;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutomatedMarketIntelligenceTool.Core.Tests.Services.Sprint7;

public class ScheduledReportServiceTests
{
    private readonly Sprint7TestContext _context;
    private readonly ScheduledReportService _service;
    private readonly Mock<IReportGenerationService> _mockReportService;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ScheduledReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<Sprint7TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Sprint7TestContext(options);
        _mockReportService = new Mock<IReportGenerationService>();
        _mockSearchService = new Mock<ISearchService>();

        _service = new ScheduledReportService(
            _context,
            _mockReportService.Object,
            _mockSearchService.Object,
            NullLogger<ScheduledReportService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScheduledReportService(null!, _mockReportService.Object, _mockSearchService.Object, NullLogger<ScheduledReportService>.Instance));
    }

    [Fact]
    public async Task CreateScheduledReportAsync_WithValidData_CreatesReport()
    {
        // Act
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Daily Market Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Assert
        Assert.NotNull(schedule);
        Assert.Equal("Daily Market Report", schedule.Name);
        Assert.Equal(ReportFormat.Pdf, schedule.Format);
        Assert.Equal(ReportSchedule.Daily, schedule.Schedule);
        Assert.Equal(ScheduledReportStatus.Active, schedule.Status);
        Assert.NotNull(schedule.NextRunAt);
    }

    [Fact]
    public async Task CreateScheduledReportAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateScheduledReportAsync(
                _testTenantId,
                "Test Report",
                ReportFormat.Html,
                ReportSchedule.Weekly,
                TimeSpan.FromHours(8),
                "/reports",
                scheduledDayOfWeek: DayOfWeek.Monday));
    }

    [Fact]
    public async Task GetScheduledReportAsync_WithExistingId_ReturnsReport()
    {
        // Arrange
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        var result = await _service.GetScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Report", result.Name);
    }

    [Fact]
    public async Task GetAllScheduledReportsAsync_ReturnsAllReportsForTenant()
    {
        // Arrange
        await _service.CreateScheduledReportAsync(_testTenantId, "Report 1", ReportFormat.Pdf, ReportSchedule.Daily, TimeSpan.FromHours(6), "/reports");
        await _service.CreateScheduledReportAsync(_testTenantId, "Report 2", ReportFormat.Html, ReportSchedule.Daily, TimeSpan.FromHours(7), "/reports");

        // Act
        var result = await _service.GetAllScheduledReportsAsync(_testTenantId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDueScheduledReportsAsync_ReturnsDueReports()
    {
        // Arrange
        var schedule = ScheduledReport.Create(
            _testTenantId,
            "Due Report",
            ReportFormat.Pdf,
            ReportSchedule.Once,
            TimeSpan.FromHours(0),
            "/reports");

        // Manually set next run to past
        var nextRunField = typeof(ScheduledReport).GetProperty("NextRunAt");
        if (nextRunField != null)
        {
            // Use reflection to set the next run time in the past
            schedule.GetType().GetProperty("NextRunAt")!
                .SetValue(schedule, DateTime.UtcNow.AddMinutes(-5));
        }

        _context.ScheduledReports.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDueScheduledReportsAsync();

        // Assert - Should find the due report
        Assert.Contains(result, r => r.Name == "Due Report");
    }

    [Fact]
    public async Task PauseScheduledReportAsync_WithExistingReport_ReturnsTrue()
    {
        // Arrange
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        var result = await _service.PauseScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);
        Assert.Equal(ScheduledReportStatus.Paused, updated!.Status);
    }

    [Fact]
    public async Task ResumeScheduledReportAsync_WithPausedReport_ReturnsTrue()
    {
        // Arrange
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");
        await _service.PauseScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Act
        var result = await _service.ResumeScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);
        Assert.Equal(ScheduledReportStatus.Active, updated!.Status);
    }

    [Fact]
    public async Task CancelScheduledReportAsync_WithExistingReport_ReturnsTrue()
    {
        // Arrange
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        var result = await _service.CancelScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Assert
        Assert.True(result);
        var updated = await _service.GetScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);
        Assert.Equal(ScheduledReportStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task DeleteScheduledReportAsync_WithExistingReport_ReturnsTrue()
    {
        // Arrange
        var schedule = await _service.CreateScheduledReportAsync(
            _testTenantId,
            "Test Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        var result = await _service.DeleteScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);

        // Assert
        Assert.True(result);
        var deleted = await _service.GetScheduledReportAsync(_testTenantId, schedule.ScheduledReportId);
        Assert.Null(deleted);
    }
}

public class ScheduledReportModelTests
{
    [Fact]
    public void Create_WithValidDailySchedule_CreatesReport()
    {
        // Act
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Daily Report",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Assert
        Assert.NotNull(report);
        Assert.Equal("Daily Report", report.Name);
        Assert.Equal(ReportSchedule.Daily, report.Schedule);
        Assert.Equal(ScheduledReportStatus.Active, report.Status);
        Assert.NotNull(report.NextRunAt);
    }

    [Fact]
    public void Create_WithWeeklyScheduleButNoDayOfWeek_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ScheduledReport.Create(
                Guid.NewGuid(),
                "Weekly Report",
                ReportFormat.Pdf,
                ReportSchedule.Weekly,
                TimeSpan.FromHours(6),
                "/reports"));
    }

    [Fact]
    public void Create_WithMonthlyScheduleButNoDayOfMonth_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ScheduledReport.Create(
                Guid.NewGuid(),
                "Monthly Report",
                ReportFormat.Pdf,
                ReportSchedule.Monthly,
                TimeSpan.FromHours(6),
                "/reports"));
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ScheduledReport.Create(
                Guid.NewGuid(),
                "",
                ReportFormat.Pdf,
                ReportSchedule.Daily,
                TimeSpan.FromHours(6),
                "/reports"));
    }

    [Fact]
    public void Pause_SetsStatusToPaused()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        report.Pause();

        // Assert
        Assert.Equal(ScheduledReportStatus.Paused, report.Status);
    }

    [Fact]
    public void Resume_SetsStatusToActive()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");
        report.Pause();

        // Act
        report.Resume();

        // Assert
        Assert.Equal(ScheduledReportStatus.Active, report.Status);
    }

    [Fact]
    public void Cancel_SetsStatusToCancelledAndClearsNextRun()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        report.Cancel();

        // Assert
        Assert.Equal(ScheduledReportStatus.Cancelled, report.Status);
        Assert.Null(report.NextRunAt);
    }

    [Fact]
    public void RecordSuccessfulRun_UpdatesRunCounts()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        report.RecordSuccessfulRun(DateTime.UtcNow);

        // Assert
        Assert.Equal(1, report.SuccessfulRuns);
        Assert.NotNull(report.LastRunAt);
    }

    [Fact]
    public void RecordFailedRun_UpdatesFailedCountAndStatus()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        report.RecordFailedRun(DateTime.UtcNow, "Test error");

        // Assert
        Assert.Equal(1, report.FailedRuns);
        Assert.Equal(ScheduledReportStatus.Error, report.Status);
        Assert.Equal("Test error", report.LastErrorMessage);
    }

    [Fact]
    public void GetOutputFilename_ReturnsCorrectFilename()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Daily Market",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports");

        // Act
        var filename = report.GetOutputFilename();

        // Assert
        Assert.EndsWith(".pdf", filename);
        Assert.Contains("Daily_Market", filename);
    }

    [Fact]
    public void GetEmailRecipientList_ReturnsCorrectList()
    {
        // Arrange
        var report = ScheduledReport.Create(
            Guid.NewGuid(),
            "Test",
            ReportFormat.Pdf,
            ReportSchedule.Daily,
            TimeSpan.FromHours(6),
            "/reports",
            emailRecipients: "user1@test.com, user2@test.com, user3@test.com");

        // Act
        var recipients = report.GetEmailRecipientList();

        // Assert
        Assert.Equal(3, recipients.Count);
        Assert.Contains("user1@test.com", recipients);
        Assert.Contains("user2@test.com", recipients);
        Assert.Contains("user3@test.com", recipients);
    }
}
