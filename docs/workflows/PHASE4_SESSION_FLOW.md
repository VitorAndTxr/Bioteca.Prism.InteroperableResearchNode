# Phase 4: Session Management Flow

**Phase**: 4 of 4
**Purpose**: Manage authenticated sessions with capability-based authorization and rate limiting
**Security**: Bearer tokens + Capability-based access + Rate limiting (60 req/min)

---

## Overview

Phase 4 provides session lifecycle management after successful authentication. Sessions use bearer tokens with automatic expiration, capability-based authorization, and distributed rate limiting.

**Prerequisites**:
- Phase 1 encrypted channel established
- Phase 2 node identification complete (status: Authorized)
- Phase 3 authentication complete (session token received)

**IMPORTANT**: All Phase 4 requests MUST be encrypted via the channel. Session token is sent **inside** the encrypted payload, NOT in HTTP headers.

---

## Session Token Structure

**Format**: GUID (UUID v4)
```
Example: a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890

Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
- Version 4 (random): "4" in third group
- Variant bits: "8", "9", "a", or "b" in fourth group
- Uniqueness: 2^122 possible values
```

**Generation**:
```csharp
var sessionToken = Guid.NewGuid().ToString();
```

---

## Session Lifecycle

### 1. Session Creation (Automatic in Phase 3)

**Server Action** (during Phase 3 authentication):
```csharp
// ChallengeService.Authenticate() → SessionService.CreateSessionAsync()

var session = new SessionContext
{
    SessionToken = Guid.NewGuid().ToString(),
    NodeId = nodeId,  // Protocol-level identifier (string)
    RegistrationId = node.Id,  // Database primary key (Guid)
    ChannelId = channelId,
    NodeAccessLevel = node.NodeAccessLevel,  // ReadOnly, ReadWrite, or Admin
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddHours(1)  // 1-hour TTL
};

await _sessionStore.StoreAsync(session);
return session;
```

**Response** (encrypted, sent in Phase 3):
```json
{
  "authenticated": true,
  "sessionToken": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "sessionExpiresAt": "2025-10-21T11:30:00Z",
  "grantedCapabilities": ["query:read", "data:write"],
  "nextPhase": "phase4_session"
}
```

---

### 2. Session Validation (Every Phase 4 Request)

**Middleware**: `PrismAuthenticatedSessionAttribute`

```csharp
// Applied to all Phase 4 endpoints
[HttpPost("whoami")]
[PrismEncryptedChannelConnection<WhoAmIRequest>]  // Step 1: Decrypt payload
[PrismAuthenticatedSession]  // Step 2: Validate session
public IActionResult WhoAmI()
{
    var session = HttpContext.Items["SessionContext"] as SessionContext;
    // ... process request
}
```

**Validation Flow**:
```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
    // 1. Retrieve decrypted request (from PrismEncryptedChannelConnectionAttribute)
    var request = context.HttpContext.Items["DecryptedRequest"] as dynamic;
    if (request == null)
        return Unauthorized("Request not decrypted");

    // 2. Extract session token from PAYLOAD (not header)
    var sessionToken = request.SessionToken;
    if (string.IsNullOrEmpty(sessionToken))
        return Unauthorized("Session token missing");

    // 3. Retrieve session from store (Redis or in-memory)
    var session = await _sessionStore.GetAsync(sessionToken);
    if (session == null)
        return Unauthorized("Session not found");

    // 4. Check expiration
    if (session.ExpiresAt < DateTime.UtcNow)
    {
        await _sessionStore.DeleteAsync(sessionToken);
        return Unauthorized("Session expired");
    }

    // 5. Check required capability (if specified)
    if (RequiredCapability.HasValue)
    {
        if (!HasCapability(session.NodeAccessLevel, RequiredCapability.Value))
            return Forbidden("Insufficient permissions");
    }

    // 6. Enforce rate limiting (60 requests/minute)
    var allowed = await _rateLimiter.AllowRequestAsync(sessionToken);
    if (!allowed)
        return TooManyRequests("Rate limit exceeded (60 requests/minute)");

    // 7. Store session context for controller
    context.HttpContext.Items["SessionContext"] = session;

    // 8. Continue to controller action
    await next();
}
```

---

## Capability-Based Authorization

### Access Levels (NodeAccessTypeEnum)

```csharp
public enum NodeAccessTypeEnum
{
    ReadOnly = 0,   // Level 1: Query federated data
    ReadWrite = 1,  // Level 2: Submit and modify research data
    Admin = 2       // Level 3: Full node administration
}
```

### Capability Hierarchy

