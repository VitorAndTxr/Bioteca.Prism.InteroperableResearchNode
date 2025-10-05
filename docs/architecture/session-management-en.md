# Session Management Between IRN Nodes

**Status**: ✅ Complete (Phase 4 Implemented)
**Last updated**: 2025-10-05

## Overview

Manages the lifecycle of sessions established between IRN nodes after successful handshake, including creation, maintenance, renewal, and termination.

## Session Lifecycle

```
[Handshake] --> [Active] --> [Idle] --> [Renewed/Expired] --> [Closed]
                   │           │
                   └───────────┘
                   (heartbeat)
```

### Session States

| State | Description | Timeout |
|-------|-------------|---------|
| `CREATING` | Session being established | 30s |
| `ACTIVE` | Active session with recent communication | - |
| `IDLE` | No recent activity, but valid | 5 min |
| `EXPIRING` | Close to expiration, renewal recommended | 2 min |
| `EXPIRED` | Session expired, requires new handshake | - |
| `CLOSING` | Closure in progress | 10s |
| `CLOSED` | Session terminated | - |

## Data Model

### SessionInfo

```csharp
public class SessionInfo
{
    public Guid SessionId { get; set; }
    public Guid LocalNodeId { get; set; }
    public Guid RemoteNodeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public SessionState State { get; set; }

    // Symmetric key for session encryption
    public byte[] SessionKey { get; set; }

    // Refresh token for renewal
    public string RefreshToken { get; set; }

    // Negotiated capabilities
    public SessionCapabilities Capabilities { get; set; }

    // Session metrics
    public SessionMetrics Metrics { get; set; }
}

public enum SessionState
{
    Creating,
    Active,
    Idle,
    Expiring,
    Expired,
    Closing,
    Closed
}

public class SessionCapabilities
{
    public int MaxRequestSize { get; set; }
    public string[] SupportedQueries { get; set; }
    public RateLimitConfig RateLimit { get; set; }
    public bool SupportsStreaming { get; set; }
}

public class SessionMetrics
{
    public long RequestCount { get; set; }
    public long BytesTransferred { get; set; }
    public long ErrorCount { get; set; }
    public TimeSpan AverageLatency { get; set; }
}
```

## Session Operations

### 1. Session Creation

Occurs after successful handshake.

**Request:** `POST /api/node/v1/session/create`

```json
{
  "handshakeToken": "jwt-from-handshake",
  "requestedDuration": 3600,
  "capabilities": {
    "supportsStreaming": true,
    "preferredFormat": "json"
  }
}
```

**Response:**

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "sessionKey": "encrypted-aes-key",
  "expiresAt": "2025-10-01T13:00:00Z",
  "refreshToken": "base64-refresh-token",
  "heartbeatInterval": 300,
  "capabilities": {
    "maxRequestSize": 10485760,
    "supportedQueries": ["metadata", "biosignal"],
    "rateLimit": {
      "requestsPerMinute": 60,
      "burstSize": 10
    }
  }
}
```

### 2. Heartbeat (Keep-Alive)

Keeps the session active and detects disconnections.

**Request:** `POST /api/node/v1/session/{sessionId}/heartbeat`

```json
{
  "timestamp": "2025-10-01T12:05:00Z",
  "metrics": {
    "pendingRequests": 2,
    "queueSize": 0
  }
}
```

**Response:**

```json
{
  "acknowledged": true,
  "serverTime": "2025-10-01T12:05:01Z",
  "sessionState": "active",
  "remainingTime": 2940
}
```

### 3. Session Renewal

Extends session validity without complete new handshake.

**Request:** `POST /api/node/v1/session/{sessionId}/renew`

```json
{
  "refreshToken": "base64-refresh-token",
  "requestedDuration": 3600
}
```

**Response:**

```json
{
  "newExpiresAt": "2025-10-01T14:00:00Z",
  "newRefreshToken": "new-base64-refresh-token",
  "sessionKey": "optionally-rotated-key"
}
```

### 4. Session Termination

Gracefully terminates a session.

**Request:** `DELETE /api/node/v1/session/{sessionId}`

```json
{
  "reason": "normal_closure",
  "message": "Session completed successfully"
}
```

**Response:**

```json
{
  "acknowledged": true,
  "finalMetrics": {
    "totalRequests": 142,
    "totalBytesTransferred": 5242880,
    "sessionDuration": 3542,
    "averageLatency": 125
  }
}
```

## Session Storage

### Option 1: In-Memory (MVP)

```csharp
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, SessionInfo> _sessions;
    private readonly ILogger<InMemorySessionStore> _logger;

    public Task<SessionInfo> GetSessionAsync(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task StoreSessionAsync(SessionInfo session)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
```

### Option 2: Redis (Production)

- Persistence
- Sharing between instances
- Automatic TTL

```csharp
public class RedisSessionStore : ISessionStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionStore> _logger;

    public async Task<SessionInfo> GetSessionAsync(Guid sessionId)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"session:{sessionId}");
        return json.HasValue
            ? JsonSerializer.Deserialize<SessionInfo>(json)
            : null;
    }

    // ... complete implementation
}
```

## Background Services

### SessionCleanupService

Removes expired sessions periodically.

```csharp
public class SessionCleanupService : BackgroundService
{
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredSessionsAsync();
            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _sessionStore.GetExpiredSessionsAsync();

