using Deduplication.Api.DTOs;
using Deduplication.Core.Entities;
using Deduplication.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Deduplication.Api.Controllers;

[ApiController]
[Route("api/deduplication/[controller]")]
public class ConfigsController : ControllerBase
{
    private readonly IDeduplicationConfigService _configService;

    public ConfigsController(IDeduplicationConfigService configService)
    {
        _configService = configService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeduplicationConfigResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var configs = await _configService.GetAllAsync(cancellationToken);
        return Ok(configs.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeduplicationConfigResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var config = await _configService.GetByIdAsync(id, cancellationToken);
        if (config == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(config));
    }

    [HttpGet("active")]
    public async Task<ActionResult<DeduplicationConfigResponse>> GetActive(
        CancellationToken cancellationToken)
    {
        var config = await _configService.GetActiveConfigAsync(cancellationToken);
        return Ok(MapToResponse(config));
    }

    [HttpPost]
    public async Task<ActionResult<DeduplicationConfigResponse>> Create(
        [FromBody] CreateConfigRequest request,
        CancellationToken cancellationToken)
    {
        var config = await _configService.CreateAsync(
            request.Name,
            request.OverallMatchThreshold,
            request.ReviewThreshold,
            request.VinWeight,
            request.TitleWeight,
            request.PriceWeight,
            request.LocationWeight,
            request.ImageHashWeight,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = config.Id }, MapToResponse(config));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DeduplicationConfigResponse>> Update(
        Guid id,
        [FromBody] UpdateConfigRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _configService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var config = await _configService.UpdateAsync(
            id,
            request.OverallMatchThreshold,
            request.ReviewThreshold,
            request.VinWeight,
            request.TitleWeight,
            request.PriceWeight,
            request.LocationWeight,
            request.ImageHashWeight,
            cancellationToken);

        return Ok(MapToResponse(config));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var config = await _configService.GetByIdAsync(id, cancellationToken);
        if (config == null)
        {
            return NotFound();
        }

        await _configService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var config = await _configService.GetByIdAsync(id, cancellationToken);
        if (config == null)
        {
            return NotFound();
        }

        await _configService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    private static DeduplicationConfigResponse MapToResponse(DeduplicationConfig config)
    {
        return new DeduplicationConfigResponse(
            config.Id,
            config.Name,
            config.IsActive,
            config.TitleSimilarityThreshold,
            config.ImageHashSimilarityThreshold,
            config.OverallMatchThreshold,
            config.ReviewThreshold,
            config.TitleWeight,
            config.VinWeight,
            config.ImageHashWeight,
            config.PriceWeight,
            config.MileageWeight,
            config.LocationWeight,
            config.CreatedAt,
            config.UpdatedAt);
    }
}
