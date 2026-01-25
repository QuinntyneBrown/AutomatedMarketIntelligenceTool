using Microsoft.AspNetCore.Mvc;
using VehicleAggregation.Api.DTOs;
using VehicleAggregation.Core.Entities;
using VehicleAggregation.Core.Interfaces;

namespace VehicleAggregation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, cancellationToken);
        if (vehicle == null) return NotFound();
        return Ok(MapToResponse(vehicle));
    }

    [HttpGet("vin/{vin}")]
    public async Task<ActionResult<VehicleResponse>> GetByVin(string vin, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByVinAsync(vin, cancellationToken);
        if (vehicle == null) return NotFound();
        return Ok(MapToResponse(vehicle));
    }

    [HttpPost("search")]
    public async Task<ActionResult<IEnumerable<VehicleResponse>>> Search([FromBody] VehicleSearchRequest request, CancellationToken cancellationToken)
    {
        var criteria = new VehicleSearchCriteria
        {
            Make = request.Make,
            Model = request.Model,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            PriceFrom = request.PriceFrom,
            PriceTo = request.PriceTo,
            Skip = request.Skip,
            Take = request.Take
        };

        var vehicles = await _vehicleService.SearchAsync(criteria, cancellationToken);
        return Ok(vehicles.Select(MapToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var data = new VehicleData
        {
            VIN = request.VIN,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Trim = request.Trim,
            BodyStyle = request.BodyStyle,
            Transmission = request.Transmission,
            Drivetrain = request.Drivetrain,
            FuelType = request.FuelType,
            ExteriorColor = request.ExteriorColor,
            InteriorColor = request.InteriorColor
        };

        var vehicle = await _vehicleService.CreateOrUpdateAsync(data, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, MapToResponse(vehicle));
    }

    [HttpPost("{id:guid}/listings")]
    public async Task<ActionResult<VehicleResponse>> LinkListing(Guid id, [FromBody] LinkListingRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.LinkListingAsync(id, request.ListingId, request.Price, request.Source, request.DealerName, cancellationToken);
        return Ok(MapToResponse(vehicle));
    }

    [HttpDelete("{id:guid}/listings/{listingId:guid}")]
    public async Task<IActionResult> UnlinkListing(Guid id, Guid listingId, CancellationToken cancellationToken)
    {
        await _vehicleService.UnlinkListingAsync(id, listingId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/price-comparison")]
    public async Task<ActionResult<object>> GetPriceComparison(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, cancellationToken);
        if (vehicle == null) return NotFound();

        return Ok(new
        {
            vehicle.BestPrice,
            vehicle.AveragePrice,
            vehicle.LowestPrice,
            vehicle.HighestPrice,
            vehicle.ListingCount,
            Listings = vehicle.Listings.Select(l => new { l.ListingId, l.Price, l.Source, l.DealerName }).OrderBy(l => l.Price)
        });
    }

    private static VehicleResponse MapToResponse(Vehicle vehicle)
    {
        return new VehicleResponse(
            vehicle.Id,
            vehicle.VIN,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.Trim,
            vehicle.BodyStyle,
            vehicle.Transmission,
            vehicle.Drivetrain,
            vehicle.FuelType,
            vehicle.BestPrice,
            vehicle.AveragePrice,
            vehicle.LowestPrice,
            vehicle.HighestPrice,
            vehicle.ListingCount,
            vehicle.BestPriceListingId,
            vehicle.Listings.Select(l => new VehicleListingResponse(l.Id, l.ListingId, l.Price, l.Source, l.DealerName, l.AddedAt)).ToList());
    }
}
