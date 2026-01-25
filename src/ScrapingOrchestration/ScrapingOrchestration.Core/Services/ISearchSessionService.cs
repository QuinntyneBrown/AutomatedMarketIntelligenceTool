using ScrapingOrchestration.Core.Entities;
using ScrapingOrchestration.Core.Enums;
using ScrapingOrchestration.Core.ValueObjects;

namespace ScrapingOrchestration.Core.Services;

/// <summary>
/// Interface for managing search sessions.
/// </summary>
public interface ISearchSessionService
{
    /// <summary>
    /// Creates a new search session.
    /// </summary>
    Task<SearchSession> CreateSessionAsync(
        SearchParameters parameters,
        IEnumerable<ScrapingSource> sources,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a search session by ID.
    /// </summary>
    Task<SearchSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a user.
    /// </summary>
    Task<IReadOnlyList<SearchSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sessions by status.
    /// </summary>
    Task<IReadOnlyList<SearchSession>> GetSessionsByStatusAsync(SearchSessionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session's status.
    /// </summary>
    Task UpdateSessionAsync(SearchSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a search session.
    /// </summary>
    Task CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
