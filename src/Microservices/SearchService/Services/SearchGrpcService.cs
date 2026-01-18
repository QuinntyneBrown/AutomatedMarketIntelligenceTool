using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate;
using AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums;
using AutomatedMarketIntelligenceTool.Protos.Search;
using AutomatedMarketIntelligenceTool.Protos.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ProtoCondition = AutomatedMarketIntelligenceTool.Protos.Common.Condition;
using ProtoTransmission = AutomatedMarketIntelligenceTool.Protos.Common.Transmission;
using ProtoFuelType = AutomatedMarketIntelligenceTool.Protos.Common.FuelType;
using ProtoBodyStyle = AutomatedMarketIntelligenceTool.Protos.Common.BodyStyle;
using ProtoDrivetrain = AutomatedMarketIntelligenceTool.Protos.Common.Drivetrain;
using ProtoSellerType = AutomatedMarketIntelligenceTool.Protos.Common.SellerType;
using CoreCondition = AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums.Condition;
using CoreTransmission = AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums.Transmission;
using CoreFuelType = AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums.FuelType;
using CoreBodyStyle = AutomatedMarketIntelligenceTool.Core.Models.ListingAggregate.Enums.BodyStyle;

namespace SearchService.Services;

public class SearchGrpcService : AutomatedMarketIntelligenceTool.Protos.Search.SearchService.SearchServiceBase
{
    private readonly ISearchService _searchService;
    private readonly IStatisticsService _statisticsService;
    private readonly IAutoCompleteService _autoCompleteService;
    private readonly IPriceHistoryService _priceHistoryService;
    private readonly IWatchListService _watchListService;
    private readonly ILogger<SearchGrpcService> _logger;

    public SearchGrpcService(
        ISearchService searchService,
        IStatisticsService statisticsService,
        IAutoCompleteService autoCompleteService,
        IPriceHistoryService priceHistoryService,
        IWatchListService watchListService,
        ILogger<SearchGrpcService> logger)
    {
        _searchService = searchService;
        _statisticsService = statisticsService;
        _autoCompleteService = autoCompleteService;
        _priceHistoryService = priceHistoryService;
        _watchListService = watchListService;
        _logger = logger;
    }

