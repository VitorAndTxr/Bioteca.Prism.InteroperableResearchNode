using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Entities.Session;
using Bioteca.Prism.Domain.Enumerators.Node;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Service.Services.Session;

/// <summary>
/// Session management service with pluggable storage backend (in-memory or Redis)
/// </summary>
public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly ISessionStore _sessionStore;

    // Configuration
    private const int MaxRequestsPerMinute = 60;

    public SessionService(
        ILogger<SessionService> logger,
        ISessionStore sessionStore)
    {
        _logger = logger;
        _sessionStore = sessionStore;
    }

    public async Task<SessionData> CreateSessionAsync(
        string nodeId,
        string channelId,
        NodeAccessTypeEnum accessLevel,
        int ttlSeconds = 3600)
    {
        var sessionToken = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var sessionData = new SessionData
        {
            SessionToken = sessionToken,
            NodeId = nodeId,
            ChannelId = channelId,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(ttlSeconds),
            LastAccessedAt = now,
            AccessLevel = accessLevel,
            RequestCount = 0
        };

        var success = await _sessionStore.CreateSessionAsync(sessionData);

        if (!success)
        {
            _logger.LogError("Failed to create session for node {NodeId}", nodeId);
            throw new InvalidOperationException("Failed to create session");
        }

        _logger.LogInformation(
            "Session created for node {NodeId}: {SessionToken}, expires at {ExpiresAt}",
            nodeId,
            sessionToken,
            sessionData.ExpiresAt);

        return sessionData;
    }

    public async Task<SessionContext?> ValidateSessionAsync(string sessionToken)
    {
        var sessionData = await _sessionStore.GetSessionAsync(sessionToken);

        if (sessionData == null)
        {
            _logger.LogWarning("Session not found: {SessionToken}", sessionToken);
            return null;
        }

        if (!sessionData.IsValid())
        {
            _logger.LogWarning(
                "Session expired: {SessionToken}, expired at {ExpiresAt}",
                sessionToken,
                sessionData.ExpiresAt);

            // Remove expired session
            await _sessionStore.RemoveSessionAsync(sessionToken);
            return null;
        }

        // Update last accessed time
        sessionData.LastAccessedAt = DateTime.UtcNow;
        await _sessionStore.UpdateSessionAsync(sessionData);

        // Convert to SessionContext
        var context = new SessionContext
        {
            SessionToken = sessionData.SessionToken,
            NodeId = sessionData.NodeId,
            ChannelId = sessionData.ChannelId,
            ExpiresAt = sessionData.ExpiresAt,
            NodeAccessLevel = sessionData.AccessLevel,
            RequestCount = sessionData.RequestCount
        };

        _logger.LogDebug(
            "Session validated: {SessionToken}, node {NodeId}, {RemainingSeconds}s remaining",
            sessionToken,
            sessionData.NodeId,
            context.GetRemainingSeconds());

        return context;
    }

    public async Task<SessionData?> RenewSessionAsync(string sessionToken, int additionalSeconds = 3600)
    {
        var sessionData = await _sessionStore.GetSessionAsync(sessionToken);

        if (sessionData == null)
        {
            _logger.LogWarning("Cannot renew - session not found: {SessionToken}", sessionToken);
            return null;
        }

        if (!sessionData.IsValid())
        {
            _logger.LogWarning(
                "Cannot renew - session expired: {SessionToken}",
                sessionToken);

            await _sessionStore.RemoveSessionAsync(sessionToken);
            return null;
        }

        // Extend expiration
        sessionData.ExpiresAt = sessionData.ExpiresAt.AddSeconds(additionalSeconds);
        sessionData.LastAccessedAt = DateTime.UtcNow;

        var success = await _sessionStore.UpdateSessionAsync(sessionData);

        if (!success)
        {
            _logger.LogError("Failed to renew session: {SessionToken}", sessionToken);
            return null;
        }

        _logger.LogInformation(
            "Session renewed: {SessionToken}, new expiration {ExpiresAt}",
            sessionToken,
            sessionData.ExpiresAt);

        return sessionData;
    }

    public async Task<bool> RevokeSessionAsync(string sessionToken)
    {
        var removed = await _sessionStore.RemoveSessionAsync(sessionToken);

        if (removed)
        {
            _logger.LogInformation("Session revoked: {SessionToken}", sessionToken);
        }
        else
        {
            _logger.LogWarning("Cannot revoke - session not found: {SessionToken}", sessionToken);
        }

        return removed;
    }

    public async Task<List<SessionData>> GetNodeSessionsAsync(string nodeId)
    {
        var sessions = await _sessionStore.GetNodeSessionsAsync(nodeId);

        _logger.LogDebug("Found {Count} active sessions for node {NodeId}", sessions.Count, nodeId);

        return sessions;
    }

    public async Task<SessionMetrics> GetSessionMetricsAsync(string nodeId)
    {
        var sessions = await _sessionStore.GetNodeSessionsAsync(nodeId);

        var metrics = new SessionMetrics
        {
            NodeId = nodeId,
            ActiveSessions = sessions.Count,
            TotalRequests = sessions.Sum(s => s.RequestCount),
            LastAccessedAt = sessions.Any()
                ? sessions.Max(s => s.LastAccessedAt)
                : null,
            NodeAccessLevel = sessions
                .Select(s => s.AccessLevel)
                .Distinct().FirstOrDefault()
        };

        _logger.LogDebug(
            "Metrics for node {NodeId}: {ActiveSessions} sessions, {TotalRequests} requests",
            nodeId,
            metrics.ActiveSessions,
            metrics.TotalRequests);

        return metrics;
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        await _sessionStore.CleanupExpiredSessionsAsync();

        _logger.LogDebug("Cleanup expired sessions requested");

        // Return 0 since Redis handles cleanup automatically via TTL
        return 0;
    }

    public async Task<bool> RecordRequestAsync(string sessionToken)
    {
        var sessionData = await _sessionStore.GetSessionAsync(sessionToken);

        if (sessionData == null)
        {
            return false;
        }

        // Increment request count
        sessionData.RequestCount++;
        sessionData.LastAccessedAt = DateTime.UtcNow;
        await _sessionStore.UpdateSessionAsync(sessionData);

        // Rate limiting: Check request count in last 60 seconds
        var now = DateTime.UtcNow;
        await _sessionStore.RecordRequestAsync(sessionToken, now);

        var requestCount = await _sessionStore.GetRequestCountAsync(sessionToken, TimeSpan.FromMinutes(1));

        // Check if rate limit exceeded
        if (requestCount >= MaxRequestsPerMinute)
        {
            _logger.LogWarning(
                "Rate limit exceeded for session {SessionToken}, node {NodeId}",
                sessionToken,
                sessionData.NodeId);
            return false;
        }

        return true;
    }
}
