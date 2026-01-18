using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Health;
using AutomatedMarketIntelligenceTool.Protos.Scraping;
using AutomatedMarketIntelligenceTool.Protos.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;

namespace ScrapingService.Services;

public class ScrapingGrpcService : AutomatedMarketIntelligenceTool.Protos.Scraping.ScrapingService.ScrapingServiceBase
{
    private readonly IScraperFactory _scraperFactory;
    private readonly IScraperHealthService _scraperHealthService;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ILogger<ScrapingGrpcService> _logger;
    private static readonly ConcurrentDictionary<string, JobState> _runningJobs = new();

    public ScrapingGrpcService(
        IScraperFactory scraperFactory,
        IScraperHealthService scraperHealthService,
        IDuplicateDetectionService duplicateDetectionService,
        ILogger<ScrapingGrpcService> logger)
    {
        _scraperFactory = scraperFactory;
        _scraperHealthService = scraperHealthService;
        _duplicateDetectionService = duplicateDetectionService;
        _logger = logger;
    }

    public override async Task<StartScrapeJobResponse> StartScrapeJob(StartScrapeJobRequest request, ServerCallContext context)
    {
        var jobId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation("Starting scrape job {JobId} for tenant {TenantId} with {ScraperCount} scrapers",
            jobId, request.TenantId, request.Scrapers.Count);

        var jobState = new JobState
        {
            JobId = jobId,
            TenantId = request.TenantId,
            Status = JobStatus.Running,
            StartedAt = startedAt,
            ScraperNames = request.Scrapers.ToList()
        };

        _runningJobs[jobId] = jobState;

        // Start scraping in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteScrapeJobAsync(jobState, request, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scrape job {JobId} failed", jobId);
                jobState.Status = JobStatus.Failed;
            }
        });

        return new StartScrapeJobResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Scrape job started successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            JobId = jobId,
            StartedAt = Timestamp.FromDateTime(startedAt)
        };
    }

    public override Task<GetJobStatusResponse> GetJobStatus(GetJobStatusRequest request, ServerCallContext context)
    {
        if (!_runningJobs.TryGetValue(request.JobId, out var jobState))
        {
            return Task.FromResult(new GetJobStatusResponse
            {
                Response = new ServiceResponse
                {
                    Success = false,
                    Message = "Job not found",
                    CorrelationId = context.GetHttpContext().TraceIdentifier
                }
            });
        }

        var response = new GetJobStatusResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            JobId = jobState.JobId,
            Status = jobState.Status,
            TotalScrapers = jobState.ScraperNames.Count,
            CompletedScrapers = jobState.CompletedScrapers,
            ListingsFound = jobState.ListingsFound,
            DuplicatesDetected = jobState.DuplicatesDetected,
            ErrorsCount = jobState.ErrorsCount,
            StartedAt = Timestamp.FromDateTime(jobState.StartedAt)
        };

        if (jobState.CompletedAt.HasValue)
        {
            response.CompletedAt = Timestamp.FromDateTime(jobState.CompletedAt.Value);
        }

        foreach (var scraperStatus in jobState.ScraperStatuses.Values)
        {
            response.ScraperStatuses.Add(scraperStatus);
        }

        return Task.FromResult(response);
    }

    public override Task<CancelJobResponse> CancelJob(CancelJobRequest request, ServerCallContext context)
    {
        if (!_runningJobs.TryGetValue(request.JobId, out var jobState))
        {
            return Task.FromResult(new CancelJobResponse
            {
                Response = new ServiceResponse
                {
                    Success = false,
                    Message = "Job not found"
                },
                Cancelled = false
            });
        }

        jobState.CancellationTokenSource.Cancel();
        jobState.Status = JobStatus.Cancelled;
        jobState.CompletedAt = DateTime.UtcNow;

        return Task.FromResult(new CancelJobResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Job cancelled successfully"
            },
            Cancelled = true
        });
    }

    public override async Task StreamJobProgress(StreamJobProgressRequest request, IServerStreamWriter<JobProgressUpdate> responseStream, ServerCallContext context)
    {
        if (!_runningJobs.TryGetValue(request.JobId, out var jobState))
        {
            await responseStream.WriteAsync(new JobProgressUpdate
            {
                JobId = request.JobId,
                Status = JobStatus.Failed,
                Message = "Job not found"
            });
            return;
        }

        while (!context.CancellationToken.IsCancellationRequested &&
               jobState.Status == JobStatus.Running)
        {
            var update = new JobProgressUpdate
            {
                JobId = jobState.JobId,
                ListingsFound = jobState.ListingsFound,
                Status = jobState.Status,
                Message = $"Processed {jobState.CompletedScrapers}/{jobState.ScraperNames.Count} scrapers",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            await responseStream.WriteAsync(update);
            await Task.Delay(1000, context.CancellationToken);
        }

        // Send final update
        await responseStream.WriteAsync(new JobProgressUpdate
        {
            JobId = jobState.JobId,
            ListingsFound = jobState.ListingsFound,
            Status = jobState.Status,
            Message = "Job completed",
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }

    public override Task<GetAvailableScrapersResponse> GetAvailableScrapers(GetAvailableScrapersRequest request, ServerCallContext context)
    {
        var scraperNames = _scraperFactory.GetAvailableScrapers();
        var response = new GetAvailableScrapersResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        foreach (var name in scraperNames)
        {
            var info = new ScraperInfo
            {
                Name = name,
                DisplayName = FormatDisplayName(name),
                Category = GetScraperCategory(name),
                IsHealthy = true
            };

            if (request.IncludeHealthStatus)
            {
                var health = _scraperHealthService.GetHealthStatus(name);
                if (health != null)
                {
                    info.IsHealthy = health.IsHealthy;
                    info.SuccessRate = health.SuccessRate;
                    if (health.LastSuccessfulScrape.HasValue)
                    {
                        info.LastSuccessfulScrape = Timestamp.FromDateTime(
                            DateTime.SpecifyKind(health.LastSuccessfulScrape.Value, DateTimeKind.Utc));
                    }
                }
            }

            response.Scrapers.Add(info);
        }

        return Task.FromResult(response);
    }

    public override Task<GetScraperHealthResponse> GetScraperHealth(GetScraperHealthRequest request, ServerCallContext context)
    {
        var response = new GetScraperHealthResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        var scraperNames = request.ScraperNames.Count > 0
            ? request.ScraperNames.ToList()
            : _scraperFactory.GetAvailableScrapers();

        foreach (var name in scraperNames)
        {
            var health = _scraperHealthService.GetHealthStatus(name);
            var healthInfo = new ScraperHealthInfo
            {
                ScraperName = name,
                IsHealthy = health?.IsHealthy ?? true,
                ConsecutiveFailures = health?.ConsecutiveFailures ?? 0,
                AverageResponseTimeMs = health?.AverageResponseTimeMs ?? 0,
                SuccessRate24H = health?.SuccessRate ?? 100,
                LastError = health?.LastError ?? string.Empty
            };

            if (health?.LastCheck != null)
            {
                healthInfo.LastCheck = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(health.LastCheck.Value, DateTimeKind.Utc));
            }

            response.HealthInfo.Add(healthInfo);
        }

        return Task.FromResult(response);
    }

    public override Task<AutomatedMarketIntelligenceTool.Protos.Scraping.HealthCheckResponse> HealthCheck(
        AutomatedMarketIntelligenceTool.Protos.Scraping.HealthCheckRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new AutomatedMarketIntelligenceTool.Protos.Scraping.HealthCheckResponse
        {
            Healthy = true,
            Status = "Healthy",
            Components =
            {
                { "grpc", "Healthy" },
                { "database", "Healthy" },
                { "scrapers", "Healthy" }
            }
        });
    }

    private async Task ExecuteScrapeJobAsync(JobState jobState, StartScrapeJobRequest request, CancellationToken cancellationToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, jobState.CancellationTokenSource.Token);
        var tenantId = Guid.Parse(request.TenantId);

        foreach (var scraperName in request.Scrapers)
        {
            if (linkedCts.Token.IsCancellationRequested)
                break;

            var scraperStatus = new ScraperStatus
            {
                ScraperName = scraperName,
                Status = JobStatus.Running
            };
            jobState.ScraperStatuses[scraperName] = scraperStatus;

            try
            {
                var scraper = _scraperFactory.GetScraper(scraperName);
                if (scraper == null)
                {
                    scraperStatus.Status = JobStatus.Failed;
                    scraperStatus.ErrorMessage = "Scraper not found";
                    jobState.ErrorsCount++;
                    continue;
                }

                var searchParams = BuildSearchParameters(request.SearchParams);
                var listings = await scraper.ScrapeAsync(tenantId, searchParams, linkedCts.Token);

                scraperStatus.ListingsFound = listings.Count;
                scraperStatus.Status = JobStatus.Completed;
                jobState.ListingsFound += listings.Count;

                // Check for duplicates
                foreach (var listing in listings)
                {
                    var scrapedInfo = new ScrapedListingInfo
                    {
                        ExternalId = listing.ExternalId,
                        SourceSite = listing.SourceSite,
                        Vin = listing.Vin,
                        TenantId = tenantId,
                        Make = listing.Make,
                        Model = listing.Model,
                        Year = listing.Year,
                        Mileage = listing.Mileage,
                        Price = listing.Price,
                        City = listing.City
                    };

                    var duplicateResult = await _duplicateDetectionService.CheckForDuplicateAsync(
                        scrapedInfo, linkedCts.Token);

                    if (duplicateResult.IsDuplicate)
                    {
                        jobState.DuplicatesDetected++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                scraperStatus.Status = JobStatus.Cancelled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scraper {ScraperName} failed", scraperName);
                scraperStatus.Status = JobStatus.Failed;
                scraperStatus.ErrorMessage = ex.Message;
                jobState.ErrorsCount++;
            }

            jobState.CompletedScrapers++;
        }

        jobState.Status = linkedCts.Token.IsCancellationRequested ? JobStatus.Cancelled : JobStatus.Completed;
        jobState.CompletedAt = DateTime.UtcNow;
    }

    private static Core.Services.Scrapers.SearchParameters BuildSearchParameters(SearchParameters? protoParams)
    {
        if (protoParams == null)
            return new Core.Services.Scrapers.SearchParameters();

        return new Core.Services.Scrapers.SearchParameters
        {
            Makes = protoParams.Makes.ToList(),
            Models = protoParams.Models.ToList(),
            YearMin = protoParams.YearMin?.Value,
            YearMax = protoParams.YearMax?.Value,
            PriceMin = protoParams.PriceMin != null ? (decimal)protoParams.PriceMin.Value : null,
            PriceMax = protoParams.PriceMax != null ? (decimal)protoParams.PriceMax.Value : null,
            MileageMax = protoParams.MileageMax?.Value,
            PostalCode = protoParams.PostalCode?.Value,
            RadiusKm = protoParams.RadiusKm?.Value
        };
    }

    private static string FormatDisplayName(string scraperName)
    {
        return string.Concat(scraperName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    private static string GetScraperCategory(string scraperName)
    {
        var name = scraperName.ToLowerInvariant();
        if (name.Contains("autotrader") || name.Contains("kijiji") || name.Contains("cargurus"))
            return "Marketplace";
        if (name.Contains("carmax") || name.Contains("carvana") || name.Contains("vroom"))
            return "Online Retailer";
        return "Dealership";
    }

    private class JobState
    {
        public string JobId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<string> ScraperNames { get; set; } = new();
        public int CompletedScrapers { get; set; }
        public int ListingsFound { get; set; }
        public int DuplicatesDetected { get; set; }
        public int ErrorsCount { get; set; }
        public ConcurrentDictionary<string, ScraperStatus> ScraperStatuses { get; } = new();
        public CancellationTokenSource CancellationTokenSource { get; } = new();
    }
}