    public override async Task<SearchListingsResponse> SearchListings(SearchListingsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Searching listings for tenant {TenantId}", request.TenantId);

        var criteria = BuildSearchCriteria(request);
        var result = await _searchService.SearchListingsAsync(criteria, context.CancellationToken);

        var response = new SearchListingsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            },
            NewListingsCount = result.NewListingsCount,
            PriceChangesCount = result.PriceChangesCount
        };

        foreach (var item in result.Listings)
        {
            var searchResult = new ListingSearchResult
            {
                Listing = MapToProtoListing(item.Listing)
            };

            if (item.DistanceKilometers.HasValue)
            {
                searchResult.DistanceKm = item.DistanceKilometers.Value;
            }

            if (item.PriceChange != null)
            {
                searchResult.PriceChange = new PriceChangeInfo
                {
                    PreviousPrice = (double)item.PriceChange.PreviousPrice,
                    CurrentPrice = (double)item.PriceChange.CurrentPrice,
                    PriceChange_ = (double)item.PriceChange.PriceChange,
                    ChangePercentage = (double)item.PriceChange.ChangePercentage,
                    ChangedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(item.PriceChange.ChangedAt, DateTimeKind.Utc))
                };
            }

            response.Listings.Add(searchResult);
        }

        return response;
    }

    public override async Task<GetListingResponse> GetListing(GetListingRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var listingId = ListingId.From(Guid.Parse(request.ListingId));

        var criteria = new SearchCriteria
        {
            TenantId = tenantId,
            Page = 1,
            PageSize = 1
        };

        var result = await _searchService.SearchListingsAsync(criteria, context.CancellationToken);
        var listing = result.Listings.FirstOrDefault(l => l.Listing.ListingId == listingId);

        if (listing == null)
        {
            return new GetListingResponse
            {
                Response = new ServiceResponse
                {
                    Success = false,
                    Message = "Listing not found",
                    CorrelationId = context.GetHttpContext().TraceIdentifier
                }
            };
        }

        return new GetListingResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Listing = MapToProtoListing(listing.Listing)
        };
    }

    public override async Task<GetStatisticsResponse> GetStatistics(GetStatisticsRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var stats = await _statisticsService.GetStatisticsAsync(tenantId, context.CancellationToken);

        var response = new GetStatisticsResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            TotalListings = stats.TotalListings,
            ActiveListings = stats.ActiveListings,
            NewListingsToday = stats.NewListingsToday,
            PriceChangesToday = stats.PriceChangesToday,
            AveragePrice = stats.AveragePrice,
            MedianPrice = stats.MedianPrice,
            MinPrice = stats.MinPrice,
            MaxPrice = stats.MaxPrice,
            AverageMileage = stats.AverageMileage
        };

        foreach (var makeCount in stats.MakesBreakdown)
        {
            response.MakesBreakdown.Add(new MakeCount
            {
                Make = makeCount.Make,
                Count = makeCount.Count
            });
        }

        foreach (var sourceCount in stats.SourcesBreakdown)
        {
            response.SourcesBreakdown.Add(new SourceCount
            {
                Source = sourceCount.Source,
                Count = sourceCount.Count
            });
        }

        return response;
    }

    public override async Task<GetAutoCompleteResponse> GetAutoComplete(GetAutoCompleteRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var suggestions = await _autoCompleteService.GetSuggestionsAsync(
            tenantId,
            request.Field,
            request.Query,
            request.Limit,
            context.CancellationToken);

        var response = new GetAutoCompleteResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        response.Suggestions.AddRange(suggestions);
        return response;
    }

    public override async Task<GetPriceHistoryResponse> GetPriceHistory(GetPriceHistoryRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var listingId = ListingId.From(Guid.Parse(request.ListingId));

        var history = await _priceHistoryService.GetPriceHistoryAsync(tenantId, listingId, context.CancellationToken);

        var response = new GetPriceHistoryResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };

        foreach (var entry in history)
        {
            var protoEntry = new PriceHistoryEntry
            {
                Price = (double)entry.Price,
                RecordedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entry.RecordedAt, DateTimeKind.Utc))
            };

            if (entry.ChangeFromPrevious.HasValue)
            {
                protoEntry.ChangeFromPrevious = (double)entry.ChangeFromPrevious.Value;
            }

            response.Entries.Add(protoEntry);
        }

        return response;
    }

    public override async Task<AddToWatchlistResponse> AddToWatchlist(AddToWatchlistRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var listingId = ListingId.From(Guid.Parse(request.ListingId));

        var entryId = await _watchListService.AddToWatchlistAsync(
            tenantId,
            listingId,
            request.Notes?.Value,
            context.CancellationToken);

        return new AddToWatchlistResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Added to watchlist",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            WatchlistEntryId = entryId.ToString()
        };
    }

    public override async Task<RemoveFromWatchlistResponse> RemoveFromWatchlist(RemoveFromWatchlistRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var listingId = ListingId.From(Guid.Parse(request.ListingId));

        await _watchListService.RemoveFromWatchlistAsync(tenantId, listingId, context.CancellationToken);

        return new RemoveFromWatchlistResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                Message = "Removed from watchlist",
                CorrelationId = context.GetHttpContext().TraceIdentifier
            }
        };
    }

    public override async Task<GetWatchlistResponse> GetWatchlist(GetWatchlistRequest request, ServerCallContext context)
    {
        var tenantId = Guid.Parse(request.TenantId);
        var page = request.Pagination?.Page ?? 1;
        var pageSize = request.Pagination?.PageSize ?? 20;

        var watchlist = await _watchListService.GetWatchlistAsync(tenantId, page, pageSize, context.CancellationToken);

        var response = new GetWatchlistResponse
        {
            Response = new ServiceResponse
            {
                Success = true,
                CorrelationId = context.GetHttpContext().TraceIdentifier
            },
            Pagination = new PaginationResponse
            {
                Page = watchlist.Page,
                PageSize = watchlist.PageSize,
                TotalCount = watchlist.TotalCount,
                TotalPages = watchlist.TotalPages
            }
        };

        foreach (var entry in watchlist.Entries)
        {
            var watchlistEntry = new WatchlistEntry
            {
                EntryId = entry.EntryId.ToString(),
                Listing = MapToProtoListing(entry.Listing),
                AddedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entry.AddedAt, DateTimeKind.Utc)),
                HasPriceChange = entry.HasPriceChange
            };

            if (entry.Notes != null)
            {
                watchlistEntry.Notes = entry.Notes;
            }

            if (entry.PriceChange != null)
            {
                watchlistEntry.PriceChange = new PriceChangeInfo
                {
                    PreviousPrice = (double)entry.PriceChange.PreviousPrice,
                    CurrentPrice = (double)entry.PriceChange.CurrentPrice,
                    PriceChange_ = (double)entry.PriceChange.PriceChange,
                    ChangePercentage = (double)entry.PriceChange.ChangePercentage,
                    ChangedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(entry.PriceChange.ChangedAt, DateTimeKind.Utc))
                };
            }

            response.Entries.Add(watchlistEntry);
        }

        return response;
    }

    public override Task<AutomatedMarketIntelligenceTool.Protos.Search.HealthCheckResponse> HealthCheck(
        AutomatedMarketIntelligenceTool.Protos.Search.HealthCheckRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new AutomatedMarketIntelligenceTool.Protos.Search.HealthCheckResponse
        {
            Healthy = true,
            Status = "Healthy",
            Components =
            {
                { "grpc", "Healthy" },
                { "database", "Healthy" }
            }
        });
    }

    private static SearchCriteria BuildSearchCriteria(SearchListingsRequest request)
    {
        return new SearchCriteria
        {
            TenantId = Guid.Parse(request.TenantId),
            Makes = request.Makes.Count > 0 ? request.Makes.ToArray() : null,
            Models = request.Models.Count > 0 ? request.Models.ToArray() : null,
            YearMin = request.YearMin?.Value,
            YearMax = request.YearMax?.Value,
            PriceMin = request.PriceMin != null ? (decimal)request.PriceMin.Value : null,
            PriceMax = request.PriceMax != null ? (decimal)request.PriceMax.Value : null,
            MileageMin = request.MileageMin?.Value,
            MileageMax = request.MileageMax?.Value,
            Conditions = request.Conditions.Count > 0
                ? request.Conditions.Select(MapToCondition).ToArray()
                : null,
            Transmissions = request.Transmissions.Count > 0
                ? request.Transmissions.Select(MapToTransmission).ToArray()
                : null,
            FuelTypes = request.FuelTypes.Count > 0
                ? request.FuelTypes.Select(MapToFuelType).ToArray()
                : null,
            BodyStyles = request.BodyStyles.Count > 0
                ? request.BodyStyles.Select(MapToBodyStyle).ToArray()
                : null,
            City = request.City?.Value,
            Province = request.Province?.Value,
            PostalCode = request.PostalCode?.Value,
            RadiusKilometers = request.RadiusKm?.Value,
            SearchLatitude = request.SearchLatitude?.Value,
            SearchLongitude = request.SearchLongitude?.Value,
            Page = request.Pagination?.Page ?? 1,
            PageSize = request.Pagination?.PageSize ?? 30,
            SortBy = MapToSortField(request.SortBy),
            SortDirection = request.SortDirection == SortDirection.SortDescending
                ? Core.Services.SortDirection.Descending
                : Core.Services.SortDirection.Ascending
        };
    }

    private static CoreCondition MapToCondition(ProtoCondition condition) => condition switch
    {
        ProtoCondition.New => CoreCondition.New,
        ProtoCondition.Used => CoreCondition.Used,
        ProtoCondition.CertifiedPreOwned => CoreCondition.CertifiedPreOwned,
        _ => CoreCondition.Used
    };

    private static CoreTransmission MapToTransmission(ProtoTransmission transmission) => transmission switch
    {
        ProtoTransmission.Automatic => CoreTransmission.Automatic,
        ProtoTransmission.Manual => CoreTransmission.Manual,
        ProtoTransmission.Cvt => CoreTransmission.CVT,
        _ => CoreTransmission.Automatic
    };

    private static CoreFuelType MapToFuelType(ProtoFuelType fuelType) => fuelType switch
    {
        ProtoFuelType.Gasoline => CoreFuelType.Gasoline,
        ProtoFuelType.Diesel => CoreFuelType.Diesel,
        ProtoFuelType.Electric => CoreFuelType.Electric,
        ProtoFuelType.Hybrid => CoreFuelType.Hybrid,
        ProtoFuelType.PlugInHybrid => CoreFuelType.PlugInHybrid,
        _ => CoreFuelType.Gasoline
    };

    private static CoreBodyStyle MapToBodyStyle(ProtoBodyStyle bodyStyle) => bodyStyle switch
    {
        ProtoBodyStyle.Sedan => CoreBodyStyle.Sedan,
        ProtoBodyStyle.Suv => CoreBodyStyle.SUV,
        ProtoBodyStyle.Truck => CoreBodyStyle.Truck,
        ProtoBodyStyle.Coupe => CoreBodyStyle.Coupe,
        ProtoBodyStyle.Hatchback => CoreBodyStyle.Hatchback,
        ProtoBodyStyle.Wagon => CoreBodyStyle.Wagon,
        ProtoBodyStyle.Convertible => CoreBodyStyle.Convertible,
        ProtoBodyStyle.Van => CoreBodyStyle.Van,
        ProtoBodyStyle.Minivan => CoreBodyStyle.Minivan,
        _ => CoreBodyStyle.Sedan
    };

    private static SearchSortField? MapToSortField(SortField sortField) => sortField switch
    {
        SortField.Price => SearchSortField.Price,
        SortField.Year => SearchSortField.Year,
        SortField.Mileage => SearchSortField.Mileage,
        SortField.Distance => SearchSortField.Distance,
        SortField.CreatedAt => SearchSortField.CreatedAt,
        _ => null
    };

    private static Protos.Common.Listing MapToProtoListing(Listing listing)
    {
        var protoListing = new Protos.Common.Listing
        {
            ListingId = listing.ListingId.Value.ToString(),
            TenantId = listing.TenantId.ToString(),
            ExternalId = listing.ExternalId,
            SourceSite = listing.SourceSite,
            ListingUrl = listing.ListingUrl,
            Make = listing.Make,
            Model = listing.Model,
            Year = listing.Year,
            Price = (double)listing.Price,
            Currency = listing.Currency,
            Condition = MapToProtoCondition(listing.Condition),
            IsNewListing = listing.IsNewListing,
            IsActive = listing.IsActive,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(listing.CreatedAt, DateTimeKind.Utc)),
            FirstSeenDate = Timestamp.FromDateTime(DateTime.SpecifyKind(listing.FirstSeenDate, DateTimeKind.Utc)),
            LastSeenDate = Timestamp.FromDateTime(DateTime.SpecifyKind(listing.LastSeenDate, DateTimeKind.Utc))
        };

        if (listing.Trim != null) protoListing.Trim = listing.Trim;
        if (listing.Mileage.HasValue) protoListing.Mileage = listing.Mileage.Value;
        if (listing.Vin != null) protoListing.Vin = listing.Vin;
        if (listing.City != null) protoListing.City = listing.City;
        if (listing.Province != null) protoListing.Province = listing.Province;
        if (listing.PostalCode != null) protoListing.PostalCode = listing.PostalCode;
        if (listing.Transmission.HasValue) protoListing.Transmission = MapToProtoTransmission(listing.Transmission.Value);
        if (listing.FuelType.HasValue) protoListing.FuelType = MapToProtoFuelType(listing.FuelType.Value);
        if (listing.BodyStyle.HasValue) protoListing.BodyStyle = MapToProtoBodyStyle(listing.BodyStyle.Value);
        if (listing.Drivetrain.HasValue) protoListing.Drivetrain = MapToProtoDrivetrain(listing.Drivetrain.Value);
        if (listing.ExteriorColor != null) protoListing.ExteriorColor = listing.ExteriorColor;
        if (listing.InteriorColor != null) protoListing.InteriorColor = listing.InteriorColor;
        if (listing.SellerType.HasValue) protoListing.SellerType = MapToProtoSellerType(listing.SellerType.Value);
        if (listing.SellerName != null) protoListing.SellerName = listing.SellerName;
        if (listing.SellerPhone != null) protoListing.SellerPhone = listing.SellerPhone;
        if (listing.Description != null) protoListing.Description = listing.Description;
        if (listing.ListingDate.HasValue) protoListing.ListingDate = Timestamp.FromDateTime(DateTime.SpecifyKind(listing.ListingDate.Value, DateTimeKind.Utc));
        if (listing.DaysOnMarket.HasValue) protoListing.DaysOnMarket = listing.DaysOnMarket.Value;
        if (listing.UpdatedAt.HasValue) protoListing.UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(listing.UpdatedAt.Value, DateTimeKind.Utc));
        if (listing.Latitude.HasValue) protoListing.Latitude = (double)listing.Latitude.Value;
        if (listing.Longitude.HasValue) protoListing.Longitude = (double)listing.Longitude.Value;
        if (listing.DealerId != null) protoListing.DealerId = listing.DealerId.Value.ToString();

        protoListing.ImageUrls.AddRange(listing.ImageUrls);

        return protoListing;
    }

    private static ProtoCondition MapToProtoCondition(CoreCondition condition) => condition switch
    {
        CoreCondition.New => ProtoCondition.New,
        CoreCondition.Used => ProtoCondition.Used,
        CoreCondition.CertifiedPreOwned => ProtoCondition.CertifiedPreOwned,
        _ => ProtoCondition.ConditionUnspecified
    };

    private static ProtoTransmission MapToProtoTransmission(CoreTransmission transmission) => transmission switch
    {
        CoreTransmission.Automatic => ProtoTransmission.Automatic,
        CoreTransmission.Manual => ProtoTransmission.Manual,
        CoreTransmission.CVT => ProtoTransmission.Cvt,
        _ => ProtoTransmission.TransmissionUnspecified
    };

    private static ProtoFuelType MapToProtoFuelType(CoreFuelType fuelType) => fuelType switch
    {
        CoreFuelType.Gasoline => ProtoFuelType.Gasoline,
        CoreFuelType.Diesel => ProtoFuelType.Diesel,
        CoreFuelType.Electric => ProtoFuelType.Electric,
        CoreFuelType.Hybrid => ProtoFuelType.Hybrid,
        CoreFuelType.PlugInHybrid => ProtoFuelType.PlugInHybrid,
        _ => ProtoFuelType.FuelTypeUnspecified
    };

    private static ProtoBodyStyle MapToProtoBodyStyle(CoreBodyStyle bodyStyle) => bodyStyle switch
    {
        CoreBodyStyle.Sedan => ProtoBodyStyle.Sedan,
        CoreBodyStyle.SUV => ProtoBodyStyle.Suv,
        CoreBodyStyle.Truck => ProtoBodyStyle.Truck,
        CoreBodyStyle.Coupe => ProtoBodyStyle.Coupe,
        CoreBodyStyle.Hatchback => ProtoBodyStyle.Hatchback,
        CoreBodyStyle.Wagon => ProtoBodyStyle.Wagon,
        CoreBodyStyle.Convertible => ProtoBodyStyle.Convertible,
        CoreBodyStyle.Van => ProtoBodyStyle.Van,
        CoreBodyStyle.Minivan => ProtoBodyStyle.Minivan,
        _ => ProtoBodyStyle.BodyStyleUnspecified
    };

    private static ProtoDrivetrain MapToProtoDrivetrain(Core.Models.ListingAggregate.Enums.Drivetrain drivetrain) => drivetrain switch
    {
        Core.Models.ListingAggregate.Enums.Drivetrain.FWD => ProtoDrivetrain.Fwd,
        Core.Models.ListingAggregate.Enums.Drivetrain.RWD => ProtoDrivetrain.Rwd,
        Core.Models.ListingAggregate.Enums.Drivetrain.AWD => ProtoDrivetrain.Awd,
        Core.Models.ListingAggregate.Enums.Drivetrain.FourWD => ProtoDrivetrain.FourWd,
        _ => ProtoDrivetrain.DrivetrainUnspecified
    };

    private static ProtoSellerType MapToProtoSellerType(Core.Models.ListingAggregate.Enums.SellerType sellerType) => sellerType switch
    {
        Core.Models.ListingAggregate.Enums.SellerType.Dealer => ProtoSellerType.Dealer,
        Core.Models.ListingAggregate.Enums.SellerType.Private => ProtoSellerType.Private,
        _ => ProtoSellerType.SellerTypeUnspecified
    };
}