```
Admin (Level 3)
├─ admin:node - Manage node settings
├─ admin:users - Manage user access
├─ session:metrics - View system metrics
├─ Inherits: All ReadWrite capabilities
│
ReadWrite (Level 2)
├─ data:write - Submit research data
├─ data:update - Update owned data
├─ Inherits: All ReadOnly capabilities
│
ReadOnly (Level 1)
├─ query:read - Execute federated queries
└─ Restrictions: Cannot modify data, no admin operations
```

### Enforcing Capability Requirements

```csharp
// Endpoint requiring specific capability
[HttpPost("submit-data")]
[PrismEncryptedChannelConnection<DataSubmitRequest>]
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]
public async Task<IActionResult> SubmitData()
{
    var session = HttpContext.Items["SessionContext"] as SessionContext;

    // session.NodeAccessLevel will be ReadWrite or Admin
    // ReadOnly level will be rejected by middleware

    // Process data submission
    return Ok(new { success = true });
}

// Admin-only endpoint
[HttpPost("metrics")]
[PrismEncryptedChannelConnection<MetricsRequest>]
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public async Task<IActionResult> GetMetrics()
{
    // Only Admin level can access
    return Ok(await _sessionService.GetSessionMetricsAsync());
}
```

---

## Rate Limiting

### Algorithm: Token Bucket (Redis Sorted Sets)

**Parameters**:
- **Capacity**: 60 requests
- **Refill Rate**: 60 requests per minute (1 request/second)
- **Window**: Rolling 60-second window
- **Storage**: Redis Sorted Set with request timestamps
- **Key Pattern**: `session:ratelimit:{sessionToken}`

### Implementation

```csharp
public async Task<bool> AllowRequestAsync(string sessionToken)
{
    var db = _redis.GetDatabase();
    var key = $"session:ratelimit:{sessionToken}";

    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var windowStart = now - 60000;  // 60 seconds ago

    // 1. Remove expired timestamps (older than 60 seconds)
    await db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);

    // 2. Count requests in current window
    var count = await db.SortedSetLengthAsync(key);

    // 3. Check if limit exceeded
    if (count >= 60)
        return false;  // Rate limit exceeded

    // 4. Add current timestamp to set
    await db.SortedSetAddAsync(key, now, now);

    // 5. Set expiration on key (cleanup)
    await db.KeyExpireAsync(key, TimeSpan.FromMinutes(5));

    return true;  // Request allowed
}
```

### Benefits

- ✅ **Smooth Rate Limiting**: Not bursty (rolling window)
- ✅ **Distributed**: Shared state across multiple nodes
- ✅ **Automatic Cleanup**: Expired timestamps removed automatically
- ✅ **Scalable**: Redis Sorted Sets handle high concurrency

---

## Session Operations

### WhoAmI (Get Current Session Info)

**Client Request**:
```csharp
var request = new WhoAmIRequest
{
    ChannelId = channelId,
    SessionToken = sessionToken,
    Timestamp = DateTime.UtcNow.ToString("O")
};

var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);
var response = await httpClient.PostAsync("/api/session/whoami", encrypted);
```

**HTTP Request**:
```http
POST /api/session/whoami HTTP/1.1
X-Channel-Id: f47ac10b-58cc-4372-a567-0e02b2c3d479
Content-Type: application/json

{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}

# Decrypted payload:
{
  "channelId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "sessionToken": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "timestamp": "2025-10-21T10:45:00Z"
}
```

**Server Response** (encrypted):
```json
{
  "sessionToken": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "nodeId": "node-a",
  "registrationId": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",
  "accessLevel": "ReadWrite",
  "capabilities": ["query:read", "data:write", "data:update"],
  "createdAt": "2025-10-21T10:30:00Z",
  "expiresAt": "2025-10-21T11:30:00Z",
  "remainingTtl": 2700  // seconds
}
```

---

### Renew Session (Extend TTL)

**Client Request**:
```csharp
var request = new RenewSessionRequest
{
    ChannelId = channelId,
    SessionToken = sessionToken,
    Timestamp = DateTime.UtcNow.ToString("O")
};

var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);
var response = await httpClient.PostAsync("/api/session/renew", encrypted);
```

**Server Action**:
```csharp
[HttpPost("renew")]
[PrismEncryptedChannelConnection<RenewSessionRequest>]
[PrismAuthenticatedSession]
public async Task<IActionResult> RenewSession()
{
    var session = HttpContext.Items["SessionContext"] as SessionContext;

    // Extend expiration by 1 hour from current time
    session.ExpiresAt = DateTime.UtcNow.AddHours(1);
    await _sessionStore.UpdateAsync(session);

    var response = new RenewSessionResponse
    {
        SessionToken = session.SessionToken,
        NewExpiresAt = session.ExpiresAt,
        ExtendedBy = 3600  // seconds
    };

    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
```

