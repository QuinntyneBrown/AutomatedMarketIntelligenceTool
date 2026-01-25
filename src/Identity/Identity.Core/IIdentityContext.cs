using Identity.Core.Models.UserAggregate;
using Identity.Core.Models.UserAggregate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Core;

/// <summary>
/// Persistence interface for the Identity bounded context.
/// </summary>
public interface IIdentityContext
{
    /// <summary>
    /// Users in the system.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// User profiles with business information.
    /// </summary>
    DbSet<UserProfile> UserProfiles { get; }

    /// <summary>
    /// Refresh tokens for authentication.
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Email verification tokens.
    /// </summary>
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }

    /// <summary>
    /// Password reset tokens.
    /// </summary>
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
