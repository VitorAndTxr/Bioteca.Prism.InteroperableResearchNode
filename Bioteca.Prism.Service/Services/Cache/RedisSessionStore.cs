using Bioteca.Prism.Core.Cache;
using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Domain.Entities.Session;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Bioteca.Prism.Service.Services.Cache;

/// <summary>
/// Redis-based session store with automatic TTL expiration
/// </summary>
public class RedisSessionStore : ISessionStore
{
    private readonly IRedisConnectionService _redis;
    private readonly ILogger<RedisSessionStore> _logger;

    // Redis key patterns
    private const string SessionKeyPrefix = "session:";
    private const string NodeSessionsKeyPrefix = "session:node:";
    private const string RateLimitKeyPrefix = "session:ratelimit:";

    public RedisSessionStore(
        IRedisConnectionService redis,
        ILogger<RedisSessionStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> CreateSessionAsync(SessionData session)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetSessionKey(session.SessionToken);
            var nodeSessionsKey = GetNodeSessionsKey(session.NodeId);

            // Calculate TTL from session expiration
            var ttl = session.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                _logger.LogWarning("Attempted to create session {SessionToken} with expired TTL",
                    session.SessionToken);
                return false;
            }

            // Serialize session data
            var json = JsonSerializer.Serialize(session);

            // Use transaction to ensure atomicity
            var transaction = db.CreateTransaction();

            // Store session with TTL
            var setTask = transaction.StringSetAsync(key, json, ttl);

            // Add session token to node's session index (with same TTL)
            var addTask = transaction.SetAddAsync(nodeSessionsKey, session.SessionToken);
            var expireTask = transaction.KeyExpireAsync(nodeSessionsKey, ttl);

            // Execute transaction
            var committed = await transaction.ExecuteAsync();

            if (committed)
            {
                _logger.LogInformation("Session {SessionToken} created for node {NodeId} with TTL {TTL}s",
                    session.SessionToken, session.NodeId, (int)ttl.TotalSeconds);
                return true;
            }

            _logger.LogWarning("Failed to create session {SessionToken} - transaction not committed",
                session.SessionToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session {SessionToken}", session.SessionToken);
            return false;
        }
    }

    public async Task<SessionData?> GetSessionAsync(string sessionToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetSessionKey(sessionToken);

            var json = await db.StringGetAsync(key);
            if (json.IsNullOrEmpty)
            {
                return null;
            }

            var session = JsonSerializer.Deserialize<SessionData>(json!);

            // Check expiration (defense in depth, Redis TTL should handle this)
            if (session != null && !session.IsValid())
            {
                _logger.LogWarning("Retrieved expired session {SessionToken}, removing", sessionToken);
                await RemoveSessionAsync(sessionToken);
                return null;
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionToken}", sessionToken);
            return null;
        }
    }

    public async Task<bool> UpdateSessionAsync(SessionData session)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetSessionKey(session.SessionToken);

            // Calculate new TTL
            var ttl = session.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                _logger.LogWarning("Attempted to update session {SessionToken} with expired TTL",
                    session.SessionToken);
                await RemoveSessionAsync(session.SessionToken);
                return false;
            }

            // Serialize and update
            var json = JsonSerializer.Serialize(session);
            var success = await db.StringSetAsync(key, json, ttl);

            if (success)
            {
                _logger.LogDebug("Session {SessionToken} updated with new TTL {TTL}s",
                    session.SessionToken, (int)ttl.TotalSeconds);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionToken}", session.SessionToken);
            return false;
        }
    }

    public async Task<bool> RemoveSessionAsync(string sessionToken)
    {
        try
        {
            var db = _redis.GetDatabase();

            // Get session first to find node ID
            var session = await GetSessionAsync(sessionToken);
            if (session == null)
            {
                return false; // Already removed or doesn't exist
            }

            var sessionKey = GetSessionKey(sessionToken);
            var nodeSessionsKey = GetNodeSessionsKey(session.NodeId);
            var rateLimitKey = GetRateLimitKey(sessionToken);

            // Use transaction to remove all related keys
            var transaction = db.CreateTransaction();
            var delSessionTask = transaction.KeyDeleteAsync(sessionKey);
            var remSetTask = transaction.SetRemoveAsync(nodeSessionsKey, sessionToken);
            var delRateLimitTask = transaction.KeyDeleteAsync(rateLimitKey);

            var committed = await transaction.ExecuteAsync();

            if (committed)
            {
                _logger.LogInformation("Session {SessionToken} removed for node {NodeId}",
                    sessionToken, session.NodeId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<List<SessionData>> GetNodeSessionsAsync(Guid nodeId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var nodeSessionsKey = GetNodeSessionsKey(nodeId);

            // Get all session tokens for this node
            var tokens = await db.SetMembersAsync(nodeSessionsKey);

            var sessions = new List<SessionData>();

            foreach (var token in tokens)
            {
                var session = await GetSessionAsync(token!);
                if (session != null)
                {
                    sessions.Add(session);
                }
            }

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for node {NodeId}", nodeId);
            return new List<SessionData>();
        }
    }

    public async Task<bool> RecordRequestAsync(string sessionToken, DateTime timestamp)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRateLimitKey(sessionToken);

            // Use sorted set with timestamp as score
            // Automatically maintains chronological order
            var score = timestamp.ToUniversalTime().Ticks;

            await db.SortedSetAddAsync(key, timestamp.ToString("O"), score);

            // Set expiration to 1 hour (rate limit window + buffer)
            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for session {SessionToken}", sessionToken);
            return false;
        }
    }

    public async Task<int> GetRequestCountAsync(string sessionToken, TimeSpan window)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = GetRateLimitKey(sessionToken);

            // Calculate cutoff time
            var cutoff = DateTime.UtcNow - window;
            var minScore = cutoff.Ticks;

            // Remove old entries (cleanup)
            await db.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, minScore);

            // Count entries within window
            var count = await db.SortedSetLengthAsync(key);

            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request count for session {SessionToken}", sessionToken);
            return 0;
        }
    }

    public async Task<int> GetActiveSessionCountAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer();

            // Scan for all session keys (expensive operation, use with caution)
            var pattern = $"{SessionKeyPrefix}*";
            var keys = server.Keys(pattern: pattern);

            var count = 0;
            foreach (var key in keys)
            {
                if (await db.KeyExistsAsync(key))
                {
                    count++;
                }
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session count");
            return 0;
        }
    }

    public Task CleanupExpiredSessionsAsync()
    {
        // Redis automatically removes expired keys via TTL
        // No manual cleanup needed for Redis implementation
        _logger.LogDebug("CleanupExpiredSessionsAsync called - no action needed for Redis (automatic TTL)");
        return Task.CompletedTask;
    }

    // Helper methods for key generation
    private static string GetSessionKey(string sessionToken) => $"{SessionKeyPrefix}{sessionToken}";
    private static string GetNodeSessionsKey(Guid nodeId) => $"{NodeSessionsKeyPrefix}{nodeId}:sessions";
    private static string GetRateLimitKey(string sessionToken) => $"{RateLimitKeyPrefix}{sessionToken}";
}
