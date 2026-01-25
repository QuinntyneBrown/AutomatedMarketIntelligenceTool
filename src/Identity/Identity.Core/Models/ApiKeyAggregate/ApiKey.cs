namespace Identity.Core.Models.ApiKeyAggregate;

/// <summary>
/// Represents an API key for programmatic access.
/// </summary>
public sealed class ApiKey
{
    public Guid ApiKeyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public string? Scopes { get; private set; }

    private ApiKey() { } // For EF Core

    public static (ApiKey ApiKey, string PlainTextKey) Create(
        Guid userId,
        string name,
        string keyHash,
        string plainTextKey,
        DateTimeOffset? expiresAt = null,
        string? scopes = null)
    {
        var apiKey = new ApiKey
        {
            ApiKeyId = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = plainTextKey[..8],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            Scopes = scopes
        };

        return (apiKey, plainTextKey);
    }

    public static string GeneratePlainTextKey()
    {
        // Generate a secure random API key
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;

    public bool IsValid => IsActive && !IsExpired;
}
