using System.Collections.Concurrent;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Entities.Session;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Service.Services.Session;

/// <summary>
/// In-memory session management service
/// TODO: Replace with database storage for production
/// </summary>
public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;

    // In-memory session storage: SessionToken -> SessionData
    private readonly ConcurrentDictionary<string, SessionData> _sessions = new();

    // Rate limiting: SessionToken -> Request timestamps (last 60 seconds)
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();

    // Configuration
    private const int MaxRequestsPerMinute = 60;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public Task<SessionData> CreateSessionAsync(
        string nodeId,
        string channelId,
        List<string> capabilities,
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
            Capabilities = capabilities,
            RequestCount = 0
        };

        _sessions[sessionToken] = sessionData;

        _logger.LogInformation(
            "Session created for node {NodeId}: {SessionToken}, expires at {ExpiresAt}",
            nodeId,
            sessionToken,
            sessionData.ExpiresAt);

        return Task.FromResult(sessionData);
    }

    public Task<SessionContext?> ValidateSessionAsync(string sessionToken)
    {
        if (!_sessions.TryGetValue(sessionToken, out var sessionData))
        {
            _logger.LogWarning("Session not found: {SessionToken}", sessionToken);
            return Task.FromResult<SessionContext?>(null);
        }

        if (!sessionData.IsValid())
        {
            _logger.LogWarning(
                "Session expired: {SessionToken}, expired at {ExpiresAt}",
                sessionToken,
                sessionData.ExpiresAt);

            // Remove expired session
            _sessions.TryRemove(sessionToken, out _);
            return Task.FromResult<SessionContext?>(null);
        }

        // Update last accessed time
        sessionData.LastAccessedAt = DateTime.UtcNow;

        // Convert to SessionContext
        var context = new SessionContext
        {
            SessionToken = sessionData.SessionToken,
            NodeId = sessionData.NodeId,
            ChannelId = sessionData.ChannelId,
            ExpiresAt = sessionData.ExpiresAt,
            Capabilities = sessionData.Capabilities,
            RequestCount = sessionData.RequestCount
        };

        _logger.LogDebug(
            "Session validated: {SessionToken}, node {NodeId}, {RemainingSeconds}s remaining",
            sessionToken,
            sessionData.NodeId,
            context.GetRemainingSeconds());

        return Task.FromResult<SessionContext?>(context);
    }

    public Task<SessionData?> RenewSessionAsync(string sessionToken, int additionalSeconds = 3600)
    {
        if (!_sessions.TryGetValue(sessionToken, out var sessionData))
        {
            _logger.LogWarning("Cannot renew - session not found: {SessionToken}", sessionToken);
            return Task.FromResult<SessionData?>(null);
        }

        if (!sessionData.IsValid())
        {
            _logger.LogWarning(
                "Cannot renew - session expired: {SessionToken}",
                sessionToken);

            _sessions.TryRemove(sessionToken, out _);
            return Task.FromResult<SessionData?>(null);
        }

        // Extend expiration
        sessionData.ExpiresAt = sessionData.ExpiresAt.AddSeconds(additionalSeconds);
        sessionData.LastAccessedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Session renewed: {SessionToken}, new expiration {ExpiresAt}",
            sessionToken,
            sessionData.ExpiresAt);

        return Task.FromResult<SessionData?>(sessionData);
    }

    public Task<bool> RevokeSessionAsync(string sessionToken)
    {
        var removed = _sessions.TryRemove(sessionToken, out var sessionData);

        if (removed)
        {
            _logger.LogInformation(
                "Session revoked: {SessionToken}, node {NodeId}",
                sessionToken,
                sessionData!.NodeId);

            // Clean up rate limiting history
            _requestHistory.TryRemove(sessionToken, out _);
        }
        else
        {
            _logger.LogWarning("Cannot revoke - session not found: {SessionToken}", sessionToken);
        }

        return Task.FromResult(removed);
    }

    public Task<List<SessionData>> GetNodeSessionsAsync(string nodeId)
    {
        var sessions = _sessions.Values
            .Where(s => s.NodeId == nodeId && s.IsValid())
            .ToList();

        _logger.LogDebug("Found {Count} active sessions for node {NodeId}", sessions.Count, nodeId);

        return Task.FromResult(sessions);
    }

    public Task<SessionMetrics> GetSessionMetricsAsync(string nodeId)
    {
        var sessions = _sessions.Values
            .Where(s => s.NodeId == nodeId && s.IsValid())
            .ToList();

        var metrics = new SessionMetrics
        {
            NodeId = nodeId,
            ActiveSessions = sessions.Count,
            TotalRequests = sessions.Sum(s => s.RequestCount),
            LastAccessedAt = sessions.Any()
                ? sessions.Max(s => s.LastAccessedAt)
                : null,
            UsedCapabilities = sessions
                .SelectMany(s => s.Capabilities)
                .Distinct()
                .ToList()
        };

        _logger.LogDebug(
            "Metrics for node {NodeId}: {ActiveSessions} sessions, {TotalRequests} requests",
            nodeId,
            metrics.ActiveSessions,
            metrics.TotalRequests);

        return Task.FromResult(metrics);
    }

    public Task<int> CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredSessions = _sessions
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        var count = 0;
        foreach (var sessionToken in expiredSessions)
        {
            if (_sessions.TryRemove(sessionToken, out _))
            {
                _requestHistory.TryRemove(sessionToken, out _);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", count);
        }

        return Task.FromResult(count);
    }

    public Task<bool> RecordRequestAsync(string sessionToken)
    {
        if (!_sessions.TryGetValue(sessionToken, out var sessionData))
        {
            return Task.FromResult(false);
        }

        // Increment request count
        sessionData.RequestCount++;
        sessionData.LastAccessedAt = DateTime.UtcNow;

        // Rate limiting: Token bucket algorithm
        var now = DateTime.UtcNow;
        var requestQueue = _requestHistory.GetOrAdd(sessionToken, _ => new Queue<DateTime>());

        // Remove requests older than 60 seconds
        while (requestQueue.Count > 0 && requestQueue.Peek() < now.AddSeconds(-60))
        {
            requestQueue.Dequeue();
        }

        // Check if rate limit exceeded
        if (requestQueue.Count >= MaxRequestsPerMinute)
        {
            _logger.LogWarning(
                "Rate limit exceeded for session {SessionToken}, node {NodeId}",
                sessionToken,
                sessionData.NodeId);
            return Task.FromResult(false);
        }

        // Record this request
        requestQueue.Enqueue(now);

        return Task.FromResult(true);
    }
}