**Response** (encrypted):
```json
{
  "sessionToken": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "newExpiresAt": "2025-10-21T12:45:00Z",
  "extendedBy": 3600
}
```

---

### Revoke Session (Logout)

**Client Request**:
```csharp
var request = new RevokeSessionRequest
{
    ChannelId = channelId,
    SessionToken = sessionToken,
    Timestamp = DateTime.UtcNow.ToString("O")
};

var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);
var response = await httpClient.PostAsync("/api/session/revoke", encrypted);
```

**Server Action**:
```csharp
[HttpPost("revoke")]
[PrismEncryptedChannelConnection<RevokeSessionRequest>]
[PrismAuthenticatedSession]
public async Task<IActionResult> RevokeSession()
{
    var session = HttpContext.Items["SessionContext"] as SessionContext;

    // Delete session immediately
    await _sessionStore.DeleteAsync(session.SessionToken);

    // Delete rate limit data
    await _rateLimiter.ClearAsync(session.SessionToken);

    var response = new RevokeSessionResponse
    {
        Revoked = true,
        RevokedAt = DateTime.UtcNow
    };

    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
```

**Response** (encrypted):
```json
{
  "revoked": true,
  "revokedAt": "2025-10-21T10:50:00Z"
}
```

---

### Get Session Metrics (Admin Only)

**Client Request**:
```csharp
var request = new SessionMetricsRequest
{
    ChannelId = channelId,
    SessionToken = sessionToken,
    Timestamp = DateTime.UtcNow.ToString("O")
};

var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);
var response = await httpClient.PostAsync("/api/session/metrics", encrypted);
```

**Server Action**:
```csharp
[HttpPost("metrics")]
[PrismEncryptedChannelConnection<SessionMetricsRequest>]
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public async Task<IActionResult> GetSessionMetrics()
{
    // Only Admin level can access this endpoint

    var metrics = await _sessionService.GetSessionMetricsAsync();

    var response = new SessionMetricsResponse
    {
        TotalActiveSessions = metrics.TotalActiveSessions,
        SessionsByAccessLevel = metrics.SessionsByAccessLevel,
        AverageSessionDuration = metrics.AverageSessionDuration,
        RequestsPerMinute = metrics.RequestsPerMinute
    };

    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
```

**Response** (encrypted):
```json
{
  "totalActiveSessions": 5,
  "sessionsByAccessLevel": {
    "ReadOnly": 2,
    "ReadWrite": 2,
    "Admin": 1
  },
  "averageSessionDuration": 1850,  // seconds
  "requestsPerMinute": 45.2
}
```

---

## Session Storage

### Redis Storage (Production)

```csharp
public async Task StoreAsync(SessionContext session)
{
    var db = _redis.GetDatabase();
    var key = $"session:{session.SessionToken}";

    // Store session as JSON
    var json = JsonSerializer.Serialize(session);
    await db.StringSetAsync(key, json, TimeSpan.FromHours(1));

    // Add to node's session set (for listing)
    var nodeKey = $"session:node:{session.NodeId}:sessions";
    await db.SetAddAsync(nodeKey, session.SessionToken);
}

public async Task<SessionContext?> GetAsync(string sessionToken)
{
    var db = _redis.GetDatabase();
    var key = $"session:{sessionToken}";

    var json = await db.StringGetAsync(key);
    if (!json.HasValue) return null;

    return JsonSerializer.Deserialize<SessionContext>((string)json);
}

public async Task UpdateAsync(SessionContext session)
{
    // Re-store with updated TTL
    await StoreAsync(session);
}

public async Task DeleteAsync(string sessionToken)
{
    var db = _redis.GetDatabase();
    var key = $"session:{sessionToken}";

    // Get session to find nodeId
    var json = await db.StringGetAsync(key);
    if (json.HasValue)
    {
        var session = JsonSerializer.Deserialize<SessionContext>((string)json);

        // Remove from node's session set
        var nodeKey = $"session:node:{session.NodeId}:sessions";
        await db.SetRemoveAsync(nodeKey, sessionToken);
    }

    // Delete session
    await db.KeyDeleteAsync(key);
}
```

---

### In-Memory Storage (Fallback)

```csharp
private readonly ConcurrentDictionary<string, SessionContext> _sessions = new();

public async Task StoreAsync(SessionContext session)
{
    _sessions[session.SessionToken] = session;

    // Schedule automatic cleanup after expiration
    _ = Task.Run(async () =>
    {
        var ttl = session.ExpiresAt - DateTime.UtcNow;
        if (ttl.TotalMilliseconds > 0)
            await Task.Delay(ttl);

        _sessions.TryRemove(session.SessionToken, out _);
    });
}

public async Task<SessionContext?> GetAsync(string sessionToken)
{
    if (_sessions.TryGetValue(sessionToken, out var session))
    {
        if (session.ExpiresAt > DateTime.UtcNow)
            return session;

        // Expired, remove
        _sessions.TryRemove(sessionToken, out _);
    }
    return null;
}
```

