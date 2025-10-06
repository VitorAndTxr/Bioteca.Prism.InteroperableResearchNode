using Bioteca.Prism.Domain.Entities.Session;

namespace Bioteca.Prism.Core.Cache.Session;

/// <summary>
/// Interface for session persistence (in-memory or distributed cache)
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Stores a new session with automatic TTL expiration
    /// </summary>
    Task<bool> CreateSessionAsync(SessionData session);

    /// <summary>
    /// Retrieves a session by token
    /// </summary>
    /// <returns>Session data if found and not expired, null otherwise</returns>
    Task<SessionData?> GetSessionAsync(string sessionToken);

    /// <summary>
    /// Updates an existing session (e.g., renew expiration, update last access)
    /// </summary>
    Task<bool> UpdateSessionAsync(SessionData session);

    /// <summary>
    /// Removes a session (logout/revoke)
    /// </summary>
    Task<bool> RemoveSessionAsync(string sessionToken);

    /// <summary>
    /// Gets all active sessions for a specific node
    /// </summary>
    Task<List<SessionData>> GetNodeSessionsAsync(Guid nodeId);

    /// <summary>
    /// Records a request for rate limiting
    /// </summary>
    Task<bool> RecordRequestAsync(string sessionToken, DateTime timestamp);

    /// <summary>
    /// Gets request count within a time window for rate limiting
    /// </summary>
    Task<int> GetRequestCountAsync(string sessionToken, TimeSpan window);

    /// <summary>
    /// Gets total number of active sessions
    /// </summary>
    Task<int> GetActiveSessionCountAsync();

    /// <summary>
    /// Cleans up expired sessions (for in-memory implementation)
    /// Redis handles this automatically via TTL
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}
