using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Protos.Common;
using AutomatedMarketIntelligenceTool.Protos.Deduplication;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using CoreMatchType = AutomatedMarketIntelligenceTool.Core.Services.DuplicateMatchType;
using ProtoMatchType = AutomatedMarketIntelligenceTool.Protos.Deduplication.DuplicateMatchType;

namespace DeduplicationService.Services;

public class DeduplicationGrpcService : AutomatedMarketIntelligenceTool.Protos.Deduplication.DeduplicationService.DeduplicationServiceBase
{
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IDeduplicationConfigService _configService;
    private readonly IDeduplicationAuditService _auditService;
    private readonly IAccuracyMetricsService _metricsService;
    private readonly IReviewService _reviewService;
    private readonly ILogger<DeduplicationGrpcService> _logger;

    public DeduplicationGrpcService(
        IDuplicateDetectionService duplicateDetectionService,
        IDeduplicationConfigService configService,
        IDeduplicationAuditService auditService,
        IAccuracyMetricsService metricsService,
        IReviewService reviewService,
        ILogger<DeduplicationGrpcService> logger)
    {
        _duplicateDetectionService = duplicateDetectionService;
        _configService = configService;
        _auditService = auditService;
        _metricsService = metricsService;
        _reviewService = reviewService;
        _logger = logger;
    }

    public override async Task<CheckDuplicateResponse> CheckDuplicate(CheckDuplicateRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Checking duplicate for listing {ExternalId} from {Source}",
            request.ScrapedListing.ExternalId, request.ScrapedListing.SourceSite);

        var tenantId = Guid.Parse(request.TenantId);
        var scrapedInfo = MapToScrapedListingInfo(request.ScrapedListing, tenantId);
        var options = MapToDetectionOptions(request.Options);

        var result = await _duplicateDetectionService.CheckForDuplicateAsync(scrapedInfo, options, context.CancellationToken);