---

## Testing

### Automated Tests

**Phase 4 Tests** (8/8 passing):
```csharp
[Fact] public async Task WhoAmI_ValidSession_ShouldReturnSessionInfo()
[Fact] public async Task WhoAmI_ExpiredSession_ShouldReject()
[Fact] public async Task RenewSession_ShouldExtendTTL()
[Fact] public async Task RevokeSession_ShouldDeleteSession()
[Fact] public async Task RateLimit_ExceedLimit_ShouldReject()
[Fact] public async Task Authorization_ReadOnlyNode_CannotSubmitData()
[Fact] public async Task Authorization_AdminNode_CanAccessMetrics()
[Fact] public async Task SessionExpiration_ShouldAutoRemove()
```

---

### End-to-End Test Script

```bash
#!/bin/bash
# test-phase4.sh

echo "=== Complete End-to-End Test (Phases 1→2→3→4) ==="

# Phase 1: Encrypted Channel
source test-phase1.sh

# Phase 2: Node Identification
source test-phase2.sh

# Phase 3: Mutual Authentication
source test-phase3.sh

# Phase 4: Session Management
echo "=== Phase 4: Session Management ==="

# Test WhoAmI
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}"

echo "WhoAmI successful"

# Test Renew
curl -X POST http://localhost:5000/api/session/renew \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}"

echo "Session renewed"

# Test Rate Limiting (send 65 requests)
for i in {1..65}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST http://localhost:5000/api/session/whoami \
    -H "X-Channel-Id: $CHANNEL_ID" \
    -H "Content-Type: application/json" \
    -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}")

  if [ "$STATUS" == "429" ]; then
    echo "Rate limit enforced at request $i"
    break
  fi
done

# Test Revoke
curl -X POST http://localhost:5000/api/session/revoke \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}"

echo "Session revoked"
echo "=== All Phases Complete ==="
```

---

## Common Issues

### Issue: "Session token missing"

**Cause**: Session token sent in HTTP header instead of encrypted payload

**Solution**:
```csharp
// WRONG: Session token in header
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {sessionToken}");

// CORRECT: Session token in encrypted payload
var request = new WhoAmIRequest
{
    SessionToken = sessionToken,  // Inside payload
    ChannelId = channelId,
    Timestamp = DateTime.UtcNow.ToString("O")
};
var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);
```

---

### Issue: "Rate limit exceeded"

**Cause**: More than 60 requests in 60-second window

**Solution**:
1. Implement exponential backoff
2. Cache responses when possible
3. Batch operations to reduce request count

---

### Issue: "Insufficient permissions"

**Cause**: Session's access level too low for requested operation

**Solution**:
- Check `session.NodeAccessLevel` before making request
- Request higher access level during registration (Phase 2)
- Contact administrator to upgrade access level

---

### Issue: "Session expired"

**Cause**: More than 1 hour elapsed since creation/renewal

**Solution**:
1. Renew session periodically (e.g., every 30 minutes)
2. Handle 401 Unauthorized by re-authenticating (Phase 3)

---

## Security Considerations

### Session Hijacking Prevention

- ✅ **Encrypted Payloads**: Session tokens never transmitted in plaintext
- ✅ **Short TTL**: 1-hour expiration limits exposure window
- ✅ **Automatic Cleanup**: Expired sessions removed from storage
- ✅ **Channel Binding**: Session tied to specific channel ID

### Distributed Denial of Service (DDoS) Protection

- ✅ **Rate Limiting**: 60 requests/minute per session
- ✅ **Distributed State**: Redis Sorted Sets for multi-node enforcement
- ✅ **Automatic Expiration**: Redis TTL prevents memory leaks

### Authorization Bypass Prevention

- ✅ **Middleware Enforcement**: `PrismAuthenticatedSessionAttribute` runs before controller
- ✅ **Capability Hierarchy**: Admin > ReadWrite > ReadOnly enforced
- ✅ **No Client-Side Capability Checks**: Server authorizes all requests

---

## Next Steps

Phase 4 is the final phase of the handshake protocol. With a valid session, nodes can now:

**Phase 5: Federated Queries** (Planned):
- Execute cross-node queries (`POST /api/query/execute`)
- Submit research data (`POST /api/data/submit`)
- Aggregate results from multiple nodes

---

## Documentation References

- **Security Overview**: `docs/SECURITY_OVERVIEW.md`
- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Phase 4 Architecture**: `docs/architecture/phase4-session-management.md`
- **Testing Guide**: `docs/testing/manual-testing-guide.md`
- **Phase 3 Flow**: `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`
