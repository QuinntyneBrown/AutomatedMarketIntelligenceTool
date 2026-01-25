using Identity.Core.Models.ApiKeyAggregate;
using Identity.Core.Services;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/api-keys")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ApiKeysController(IdentityDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKeyResponse>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var apiKeys = await _context.ApiKeys
            .Where(k => k.UserId == userId.Value)
            .Select(k => new ApiKeyResponse
            {
                ApiKeyId = k.ApiKeyId,
                Name = k.Name,
                KeyPrefix = k.KeyPrefix,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                Scopes = k.Scopes
            })
            .ToListAsync(cancellationToken);

        return Ok(apiKeys);
    }

    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var plainTextKey = ApiKey.GeneratePlainTextKey();
        var keyHash = _passwordHasher.HashPassword(plainTextKey);

        var (apiKey, _) = ApiKey.Create(
            userId.Value,
            request.Name,
            keyHash,
            plainTextKey,
            request.ExpiresAt,
            request.Scopes);

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetApiKeys), new CreateApiKeyResponse
        {
            ApiKeyId = apiKey.ApiKeyId,
            Name = apiKey.Name,
            Key = plainTextKey, // Only returned once!
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteApiKey(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.ApiKeyId == id && k.UserId == userId.Value, cancellationToken);

        if (apiKey == null) return NotFound();

        apiKey.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("userId");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

public record CreateApiKeyRequest
{
    public required string Name { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Scopes { get; init; }
}

public record CreateApiKeyResponse
{
    public Guid ApiKeyId { get; init; }
    public required string Name { get; init; }
    public required string Key { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public record ApiKeyResponse
{
    public Guid ApiKeyId { get; init; }
    public required string Name { get; init; }
    public required string KeyPrefix { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? Scopes { get; init; }
}
