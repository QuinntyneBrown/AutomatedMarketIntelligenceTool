using Microsoft.AspNetCore.Mvc;
using Dealer.Api.DTOs;
using Dealer.Core.Interfaces;

namespace Dealer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealersController : ControllerBase
{
    private readonly IDealerService _dealerService;

    public DealersController(IDealerService dealerService)
    {
        _dealerService = dealerService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DealerResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dealer = await _dealerService.GetByIdAsync(id, cancellationToken);
        if (dealer == null) return NotFound();
        return Ok(MapToResponse(dealer));
    }

    [HttpGet("normalized/{normalizedName}")]
    public async Task<ActionResult<DealerResponse>> GetByNormalizedName(string normalizedName, CancellationToken cancellationToken)
    {
        var dealer = await _dealerService.GetByNormalizedNameAsync(normalizedName, cancellationToken);
        if (dealer == null) return NotFound();
        return Ok(MapToResponse(dealer));
    }

    [HttpPost("search")]
    public async Task<ActionResult<IEnumerable<DealerResponse>>> Search([FromBody] DealerSearchRequest request, CancellationToken cancellationToken)
    {
        var criteria = new DealerSearchCriteria
        {
            Name = request.Name,
            City = request.City,
            Province = request.Province,
            MinReliabilityScore = request.MinReliabilityScore,
            MinListingCount = request.MinListingCount,
            Skip = request.Skip,
            Take = request.Take
        };

        var dealers = await _dealerService.SearchAsync(criteria, cancellationToken);
        return Ok(dealers.Select(MapToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<DealerResponse>> Create([FromBody] CreateDealerRequest request, CancellationToken cancellationToken)
    {
        var data = new DealerData
        {
            Name = request.Name,
            Website = request.Website,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode
        };

        var dealer = await _dealerService.CreateAsync(data, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dealer.Id }, MapToResponse(dealer));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DealerResponse>> Update(Guid id, [FromBody] UpdateDealerRequest request, CancellationToken cancellationToken)
    {
        var data = new DealerData
        {
            Name = request.Name ?? string.Empty,
            Website = request.Website,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode
        };

        try
        {
            var dealer = await _dealerService.UpdateAsync(id, data, cancellationToken);
            return Ok(MapToResponse(dealer));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _dealerService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/inventory")]
    public async Task<ActionResult<DealerInventoryResponse>> GetInventory(Guid id, CancellationToken cancellationToken)
    {
        var dealer = await _dealerService.GetByIdAsync(id, cancellationToken);
        if (dealer == null) return NotFound();

        return Ok(new DealerInventoryResponse(
            dealer.Id,
            dealer.Name,
            dealer.ListingCount,
            dealer.City,
            dealer.Province));
    }

    [HttpGet("{id:guid}/reliability")]
    public async Task<ActionResult<DealerReliabilityResponse>> GetReliability(Guid id, CancellationToken cancellationToken)
    {
        var dealer = await _dealerService.GetByIdAsync(id, cancellationToken);
        if (dealer == null) return NotFound();

        return Ok(new DealerReliabilityResponse(
            dealer.Id,
            dealer.Name,
            dealer.ReliabilityScore,
            dealer.ListingCount,
            dealer.City,
            dealer.Province));
    }

    [HttpPut("{id:guid}/reliability")]
    public async Task<ActionResult<DealerReliabilityResponse>> UpdateReliability(Guid id, [FromBody] UpdateReliabilityScoreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dealer = await _dealerService.UpdateReliabilityScoreAsync(id, request.Score, request.Reason, cancellationToken);
            return Ok(new DealerReliabilityResponse(
                dealer.Id,
                dealer.Name,
                dealer.ReliabilityScore,
                dealer.ListingCount,
                dealer.City,
                dealer.Province));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("province/{province}")]
    public async Task<ActionResult<IEnumerable<DealerResponse>>> GetByProvince(string province, CancellationToken cancellationToken)
    {
        var dealers = await _dealerService.GetByProvinceAsync(province, cancellationToken);
        return Ok(dealers.Select(MapToResponse));
    }

    [HttpGet("city/{city}")]
    public async Task<ActionResult<IEnumerable<DealerResponse>>> GetByCity(string city, CancellationToken cancellationToken)
    {
        var dealers = await _dealerService.GetByCityAsync(city, cancellationToken);
        return Ok(dealers.Select(MapToResponse));
    }

    private static DealerResponse MapToResponse(Core.Entities.Dealer dealer)
    {
        return new DealerResponse(
            dealer.Id,
            dealer.Name,
            dealer.NormalizedName,
            dealer.Website,
            dealer.Phone,
            dealer.Address,
            dealer.City,
            dealer.Province,
            dealer.PostalCode,
            dealer.ReliabilityScore,
            dealer.ListingCount,
            dealer.CreatedAt,
            dealer.UpdatedAt);
    }
}
