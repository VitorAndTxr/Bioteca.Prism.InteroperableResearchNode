using System.Collections.Concurrent;
using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Domain.Entities.Session;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Service.Services.Cache;

/// <summary>
/// In-memory session store (original implementation, used when Redis is disabled)
/// </summary>
public class InMemorySessionStore : ISessionStore
{
    private readonly ILogger<InMemorySessionStore> _logger;

    // In-memory storage
    private readonly ConcurrentDictionary<string, SessionData> _sessions = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();

    public InMemorySessionStore(ILogger<InMemorySessionStore> logger)
    {
        _logger = logger;
    }

    public Task<bool> CreateSessionAsync(SessionData session)
    {
        var success = _sessions.TryAdd(session.SessionToken, session);

        if (success)
        {
            _logger.LogDebug("Session {SessionToken} created in memory", session.SessionToken);
        }

        return Task.FromResult(success);
    }

    public Task<SessionData?> GetSessionAsync(string sessionToken)
    {
        _sessions.TryGetValue(sessionToken, out var session);
        return Task.FromResult(session);
    }

    public Task<bool> UpdateSessionAsync(SessionData session)
    {
        _sessions[session.SessionToken] = session;
        return Task.FromResult(true);
    }

    public Task<bool> RemoveSessionAsync(string sessionToken)
    {
        var removed = _sessions.TryRemove(sessionToken, out _);

        if (removed)
        {
            _requestHistory.TryRemove(sessionToken, out _);
        }

        return Task.FromResult(removed);
    }

    public Task<List<SessionData>> GetNodeSessionsAsync(string nodeId)
    {
        var sessions = _sessions.Values
            .Where(s => s.NodeId == nodeId && s.IsValid())
            .ToList();

        return Task.FromResult(sessions);
    }

    public Task<bool> RecordRequestAsync(string sessionToken, DateTime timestamp)
    {
        var queue = _requestHistory.GetOrAdd(sessionToken, _ => new Queue<DateTime>());
        queue.Enqueue(timestamp);
        return Task.FromResult(true);
    }

    public Task<int> GetRequestCountAsync(string sessionToken, TimeSpan window)
    {
        if (!_requestHistory.TryGetValue(sessionToken, out var queue))
        {
            return Task.FromResult(0);
        }

        var cutoff = DateTime.UtcNow - window;

        // Remove old entries
        while (queue.Count > 0 && queue.Peek() < cutoff)
        {
            queue.Dequeue();
        }

        return Task.FromResult(queue.Count);
    }

    public Task<int> GetActiveSessionCountAsync()
    {
        var count = _sessions.Values.Count(s => s.IsValid());
        return Task.FromResult(count);
    }

    public Task CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _sessions
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        var count = 0;
        foreach (var token in expiredTokens)
        {
            if (_sessions.TryRemove(token, out _))
            {
                _requestHistory.TryRemove(token, out _);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions from memory", count);
        }

        return Task.CompletedTask;
    }
}
