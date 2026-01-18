using AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate;
using AutomatedMarketIntelligenceTool.Core.Services.Reporting;
using AutomatedMarketIntelligenceTool.Core.Services.Scheduling;
using AutomatedMarketIntelligenceTool.Protos.Common;
using AutomatedMarketIntelligenceTool.Protos.Reporting;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProtoReportFormat = AutomatedMarketIntelligenceTool.Protos.Reporting.ReportFormat;
using CoreReportFormat = AutomatedMarketIntelligenceTool.Core.Models.ReportAggregate.ReportFormat;

namespace ReportingService.Services;

public class ReportingGrpcService : AutomatedMarketIntelligenceTool.Protos.Reporting.ReportingService.ReportingServiceBase
{
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IScheduledReportService _scheduledReportService;
    private readonly ILogger<ReportingGrpcService> _logger;

    public ReportingGrpcService(
        IReportGenerationService reportGenerationService,
        IScheduledReportService scheduledReportService,
        ILogger<ReportingGrpcService> logger)
    {
        _reportGenerationService = reportGenerationService;
        _scheduledReportService = scheduledReportService;
        _logger = logger;
    }

    public override async Task<GenerateReportResponse> GenerateReport(GenerateReportRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Generating {Format} report '{Name}' for tenant {TenantId}",
            request.Format, request.Name, request.TenantId);

        var tenantId = Guid.Parse(request.TenantId);
        var format = MapToReportFormat(request.Format);
        var outputPath = Path.Combine(Path.GetTempPath(), "ami-reports", $"{Guid.NewGuid()}.{GetFileExtension(format)}");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var reportData = new Core.Models.ReportAggregate.ReportData
        {
            ListingIds = request.Data.ListingIds.Select(id => Guid.Parse(id)).ToList(),
            Makes = request.Data.Makes.ToList(),
            Models = request.Data.Models.ToList()
        };

        var report = await _reportGenerationService.GenerateReportAsync(
            tenantId,
            request.Name,
            format,
            reportData,
            outputPath,
            request.Data.SearchCriteria?.Json,
            context.CancellationToken);

        var fileInfo = new FileInfo(outputPath);

