using Listing.Api.DTOs;
using Listing.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Listing.Api.Controllers;

/// <summary>
/// API controller for listing operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly ILogger<ListingsController> _logger;

    public ListingsController(IListingService listingService, ILogger<ListingsController> logger)
    {
        _listingService = listingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets listings with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ListingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ListingResponse>>> GetListings(
        [FromQuery] SearchListingsRequest request,
        CancellationToken cancellationToken)
    {
        var listings = await _listingService.SearchAsync(
            request.Make,
            request.Model,
            request.YearFrom,
            request.YearTo,
            request.MinPrice,
            request.MaxPrice,
            request.MaxMileage,
            request.Province,
            request.IsActive,
            request.Skip,
            Math.Min(request.Take, 100),
            cancellationToken);

        return Ok(listings.Select(MapToResponse));
    }

    /// <summary>
    /// Gets a listing by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ListingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _listingService.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(listing));
    }

    /// <summary>
    /// Creates a new listing.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ListingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListingResponse>> Create(
        [FromBody] CreateListingRequest request,
        CancellationToken cancellationToken)
    {
        // Check if listing already exists
        var existing = await _listingService.GetBySourceIdAsync(request.Source, request.SourceListingId, cancellationToken);
        if (existing != null)
        {
            return Conflict($"Listing from {request.Source} with ID {request.SourceListingId} already exists");
        }

        var listing = Core.Entities.Listing.Create(
            request.SourceListingId,
            request.Source,
            request.Title,
            request.Price,
            request.Make,
            request.Model,
            request.Year);

        if (request.Mileage.HasValue || !string.IsNullOrEmpty(request.VIN) || request.ImageUrls?.Count > 0)
        {
            listing.Update(
                mileage: request.Mileage,
                vin: request.VIN,
                description: request.Description,
                imageUrls: request.ImageUrls);
        }

        if (request.Condition.HasValue || request.BodyStyle.HasValue || request.Transmission.HasValue)
        {
            listing.SetVehicleDetails(
                request.Condition,
                request.BodyStyle,
                request.Transmission,
                request.FuelType,
                request.Drivetrain,
                request.ExteriorColor,
                request.InteriorColor);
        }

        if (!string.IsNullOrEmpty(request.City) || !string.IsNullOrEmpty(request.Province))
        {
            listing.SetLocation(request.City, request.Province, request.PostalCode);
        }

        if (!string.IsNullOrEmpty(request.DealerName))
        {
            listing.SetDealer(request.DealerName);
        }

        var created = await _listingService.CreateAsync(listing, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    /// <summary>
    /// Updates a listing.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ListingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingResponse>> Update(
        Guid id,
        [FromBody] UpdateListingRequest request,
        CancellationToken cancellationToken)
    {
        var listing = await _listingService.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
            return NotFound();
        }

        if (request.Price.HasValue && request.Price != listing.Price)
        {
            listing.UpdatePrice(request.Price.Value);
        }

        listing.Update(
            request.Title,
            request.Mileage,
            description: request.Description,
            imageUrls: request.ImageUrls);

        await _listingService.UpdateAsync(listing, cancellationToken);

        return Ok(MapToResponse(listing));
    }

    /// <summary>
    /// Deletes (deactivates) a listing.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _listingService.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
            return NotFound();
        }

        await _listingService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets price history for a listing.
    /// </summary>
    [HttpGet("{id:guid}/price-history")]
    [ProducesResponseType(typeof(IEnumerable<PriceHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PriceHistoryResponse>>> GetPriceHistory(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _listingService.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
            return NotFound();
        }

        var history = await _listingService.GetPriceHistoryAsync(id, cancellationToken);

        return Ok(history.Select(h => new PriceHistoryResponse
        {
            Price = h.Price,
            RecordedAt = h.RecordedAt
        }));
    }

    /// <summary>
    /// Creates multiple listings in batch.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchCreateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchCreateResponse>> CreateBatch(
        [FromBody] IEnumerable<CreateListingRequest> requests,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var request in requests)
        {
            try
            {
                var existing = await _listingService.GetBySourceIdAsync(request.Source, request.SourceListingId, cancellationToken);
                if (existing != null)
                {
                    skipped++;
                    continue;
                }

                var listing = Core.Entities.Listing.Create(
                    request.SourceListingId,
                    request.Source,
                    request.Title,
                    request.Price,
                    request.Make,
                    request.Model,
                    request.Year);

                await _listingService.CreateAsync(listing, cancellationToken);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error creating listing {request.SourceListingId}: {ex.Message}");
            }
        }

        return Ok(new BatchCreateResponse
        {
            Created = created,
            Skipped = skipped,
            Errors = errors
        });
    }

    private static ListingResponse MapToResponse(Core.Entities.Listing listing)
    {
        return new ListingResponse
        {
            Id = listing.Id,
            SourceListingId = listing.SourceListingId,
            Source = listing.Source,
            Title = listing.Title,
            Price = listing.Price,
            Make = listing.Make,
            Model = listing.Model,
            Year = listing.Year,
            Mileage = listing.Mileage,
            VIN = listing.VIN,
            Condition = listing.Condition?.ToString(),
            BodyStyle = listing.BodyStyle?.ToString(),
            Transmission = listing.Transmission?.ToString(),
            FuelType = listing.FuelType?.ToString(),
            Drivetrain = listing.Drivetrain?.ToString(),
            ExteriorColor = listing.ExteriorColor,
            InteriorColor = listing.InteriorColor,
            DealerName = listing.DealerName,
            DealerId = listing.DealerId,
            City = listing.City,
            Province = listing.Province,
            PostalCode = listing.PostalCode,
            ListingUrl = listing.ListingUrl,
            ImageUrls = listing.ImageUrls,
            FirstSeenAt = listing.FirstSeenAt,
            LastSeenAt = listing.LastSeenAt,
            IsActive = listing.IsActive
        };
    }
}

/// <summary>
/// Response for batch create operation.
/// </summary>
public sealed record BatchCreateResponse
{
    public int Created { get; init; }
    public int Skipped { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