        return new CheckDuplicateResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            IsDuplicate = result.IsDuplicate,
            MatchType = MapToProtoMatchType(result.MatchType),
            ExistingListingId = result.ExistingListingId?.ToString(),
            ConfidenceScore = result.ConfidenceScore,
            RequiresReview = result.RequiresReview,
            FuzzyMatchDetails = result.FuzzyMatchDetails != null ? MapToProtoFuzzyDetails(result.FuzzyMatchDetails) : null
        };
    }

    public override async Task<BatchCheckDuplicatesResponse> BatchCheckDuplicates(BatchCheckDuplicatesRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var options = MapToDetectionOptions(request.Options);

        var response = new BatchCheckDuplicatesResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        foreach (var scrapedListing in request.ScrapedListings)
        {
            var scrapedInfo = MapToScrapedListingInfo(scrapedListing, tenantId);
            var result = await _duplicateDetectionService.CheckForDuplicateAsync(scrapedInfo, options, context.CancellationToken);

            var checkResponse = new CheckDuplicateResponse
            {
                Response = new ServiceResponse { Success = true },
                IsDuplicate = result.IsDuplicate,
                MatchType = MapToProtoMatchType(result.MatchType),
                ExistingListingId = result.ExistingListingId?.ToString(),
                ConfidenceScore = result.ConfidenceScore,
                RequiresReview = result.RequiresReview
            };

            response.Results.Add(checkResponse);

            if (result.IsDuplicate)
                response.DuplicatesFound++;
            if (result.RequiresReview)
                response.ReviewRequired++;
        }

        response.TotalChecked = request.ScrapedListings.Count;
        return response;
    }

    public override async Task<GetConfigurationResponse> GetConfiguration(GetConfigurationRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var config = await _configService.GetConfigurationAsync(tenantId, context.CancellationToken);

        return new GetConfigurationResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Configuration = new DeduplicationConfiguration
            {
                ConfigId = config.ConfigId.ToString(),
                TenantId = config.TenantId.ToString(),
                EnableFuzzyMatching = config.EnableFuzzyMatching,
                EnableImageMatching = config.EnableImageMatching,
                AutoMatchThreshold = config.AutoMatchThreshold,
                ReviewThreshold = config.ReviewThreshold,
                MileageTolerance = config.MileageTolerance,
                PriceTolerance = (double)config.PriceTolerance,
                MakeModelWeight = config.MakeModelWeight,
                YearWeight = config.YearWeight,
                MileageWeight = config.MileageWeight,
                PriceWeight = config.PriceWeight,
                LocationWeight = config.LocationWeight,
                ImageWeight = config.ImageWeight,
                UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(config.UpdatedAt, DateTimeKind.Utc))
            }
        };
    }

    public override async Task<UpdateConfigurationResponse> UpdateConfiguration(UpdateConfigurationRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);

        var updatedConfig = await _configService.UpdateConfigurationAsync(
            tenantId,
            new Core.Models.DeduplicationConfigAggregate.DeduplicationConfig
            {
                EnableFuzzyMatching = request.Configuration.EnableFuzzyMatching,
                EnableImageMatching = request.Configuration.EnableImageMatching,
                AutoMatchThreshold = request.Configuration.AutoMatchThreshold,
                ReviewThreshold = request.Configuration.ReviewThreshold,
                MileageTolerance = request.Configuration.MileageTolerance,
                PriceTolerance = (decimal)request.Configuration.PriceTolerance,
                MakeModelWeight = request.Configuration.MakeModelWeight,
                YearWeight = request.Configuration.YearWeight,
                MileageWeight = request.Configuration.MileageWeight,
                PriceWeight = request.Configuration.PriceWeight,
                LocationWeight = request.Configuration.LocationWeight,
                ImageWeight = request.Configuration.ImageWeight
            },
            context.CancellationToken);

        return new UpdateConfigurationResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Configuration updated successfully",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Configuration = new DeduplicationConfiguration
            {
                ConfigId = updatedConfig.ConfigId.ToString(),
                TenantId = updatedConfig.TenantId.ToString(),
                EnableFuzzyMatching = updatedConfig.EnableFuzzyMatching,
                EnableImageMatching = updatedConfig.EnableImageMatching,
                AutoMatchThreshold = updatedConfig.AutoMatchThreshold,
                ReviewThreshold = updatedConfig.ReviewThreshold,
                MileageTolerance = updatedConfig.MileageTolerance,
                PriceTolerance = (double)updatedConfig.PriceTolerance,
                UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(updatedConfig.UpdatedAt, DateTimeKind.Utc))
            }
        };
    }

    public override async Task<GetAuditTrailResponse> GetAuditTrail(GetAuditTrailRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var fromDate = request.FromDate?.ToDateTime();
        var toDate = request.ToDate?.ToDateTime();
        var page = request.Pagination?.Page ?? 1;
        var pageSize = request.Pagination?.PageSize ?? 20;

        var auditEntries = await _auditService.GetAuditTrailAsync(tenantId, fromDate, toDate, page, pageSize, context.CancellationToken);

        var response = new GetAuditTrailResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = auditEntries.TotalCount,
                TotalPages = auditEntries.TotalPages
            }
        };

        foreach (var entry in auditEntries.Entries)
        {
            response.Entries.Add(new AuditEntry
            {
                EntryId = entry.EntryId.ToString(),
                TenantId = entry.TenantId.ToString(),
                ListingId = entry.ListingId.ToString(),
                MatchedListingId = entry.MatchedListingId?.ToString(),
                MatchType = MapToProtoMatchType(entry.MatchType),
                ConfidenceScore = entry.ConfidenceScore,
                Decision = entry.Decision,
                DecidedBy = entry.DecidedBy,
                CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc))
            });
        }

        return response;
    }

    public override async Task<GetAccuracyMetricsResponse> GetAccuracyMetrics(GetAccuracyMetricsRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var fromDate = request.FromDate?.ToDateTime();
        var toDate = request.ToDate?.ToDateTime();

        var metrics = await _metricsService.GetMetricsAsync(tenantId, fromDate, toDate, context.CancellationToken);

        var response = new GetAccuracyMetricsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            TotalChecks = metrics.TotalChecks,
            AutoMatched = metrics.AutoMatched,
            ReviewItems = metrics.ReviewItems,
            ConfirmedDuplicates = metrics.ConfirmedDuplicates,
            FalsePositives = metrics.FalsePositives,
            FalseNegatives = metrics.FalseNegatives,
            Precision = metrics.Precision,
            Recall = metrics.Recall,
            F1Score = metrics.F1Score
        };

        return response;
    }

    public override async Task<GetReviewQueueResponse> GetReviewQueue(GetReviewQueueRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var page = request.Pagination?.Page ?? 1;
        var pageSize = request.Pagination?.PageSize ?? 20;

        var reviewItems = await _reviewService.GetReviewQueueAsync(tenantId, page, pageSize, context.CancellationToken);

        var response = new GetReviewQueueResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = reviewItems.TotalCount,
                TotalPages = reviewItems.TotalPages
            }
        };

        foreach (var item in reviewItems.Items)
        {
            response.Items.Add(new ReviewQueueItem
            {
                ItemId = item.ItemId.ToString(),
                MatchType = MapToProtoMatchType(item.MatchType),
                ConfidenceScore = item.ConfidenceScore,
                CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc))
            });
        }

        return response;
    }

    public override async Task<ResolveReviewItemResponse> ResolveReviewItem(ResolveReviewItemRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var itemId = Guid.Parse(request.ItemId);

        await _reviewService.ResolveReviewItemAsync(
            tenantId,
            itemId,
            MapToReviewDecision(request.Decision),
            request.Notes?.Value,
            context.CancellationToken);

        return new ResolveReviewItemResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Review item resolved",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override async Task StreamDuplicateCheck(IAsyncStreamReader<CheckDuplicateRequest> requestStream, IServerStreamWriter<CheckDuplicateResponse> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var result = await CheckDuplicate(request, context);
            await responseStream.WriteAsync(result);
        }
    }

    public override Task<AutomatedMarketIntelligenceTool.Protos.Deduplication.HealthCheckResponse> HealthCheck(
        AutomatedMarketIntelligenceTool.Protos.Deduplication.HealthCheckRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new AutomatedMarketIntelligenceTool.Protos.Deduplication.HealthCheckResponse
        {
            Healthy = true,
            Status = "Healthy",
            Components =
            {
                { "grpc", "Healthy" },
                { "database", "Healthy" },
                { "fuzzy-matching", "Healthy" }
            }
        });
    }

    private static Core.Services.ScrapedListingInfo MapToScrapedListingInfo(Protos.Deduplication.ScrapedListingInfo proto, Guid tenantId)
    {
        return new Core.Services.ScrapedListingInfo
        {
            ExternalId = proto.ExternalId,
            SourceSite = proto.SourceSite,
            Vin = proto.Vin?.Value,
            TenantId = tenantId,
            Make = proto.Make?.Value,
            Model = proto.Model?.Value,
            Year = proto.Year?.Value,
            Mileage = proto.Mileage?.Value,
            Price = proto.Price != null ? (decimal)proto.Price.Value : null,
            City = proto.City?.Value,
            ImageUrls = proto.ImageUrls.ToList()
        };
    }

    private static Core.Services.DuplicateDetectionOptions MapToDetectionOptions(Protos.Deduplication.DuplicateDetectionOptions? proto)
    {
        if (proto == null)
            return Core.Services.DuplicateDetectionOptions.Default;

        return new Core.Services.DuplicateDetectionOptions
        {
            EnableFuzzyMatching = proto.EnableFuzzyMatching,
            EnableImageMatching = proto.EnableImageMatching,
            AutoMatchThreshold = proto.AutoMatchThreshold,
            ReviewThreshold = proto.ReviewThreshold,
            MileageTolerance = proto.MileageTolerance,
            PriceTolerance = (decimal)proto.PriceTolerance
        };
    }

    private static ProtoMatchType MapToProtoMatchType(CoreMatchType matchType) => matchType switch
    {
        CoreMatchType.VinMatch => ProtoMatchType.VinMatch,
        CoreMatchType.ExternalIdMatch => ProtoMatchType.ExternalIdMatch,
        CoreMatchType.FuzzyMatch => ProtoMatchType.FuzzyMatch,
        CoreMatchType.ImageMatch => ProtoMatchType.ImageMatch,
        CoreMatchType.CombinedMatch => ProtoMatchType.CombinedMatch,
        _ => ProtoMatchType.DuplicateMatchTypeNone
    };

    private static Protos.Deduplication.FuzzyMatchDetails MapToProtoFuzzyDetails(Core.Services.FuzzyMatchDetails details)
    {
        return new Protos.Deduplication.FuzzyMatchDetails
        {
            MakeModelScore = details.MakeModelScore,
            YearScore = details.YearScore,
            MileageScore = details.MileageScore,
            PriceScore = details.PriceScore,
            LocationScore = details.LocationScore,
            ImageScore = details.ImageScore,
            OverallScore = details.OverallScore
        };
    }

    private static Core.Services.ReviewDecision MapToReviewDecision(ReviewDecision decision) => decision switch
    {
        ReviewDecision.ConfirmDuplicate => Core.Services.ReviewDecision.ConfirmDuplicate,
        ReviewDecision.NotDuplicate => Core.Services.ReviewDecision.NotDuplicate,
        ReviewDecision.MergeListings => Core.Services.ReviewDecision.MergeListings,
        ReviewDecision.Skip => Core.Services.ReviewDecision.Skip,
        _ => Core.Services.ReviewDecision.Skip
    };
}
