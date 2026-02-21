using Bioteca.Prism.Domain.Entities.Session;
using Bioteca.Prism.Domain.Enumerators.Node;

namespace Bioteca.Prism.Core.Middleware.Session;

/// <summary>
/// Service for managing authenticated sessions (Phase 4)
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Create a new session for an authenticated node
    /// </summary>
    /// <param name="nodeId">Node Guid identifier</param>
    /// <param name="channelId">Channel ID used during authentication</param>
    /// <param name="capabilities">Capabilities to grant to this session</param>
    /// <param name="ttlSeconds">Session TTL in seconds (default: 3600 = 1 hour)</param>
    /// <returns>Created session data</returns>
    Task<SessionData> CreateSessionAsync(
        Guid nodeId,
        string channelId,
        NodeAccessTypeEnum accessLevel,
        int ttlSeconds = 3600);

    /// <summary>
    /// Validate a session token and return session context
    /// </summary>
    /// <param name="sessionToken">Session token (GUID)</param>
    /// <returns>Session context if valid, null if invalid/expired</returns>
    Task<SessionContext?> ValidateSessionAsync(string sessionToken);

    /// <summary>
    /// Renew an existing session (extend TTL)
    /// </summary>
    /// <param name="sessionToken">Session token to renew</param>
    /// <param name="additionalSeconds">Seconds to add to expiration (default: 3600)</param>
    /// <returns>Updated session data if successful, null if session not found/expired</returns>
    Task<SessionData?> RenewSessionAsync(string sessionToken, int additionalSeconds = 3600);

    /// <summary>
    /// Revoke a session (logout)
    /// </summary>
    /// <param name="sessionToken">Session token to revoke</param>
    /// <returns>True if revoked, false if session not found</returns>
    Task<bool> RevokeSessionAsync(string sessionToken);

    /// <summary>
    /// Get all active sessions for a node
    /// </summary>
    /// <param name="nodeId">Node Guid identifier</param>
    /// <returns>List of active sessions</returns>
    Task<List<SessionData>> GetNodeSessionsAsync(Guid nodeId);

    /// <summary>
    /// Get session metrics for a node
    /// </summary>
    /// <param name="nodeId">Node Guid identifier</param>
    /// <returns>Session metrics (total sessions, total requests, etc.)</returns>
    Task<SessionMetrics> GetSessionMetricsAsync(Guid nodeId);

    /// <summary>
    /// Cleanup expired sessions (background job)
    /// </summary>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync();

    /// <summary>
    /// Record a request for rate limiting
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <param name="overrideLimit">When greater than 0, overrides the default rate limit (60 req/min).
    /// Use 600 for sync endpoints.</param>
    /// <returns>True if request allowed, false if rate limit exceeded</returns>
    Task<bool> RecordRequestAsync(string sessionToken, int overrideLimit = 0);
}

/// <summary>
/// Session metrics for a node
/// </summary>
public class SessionMetrics
{
    public Guid NodeId { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalRequests { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }
}