        foreach (var session in expiredSessions)
        {
            _logger.LogInformation(
                "Cleaning up expired session {SessionId} from node {NodeId}",
                session.SessionId, session.RemoteNodeId);

            await _sessionStore.DeleteSessionAsync(session.SessionId);
        }
    }
}
```

### SessionMonitoringService

Monitors health of active sessions.

```csharp
public class SessionMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await MonitorActiveSessionsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task MonitorActiveSessionsAsync()
    {
        var sessions = await _sessionStore.GetActiveSessionsAsync();

        foreach (var session in sessions)
        {
            // Check if heartbeat is delayed
            if (DateTime.UtcNow - session.LastActivityAt > TimeSpan.FromMinutes(6))
            {
                _logger.LogWarning(
                    "Session {SessionId} has not received heartbeat for over 6 minutes",
                    session.SessionId);

                session.State = SessionState.Idle;
                await _sessionStore.UpdateSessionAsync(session);
            }

            // Check if close to expiration
            if (session.ExpiresAt - DateTime.UtcNow < TimeSpan.FromMinutes(2))
            {
                session.State = SessionState.Expiring;
                await _sessionStore.UpdateSessionAsync(session);

                // Optionally, notify remote node
                await NotifySessionExpiringAsync(session);
            }
        }
    }
}
```

## Session Middleware

```csharp
public class NodeSessionMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, ISessionStore sessionStore)
    {
        // Endpoints that require active session
        if (context.Request.Path.StartsWithSegments("/api/node/v1/query"))
        {
            if (!context.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdValue))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Missing session ID" });
                return;
            }

            if (!Guid.TryParse(sessionIdValue, out var sessionId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid session ID format" });
                return;
            }

            var session = await sessionStore.GetSessionAsync(sessionId);

            if (session == null || session.State != SessionState.Active)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
                return;
            }

            // Update last activity
            session.LastActivityAt = DateTime.UtcNow;
            session.Metrics.RequestCount++;
            await sessionStore.UpdateSessionAsync(session);

            // Add session to context for use in controllers
            context.Items["Session"] = session;
        }

        await _next(context);
    }
}
```

## Rate Limiting per Session

```csharp
public class SessionRateLimiter
{
    private readonly ConcurrentDictionary<Guid, TokenBucket> _buckets = new();

    public bool TryConsume(SessionInfo session)
    {
        var bucket = _buckets.GetOrAdd(session.SessionId, _ =>
            new TokenBucket(
                session.Capabilities.RateLimit.RequestsPerMinute,
                session.Capabilities.RateLimit.BurstSize));

        return bucket.TryConsume();
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private readonly int _refillRate;
    private int _tokens;
    private DateTime _lastRefill;

    public TokenBucket(int refillRate, int capacity)
    {
        _refillRate = refillRate;
        _capacity = capacity;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        Refill();

        if (_tokens > 0)
        {
            _tokens--;
            return true;
        }

        return false;
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefill;
        var tokensToAdd = (int)(elapsed.TotalMinutes * _refillRate);

        if (tokensToAdd > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
```

## Implementation

### Current Status
✅ **Complete** - Phase 4 session management implemented with Redis persistence

### Implementation Details

**Implemented Components:**
- ✅ `SessionService` - Complete session lifecycle management
- ✅ `RedisSessionStore` - Redis persistence with automatic TTL
- ✅ `InMemorySessionStore` - Fallback implementation
- ✅ `PrismAuthenticatedSessionAttribute` - Session validation middleware
- ✅ `SessionController` - Session endpoints (whoami, renew, revoke, metrics)
- ✅ Capability-based authorization (ReadOnly, ReadWrite, Admin)
- ✅ Rate limiting (60 requests/minute) using Redis Sorted Sets
- ✅ Session metrics and monitoring

**Key Features:**
- Session tokens generated as GUID (one-time use)
- 1-hour default TTL (3600 seconds)
- Automatic expiration via Redis TTL
- All requests encrypted via AES-256-GCM channel
- Session token sent inside encrypted payload (NOT in HTTP headers)

### Configuration

Added in `appsettings.json`:

```json
{
  "SessionManagement": {
    "DefaultDuration": 3600,
    "MaxDuration": 86400,
    "HeartbeatInterval": 300,
    "CleanupInterval": 300,
    "Store": "Redis"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=prism-redis-password,abortConnect=false",
    "EnableRedis": true
  },
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

## Tests

### Test Scenarios

1. ✅ **Creation and normal use**
2. ✅ **Expiration by timeout**
3. ✅ **Successful renewal**
4. ✅ **Heartbeat keeps session alive**
5. ✅ **Rate limiting works correctly**
6. ✅ **Cleanup removes expired sessions**
7. ✅ **Multiple simultaneous sessions**

**Test Coverage:** 8/8 Phase 4 tests passing (100%)

## AI Context

### Suggested Prompt

```
Implement session management according to docs/architecture/session-management-en.md.
Start with the ISessionStore interface and InMemorySessionStore implementation, then create
the SessionManagementService and NodeSessionController.
```

### Dependencies
- Depends on: `handshake-protocol.md` (session created after handshake)
- Depends on: `node-communication.md` (general architecture)

## References

- OAuth 2.0 Token Management
- Session Management Best Practices
- Token Bucket Algorithm
- Redis Session Store Patterns