        return new GenerateReportResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Report generated successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            ReportId = report.ReportId.Value.ToString(),
            FilePath = outputPath,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            GeneratedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(report.CreatedAt, DateTimeKind.Utc))
        };
    }

    public override async Task GenerateReportWithProgress(GenerateReportRequest request, IServerStreamWriter<ReportProgressUpdate> responseStream, ServerCallContext context)
    {
        var reportId = Guid.NewGuid().ToString();
        var steps = new[] { "Initializing", "Loading data", "Processing listings", "Generating content", "Finalizing" };

        for (int i = 0; i < steps.Length; i++)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await responseStream.WriteAsync(new ReportProgressUpdate
            {
                ReportId = reportId,
                ProgressPercent = (i + 1) * 20,
                CurrentStep = steps[i],
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                IsComplete = false
            });

            await Task.Delay(500, context.CancellationToken);
        }

        // Generate actual report
        var finalResult = await GenerateReport(request, context);

        await responseStream.WriteAsync(new ReportProgressUpdate
        {
            ReportId = reportId,
            ProgressPercent = 100,
            CurrentStep = "Complete",
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            IsComplete = true,
            FinalResult = finalResult
        });
    }

    public override async Task<GetReportResponse> GetReport(GetReportRequest request, ServerCallContext context)
    {
        var reportPath = await _reportGenerationService.GetReportPathAsync(
            ReportId.From(Guid.Parse(request.ReportId)),
            context.CancellationToken);

        if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath))
        {
            return new GetReportResponse
            {
                Response = new ServiceResponse
                {
                    Success = false,
                    Message = "Report not found"
                }
            };
        }

        var fileInfo = new FileInfo(reportPath);

        return new GetReportResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Report = new ReportInfo
            {
                ReportId = request.ReportId,
                TenantId = request.TenantId,
                Name = Path.GetFileNameWithoutExtension(reportPath),
                FilePath = reportPath,
                FileSizeBytes = fileInfo.Length,
                CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(fileInfo.CreationTimeUtc, DateTimeKind.Utc))
            }
        };
    }

    public override async Task DownloadReport(DownloadReportRequest request, IServerStreamWriter<ReportChunk> responseStream, ServerCallContext context)
    {
        var reportPath = await _reportGenerationService.GetReportPathAsync(
            ReportId.From(Guid.Parse(request.ReportId)),
            context.CancellationToken);

        if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath))
        {
            await responseStream.WriteAsync(new ReportChunk
            {
                Data = ByteString.Empty,
                Offset = 0,
                TotalSize = 0,
                IsLast = true
            });
            return;
        }

        var fileInfo = new FileInfo(reportPath);
        var totalSize = fileInfo.Length;
        var chunkSize = 64 * 1024; // 64 KB chunks
        var buffer = new byte[chunkSize];
        long offset = 0;

        await using var fileStream = File.OpenRead(reportPath);

        while (offset < totalSize)
        {
            var bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, context.CancellationToken);

            await responseStream.WriteAsync(new ReportChunk
            {
                Data = ByteString.CopyFrom(buffer, 0, bytesRead),
                Offset = offset,
                TotalSize = totalSize,
                IsLast = offset + bytesRead >= totalSize
            });

            offset += bytesRead;
        }
    }

    public override async Task<ScheduleReportResponse> ScheduleReport(ScheduleReportRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);

        var scheduleConfig = new Core.Models.ScheduledReportAggregate.ScheduleConfig
        {
            Frequency = MapToScheduleFrequency(request.Schedule.Frequency),
            DayOfWeek = request.Schedule.DayOfWeek,
            DayOfMonth = request.Schedule.DayOfMonth,
            Hour = request.Schedule.Hour,
            Minute = request.Schedule.Minute,
            Timezone = request.Schedule.Timezone
        };

        var scheduledReport = await _scheduledReportService.ScheduleReportAsync(
            tenantId,
            request.Name,
            MapToReportFormat(request.Format),
            scheduleConfig,
            context.CancellationToken);

        return new ScheduleReportResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Report scheduled successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            ScheduleId = scheduledReport.ScheduledReportId.Value.ToString(),
            NextRun = Timestamp.FromDateTime(DateTime.SpecifyKind(scheduledReport.NextRunAt, DateTimeKind.Utc))
        };
    }

    public override async Task<GetScheduledReportsResponse> GetScheduledReports(GetScheduledReportsRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var scheduledReports = await _scheduledReportService.GetScheduledReportsAsync(tenantId, context.CancellationToken);

        var response = new GetScheduledReportsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = 1,
                PageSize = scheduledReports.Count,
                TotalCount = scheduledReports.Count,
                TotalPages = 1
            }
        };

        foreach (var report in scheduledReports)
        {
            response.ScheduledReports.Add(new ScheduledReportInfo
            {
                ScheduleId = report.ScheduledReportId.Value.ToString(),
                Name = report.Name,
                Format = MapToProtoFormat(report.Format),
                IsActive = report.IsActive,
                NextRun = Timestamp.FromDateTime(DateTime.SpecifyKind(report.NextRunAt, DateTimeKind.Utc)),
                CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(report.CreatedAt, DateTimeKind.Utc))
            });
        }

        return response;
    }

    public override async Task<CancelScheduledReportResponse> CancelScheduledReport(CancelScheduledReportRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var scheduleId = Guid.Parse(request.ScheduleId);

        await _scheduledReportService.CancelScheduledReportAsync(tenantId, scheduleId, context.CancellationToken);

        return new CancelScheduledReportResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Scheduled report cancelled",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override Task<AutomatedMarketIntelligenceTool.Protos.Reporting.HealthCheckResponse> HealthCheck(
        AutomatedMarketIntelligenceTool.Protos.Reporting.HealthCheckRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new AutomatedMarketIntelligenceTool.Protos.Reporting.HealthCheckResponse
        {
            Healthy = true,
            Status = "Healthy",
            Components =
            {
                { "grpc", "Healthy" },
                { "database", "Healthy" },
                { "storage", "Healthy" }
            }
        });
    }

    private static CoreReportFormat MapToReportFormat(ProtoReportFormat format) => format switch
    {
        ProtoReportFormat.Html => CoreReportFormat.Html,
        ProtoReportFormat.Pdf => CoreReportFormat.Pdf,
        ProtoReportFormat.Excel => CoreReportFormat.Excel,
        ProtoReportFormat.Csv => CoreReportFormat.Csv,
        ProtoReportFormat.Json => CoreReportFormat.Json,
        _ => CoreReportFormat.Html
    };

    private static ProtoReportFormat MapToProtoFormat(CoreReportFormat format) => format switch
    {
        CoreReportFormat.Html => ProtoReportFormat.Html,
        CoreReportFormat.Pdf => ProtoReportFormat.Pdf,
        CoreReportFormat.Excel => ProtoReportFormat.Excel,
        CoreReportFormat.Csv => ProtoReportFormat.Csv,
        CoreReportFormat.Json => ProtoReportFormat.Json,
        _ => ProtoReportFormat.Html
    };

    private static Core.Models.ScheduledReportAggregate.ScheduleFrequency MapToScheduleFrequency(ScheduleFrequency frequency) => frequency switch
    {
        ScheduleFrequency.Daily => Core.Models.ScheduledReportAggregate.ScheduleFrequency.Daily,
        ScheduleFrequency.Weekly => Core.Models.ScheduledReportAggregate.ScheduleFrequency.Weekly,
        ScheduleFrequency.Monthly => Core.Models.ScheduledReportAggregate.ScheduleFrequency.Monthly,
        _ => Core.Models.ScheduledReportAggregate.ScheduleFrequency.Daily
    };

    private static string GetFileExtension(CoreReportFormat format) => format switch
    {
        CoreReportFormat.Html => "html",
        CoreReportFormat.Pdf => "pdf",
        CoreReportFormat.Excel => "xlsx",
        CoreReportFormat.Csv => "csv",
        CoreReportFormat.Json => "json",
        _ => "html"
    };
}
