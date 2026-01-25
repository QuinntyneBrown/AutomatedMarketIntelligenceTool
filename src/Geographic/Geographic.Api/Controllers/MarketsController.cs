using Geographic.Api.DTOs;
using Geographic.Core.Entities;
using Geographic.Core.Interfaces;
using Geographic.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Geographic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketsController : ControllerBase
{
    private readonly ICustomMarketService _marketService;
    private readonly IGeoDistanceCalculator _distanceCalculator;

    public MarketsController(ICustomMarketService marketService, IGeoDistanceCalculator distanceCalculator)
    {
        _marketService = marketService;
        _distanceCalculator = distanceCalculator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarketResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var markets = await _marketService.GetAllAsync(cancellationToken);
        return Ok(markets.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MarketResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var market = await _marketService.GetByIdAsync(id, cancellationToken);
        if (market == null) return NotFound();
        return Ok(MapToResponse(market));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<MarketResponse>>> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        var markets = await _marketService.SearchByNameAsync(name, cancellationToken);
        return Ok(markets.Select(MapToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<MarketResponse>> Create([FromBody] CreateMarketRequest request, CancellationToken cancellationToken)
    {
        var market = new CustomMarket
        {
            Name = request.Name,
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            RadiusKm = request.RadiusKm,
            PostalCodes = request.PostalCodes
        };

        var created = await _marketService.CreateAsync(market, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MarketResponse>> Update(Guid id, [FromBody] UpdateMarketRequest request, CancellationToken cancellationToken)
    {
        var existing = await _marketService.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound();

        var market = new CustomMarket
        {
            Id = id,
            Name = request.Name,
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            RadiusKm = request.RadiusKm,
            PostalCodes = request.PostalCodes
        };

        var updated = await _marketService.UpdateAsync(market, cancellationToken);
        return Ok(MapToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _marketService.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound();

        await _marketService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("geocode")]
    public async Task<ActionResult<GeoLocationResponse>> Geocode([FromBody] GeocodeRequest request, CancellationToken cancellationToken)
    {
        var location = await _distanceCalculator.GeocodeAsync(request.Address, cancellationToken);
        if (location == null) return NotFound($"Could not geocode address: {request.Address}");

        return Ok(new GeoLocationResponse(
            location.Latitude,
            location.Longitude,
            location.City,
            location.Province,
            location.PostalCode));
    }

    [HttpPost("distance")]
    public ActionResult<DistanceResponse> CalculateDistance([FromBody] DistanceRequest request)
    {
        var from = new GeoLocation
        {
            Latitude = request.FromLatitude,
            Longitude = request.FromLongitude
        };

        var to = new GeoLocation
        {
            Latitude = request.ToLatitude,
            Longitude = request.ToLongitude
        };

        var result = _distanceCalculator.CalculateDistance(from, to);

        return Ok(new DistanceResponse(
            new GeoLocationResponse(result.FromLocation.Latitude, result.FromLocation.Longitude, null, null, null),
            new GeoLocationResponse(result.ToLocation.Latitude, result.ToLocation.Longitude, null, null, null),
            result.DistanceKm));
    }

    [HttpPost("within")]
    public ActionResult<WithinRadiusResponse> CheckWithinRadius([FromBody] WithinRadiusRequest request)
    {
        var isWithin = _distanceCalculator.IsWithinRadius(
            request.CenterLatitude,
            request.CenterLongitude,
            request.PointLatitude,
            request.PointLongitude,
            request.RadiusKm);

        var distance = _distanceCalculator.CalculateDistance(
            request.CenterLatitude,
            request.CenterLongitude,
            request.PointLatitude,
            request.PointLongitude);

        return Ok(new WithinRadiusResponse(isWithin, distance));
    }

    private static MarketResponse MapToResponse(CustomMarket market)
    {
        return new MarketResponse(
            market.Id,
            market.Name,
            market.CenterLatitude,
            market.CenterLongitude,
            market.RadiusKm,
            market.PostalCodes,
            market.CreatedAt);
    }
}
