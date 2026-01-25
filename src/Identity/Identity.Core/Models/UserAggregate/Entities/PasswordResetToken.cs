namespace Identity.Core.Models.UserAggregate.Entities;

/// <summary>
/// Token for password reset.
/// </summary>
public sealed class PasswordResetToken
{
    public Guid PasswordResetTokenId { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken() { } // For EF Core

    /// <summary>
    /// Creates a new password reset token.
    /// </summary>
    public static PasswordResetToken Create(Guid userId, TimeSpan lifetime)
    {
        return new PasswordResetToken
        {
            PasswordResetTokenId = Guid.NewGuid(),
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
        };
    }

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Checks if the token has been used.
    /// </summary>
    public bool IsUsed => UsedAt.HasValue;

    /// <summary>
    /// Checks if the token is valid (not expired and not used).
    /// </summary>
    public bool IsValid => !IsExpired && !IsUsed;

    /// <summary>
    /// Marks the token as used.
    /// </summary>
    public void MarkUsed()
    {
        UsedAt = DateTimeOffset.UtcNow;
    }
}
