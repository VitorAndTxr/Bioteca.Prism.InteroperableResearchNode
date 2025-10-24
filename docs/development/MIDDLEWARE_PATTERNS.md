# Middleware Execution Patterns

**Last Updated**: 2025-10-24
**Version**: 0.10.0
**Status**: Production

---

## Table of Contents

1. [Overview](#overview)
2. [Middleware Architecture](#middleware-architecture)
3. [PrismEncryptedChannelConnection Middleware](#prismencryptedchannelconnection-middleware)
4. [PrismAuthenticatedSession Middleware](#prismauthorenticatedsession-middleware)
5. [Execution Order](#execution-order)
6. [HttpContext Items Pattern](#httpcontext-items-pattern)
7. [Common Usage Patterns](#common-usage-patterns)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

---

## Overview

PRISM uses **custom middleware attributes** to implement security layers for node-to-node communication. These middleware components:

1. **Decrypt incoming encrypted payloads** using channel symmetric keys
2. **Validate session tokens** and enforce authorization
3. **Apply rate limiting** to prevent abuse
4. **Store request context** in `HttpContext.Items` for controller access

**Key Design Principles**:
- Middleware executes in a **specific order** (encryption → authentication)
- Each middleware **stores context** in `HttpContext.Items` for downstream use
- **Error responses** are automatically generated and encrypted
- **Separation of concerns**: Encryption logic separate from authentication logic

---

## Middleware Architecture

### Middleware Layers

```
┌──────────────────────────────────────────────────────────┐
│                    Incoming Request                       │
│   Headers: X-Channel-Id, X-Session-Id                    │
│   Body: {encrypted payload}                              │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│        PrismEncryptedChannelConnection Middleware         │
│                                                           │
│  1. Extract X-Channel-Id header                          │
│  2. Retrieve channel from Redis/in-memory                │
│  3. Decrypt payload using AES-256-GCM                    │
│  4. Deserialize JSON to TRequest DTO                     │
│  5. Store in HttpContext.Items["DecryptedRequest"]       │
│  6. Store channel context                                │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│         PrismAuthenticatedSession Middleware              │
│                                                           │
│  1. Extract session token (X-Session-Id or body)         │
│  2. Retrieve session from Redis/in-memory                │
│  3. Validate expiration timestamp                        │
│  4. Check capability requirements                        │
│  5. Apply rate limiting (60 req/min)                     │
│  6. Update LastAccessedAt, RequestCount                  │
│  7. Store SessionContext in HttpContext.Items            │
│  8. Add X-Session-Id response header                     │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│                   Controller Action                       │
│                                                           │
│  - Access HttpContext.Items["DecryptedRequest"]          │
│  - Access HttpContext.Items["ChannelContext"]            │
│  - Access HttpContext.Items["SessionContext"]            │
│  - Execute business logic                                │
│  - Return response DTO                                   │
└────────────────────┬─────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────┐
│              Response Encryption (Automatic)              │
│                                                           │
│  - Serialize response DTO to JSON                        │
│  - Encrypt using channel symmetric key                   │
│  - Return encrypted response                             │
└──────────────────────────────────────────────────────────┘
```

### Middleware Types

PRISM implements middleware using **ASP.NET Core Action Filters** (`ActionFilterAttribute`):

1. **PrismEncryptedChannelConnectionAttribute** (`Core/Middleware/Channel/`)
   - Generic middleware: `PrismEncryptedChannelConnection<TRequest>`
   - Decrypts payloads using channel encryption
   - Stores decrypted request in `HttpContext.Items`

2. **PrismAuthenticatedSessionAttribute** (`Middleware/`)
   - Non-generic middleware
   - Validates session tokens
   - Enforces capability-based authorization
   - Applies rate limiting

---

## PrismEncryptedChannelConnection Middleware

### Purpose

Decrypt incoming encrypted payloads using the symmetric key established in Phase 1 (channel establishment).

### Location

`Bioteca.Prism.Core/Middleware/Channel/PrismEncryptedChannelConnectionAttribute.cs`

### Declaration

```csharp
public class PrismEncryptedChannelConnectionAttribute<TRequest> : ActionFilterAttribute
    where TRequest : class, new()
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Implementation
    }
}
```

### Execution Flow

```
1. Extract X-Channel-Id from request header
   ↓
2. Validate header presence → 400 Bad Request if missing
   ↓
3. Query IChannelStore for channel data
   ↓
4. Validate channel existence → 404 Not Found if missing
   ↓
5. Validate channel expiration → 410 Gone if expired
   ↓
6. Read request body stream
   ↓
7. Deserialize to EncryptedPayload DTO
   ↓
8. Decode Base64 ciphertext
   ↓
9. Decrypt using AES-256-GCM (key from channel)
   ↓
10. Deserialize decrypted JSON to TRequest type → 400 Bad Request if invalid
   ↓
11. Store in HttpContext.Items["DecryptedRequest"] = TRequest
   ↓
12. Store in HttpContext.Items["ChannelContext"] = ChannelContext
   ↓
13. Call next middleware (await next())
```

### HttpContext Items

**ChannelContext**:
```csharp
public class ChannelContext
{
    public string ChannelId { get; set; }
    public byte[] SymmetricKey { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**DecryptedRequest**:
```csharp
HttpContext.Items["DecryptedRequest"] = TRequest // Strongly-typed request DTO
```

### Usage Example

```csharp
[HttpPost("whoami")]
[PrismEncryptedChannelConnection<WhoAmIRequest>]
[PrismAuthenticatedSession]
public IActionResult WhoAmI()
{
    // Access decrypted request
    var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;

    // Access channel context
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

    Console.WriteLine($"Channel ID: {channelContext?.ChannelId}");
    Console.WriteLine($"Request Timestamp: {request?.Timestamp}");

    return Ok(new { message = "Session validated" });
}
```

### Error Scenarios

| Error | HTTP Status | Cause | Response |
|-------|------------|-------|----------|
| Missing X-Channel-Id | 400 Bad Request | Header not present | `{"error": "X-Channel-Id header is required"}` |
| Channel Not Found | 404 Not Found | Channel doesn't exist in Redis | `{"error": "Channel not found"}` |
| Channel Expired | 410 Gone | ExpiresAt < now | `{"error": "Channel has expired"}` |
| Decryption Failure | 400 Bad Request | Invalid ciphertext or wrong key | `{"error": "Failed to decrypt payload"}` |
| Deserialization Failure | 400 Bad Request | Malformed JSON or wrong DTO shape | `{"error": "Invalid request format"}` |

---

## PrismAuthenticatedSession Middleware

### Purpose

Validate session tokens, enforce capability-based authorization, and apply rate limiting.

### Location

`Bioteca.Prism.InteroperableResearchNode/Middleware/PrismAuthenticatedSessionAttribute.cs`

### Declaration

```csharp
public class PrismAuthenticatedSessionAttribute : ActionFilterAttribute
{
    public NodeAccessTypeEnum? RequiredCapability { get; set; }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Implementation
    }
}
```

### Execution Flow

```
1. Extract session token from:
   a. X-Session-Id header (RECOMMENDED, v0.10.0+)
   b. DecryptedRequest body (DEPRECATED, will remove in v0.11.0)
   ↓
2. Validate session token presence → 401 Unauthorized if missing
   ↓
3. Query ISessionStore for session data
   ↓
4. Validate session existence → 401 Unauthorized if not found
   ↓
5. Validate expiration (ExpiresAt >= now) → 401 Unauthorized if expired
   ↓
6. Check capability requirements (if specified) → 403 Forbidden if insufficient
   ↓
7. Apply rate limiting (60 req/min via Redis Sorted Sets) → 429 Too Many Requests if exceeded
   ↓
8. Update LastAccessedAt timestamp
   ↓
9. Increment RequestCount
   ↓
10. Store SessionContext in HttpContext.Items
   ↓
11. Add X-Session-Id response header
   ↓
12. Call controller action (await next())
```

### Session Token Extraction (v0.10.0+)

**Priority Order**:
1. **X-Session-Id header** (RECOMMENDED):
   ```http
   X-Session-Id: session-87654321-4321-4321-4321-876543210987
   ```

2. **SessionToken in request body** (DEPRECATED):
   ```json
   {
     "SessionToken": "session-87654321-4321-4321-4321-876543210987",
     "Timestamp": "2025-10-24T10:00:00Z"
   }
   ```

**Migration Path**:
- v0.10.0: Both methods supported (backward compatibility)
- v0.11.0: Body-based token will be removed (breaking change)

### Capability-Based Authorization

**Capability Enum**:
```csharp
public enum NodeAccessTypeEnum
{
    ReadOnly = 0,    // Can read data only
    ReadWrite = 1,   // Can read and write data
    Admin = 2        // Full administrative access
}
```

**Usage Examples**:

```csharp
// No capability requirement (any authenticated session)
[PrismAuthenticatedSession]
public IActionResult WhoAmI() { ... }

// Requires ReadWrite or Admin
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]
public IActionResult SubmitData() { ... }

// Requires Admin only
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public IActionResult GetMetrics() { ... }
```

**Capability Hierarchy**:
- `Admin` has access to `ReadWrite` and `ReadOnly` endpoints
- `ReadWrite` has access to `ReadOnly` endpoints
- `ReadOnly` has access to `ReadOnly` endpoints only

### Rate Limiting

**Algorithm**: Redis Sorted Set (Sliding Window)

**Implementation**:
```
Key: rate-limit:session:{sessionToken}
Members: {timestamp1}: {timestamp1}, {timestamp2}: {timestamp2}, ...
Score: Unix timestamp (milliseconds)
TTL: 60 seconds
```

**Flow**:
1. Calculate window start: `now - 60 seconds`
2. Remove expired entries: `ZREMRANGEBYSCORE key -inf {window_start}`
3. Count remaining entries: `ZCARD key`
4. If count >= 60: Reject request (429 Too Many Requests)
5. Add new entry: `ZADD key {now} {now}`
6. Set TTL: `EXPIRE key 60`

**Benefits**:
- **Sliding window**: More accurate than fixed window
- **Distributed**: Works across multiple node instances
- **Automatic cleanup**: Redis TTL handles expiration
- **Per-session isolation**: Each session has independent limit

### HttpContext Items

**SessionContext**:
```csharp
public class SessionContext
{
    public string SessionToken { get; set; }
    public Guid NodeId { get; set; }
    public string ChannelId { get; set; }
    public List<NodeAccessTypeEnum> Capabilities { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RequestCount { get; set; }
}
```

**Storage**:
```csharp
HttpContext.Items["SessionContext"] = SessionContext
```

### Response Headers

**X-Session-Id** (Automatic):
```http
HTTP/1.1 200 OK
X-Session-Id: session-87654321-4321-4321-4321-876543210987
Content-Type: application/json
```

This header is **automatically added** by the middleware, allowing clients to verify which session was used.

### Error Scenarios

| Error | HTTP Status | Cause | Response |
|-------|------------|-------|----------|
| Missing Session Token | 401 Unauthorized | No X-Session-Id header or body token | `{"error": "Session token is required"}` |
| Session Not Found | 401 Unauthorized | Session doesn't exist in Redis | `{"error": "Invalid session"}` |
| Session Expired | 401 Unauthorized | ExpiresAt < now | `{"error": "Session has expired"}` |
| Insufficient Capabilities | 403 Forbidden | Required capability not present | `{"error": "Insufficient permissions"}` |
| Rate Limit Exceeded | 429 Too Many Requests | > 60 requests in 1 minute | `{"error": "Rate limit exceeded"}` |

---

## Execution Order

### Attribute Declaration Order

**IMPORTANT**: Middleware attributes **MUST** be declared in this order:

```csharp
[PrismEncryptedChannelConnection<TRequest>]  // 1. Decrypt payload first
[PrismAuthenticatedSession]                   // 2. Validate session second
public IActionResult MyAction() { ... }
```

**Rationale**:
1. **Decryption first**: Session token may be in the encrypted payload body
2. **Authentication second**: Requires decrypted request to extract session token

### Execution Timeline

```
Time →

┌─────────────────────────────────────────────────────────────────┐
│ T0: Request arrives at ASP.NET Core pipeline                    │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ T1: PrismEncryptedChannelConnection.OnActionExecutionAsync      │
│     - Extract X-Channel-Id: "channel-12345678"                  │
│     - Query Redis: GET channel:channel-12345678                 │
│     - Read request body stream                                  │
│     - Decrypt payload using AES-256-GCM                         │
│     - Store HttpContext.Items["DecryptedRequest"]               │
│     - Store HttpContext.Items["ChannelContext"]                 │
│     Duration: ~10-20ms (including Redis query)                  │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ T2: PrismAuthenticatedSession.OnActionExecutionAsync            │
│     - Extract X-Session-Id: "session-87654321"                  │
│     - Query Redis: GET session:session-87654321                 │
│     - Validate expiration: ExpiresAt >= now                     │
│     - Check capabilities: AccessLevel >= RequiredCapability     │
│     - Rate limiting: ZCARD rate-limit:session:session-87654321  │
│     - Update: LastAccessedAt, RequestCount                      │
│     - Store HttpContext.Items["SessionContext"]                 │
│     - Add response header: X-Session-Id                         │
│     Duration: ~15-30ms (including Redis operations)             │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ T3: Controller Action Execution                                 │
│     - Access HttpContext.Items["DecryptedRequest"]              │
│     - Access HttpContext.Items["SessionContext"]                │
│     - Execute business logic                                    │
│     - Query database (if needed)                                │
│     - Return response DTO                                       │
│     Duration: Variable (depends on business logic)              │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ T4: Response Encryption (Automatic)                             │
│     - Serialize response DTO to JSON                            │
│     - Retrieve channel key from HttpContext.Items               │
│     - Encrypt JSON using AES-256-GCM                            │
│     - Return encrypted response                                 │
│     Duration: ~5-10ms                                           │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ T5: Response sent to client                                     │
└─────────────────────────────────────────────────────────────────┘

Total Latency: ~30-60ms (middleware overhead) + business logic time
```

### Performance Considerations

**Redis Operations**:
- Channel retrieval: `GET channel:{channelId}` (~5ms)
- Session retrieval: `GET session:{sessionToken}` (~5ms)
- Rate limiting: `ZREMRANGEBYSCORE` + `ZCARD` + `ZADD` + `EXPIRE` (~10ms)
- Session update: `SET session:{sessionToken}` with TTL (~5ms)

**Total Redis Overhead**: ~25ms per request

**Optimization Strategies**:
1. Use Redis connection pooling (StackExchange.Redis default)
2. Enable Redis pipelining for multiple commands
3. Consider local caching for hot channel/session data (with TTL)
4. Monitor Redis latency using `INFO commandstats`

---

## HttpContext Items Pattern

### Overview

PRISM uses `HttpContext.Items` to pass middleware-processed data to controllers without coupling.

### Storage Pattern

```csharp
// Middleware stores data
HttpContext.Items["KeyName"] = value;

// Controller retrieves data
var value = HttpContext.Items["KeyName"] as TargetType;
```

### Standard Keys

| Key | Type | Set By | Description |
|-----|------|--------|-------------|
| `ChannelContext` | `ChannelContext` | `PrismEncryptedChannelConnection` | Channel metadata (ChannelId, SymmetricKey, ExpiresAt) |
| `DecryptedRequest` | `TRequest` (generic) | `PrismEncryptedChannelConnection` | Decrypted and deserialized request DTO |
| `SessionContext` | `SessionContext` | `PrismAuthenticatedSession` | Session metadata (SessionToken, NodeId, Capabilities, etc.) |

### Usage in Controllers

**Basic Pattern**:
```csharp
[HttpPost("whoami")]
[PrismEncryptedChannelConnection<WhoAmIRequest>]
[PrismAuthenticatedSession]
public IActionResult WhoAmI()
{
    // Extract context from HttpContext.Items
    var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

    // Validate extraction
    if (request == null || sessionContext == null)
    {
        return BadRequest(new { error = "Failed to retrieve request context" });
    }

    // Use context in business logic
    Console.WriteLine($"Session: {sessionContext.SessionToken}");
    Console.WriteLine($"Channel: {channelContext?.ChannelId}");
    Console.WriteLine($"Node: {sessionContext.NodeId}");
    Console.WriteLine($"Capabilities: {string.Join(", ", sessionContext.Capabilities)}");

    return Ok(new
    {
        sessionToken = sessionContext.SessionToken,
        expiresAt = sessionContext.ExpiresAt,
        requestCount = sessionContext.RequestCount
    });
}
```

**Helper Extension Method** (Recommended):
```csharp
public static class HttpContextExtensions
{
    public static TRequest GetDecryptedRequest<TRequest>(this HttpContext context)
        where TRequest : class
    {
        return context.Items["DecryptedRequest"] as TRequest;
    }

    public static SessionContext GetSessionContext(this HttpContext context)
    {
        return context.Items["SessionContext"] as SessionContext;
    }

    public static ChannelContext GetChannelContext(this HttpContext context)
    {
        return context.Items["ChannelContext"] as ChannelContext;
    }
}

// Usage in controller
[HttpPost("whoami")]
[PrismEncryptedChannelConnection<WhoAmIRequest>]
[PrismAuthenticatedSession]
public IActionResult WhoAmI()
{
    var request = HttpContext.GetDecryptedRequest<WhoAmIRequest>();
    var session = HttpContext.GetSessionContext();

    return Ok(new { sessionToken = session.SessionToken });
}
```

### Type Safety

**Problem**: `HttpContext.Items` is `IDictionary<object, object?>`, losing type information.

**Solution**: Use explicit casting and null-checking:

```csharp
// ✅ Safe pattern
var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;
if (request == null)
{
    return BadRequest(new { error = "Invalid request context" });
}

// ❌ Unsafe pattern (may throw InvalidCastException)
var request = (WhoAmIRequest)HttpContext.Items["DecryptedRequest"];
```

---

## Common Usage Patterns

### Pattern 1: Basic Authenticated Endpoint

```csharp
[HttpPost("data")]
[PrismEncryptedChannelConnection<DataRequest>]
[PrismAuthenticatedSession]
public async Task<IActionResult> SubmitData()
{
    var request = HttpContext.GetDecryptedRequest<DataRequest>();
    var session = HttpContext.GetSessionContext();

    // Business logic
    await _dataService.SaveAsync(request.Data, session.NodeId);

    return Ok(new { message = "Data saved successfully" });
}
```

### Pattern 2: Capability-Restricted Endpoint

```csharp
[HttpPost("metrics")]
[PrismEncryptedChannelConnection<MetricsRequest>]
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public async Task<IActionResult> GetMetrics()
{
    var session = HttpContext.GetSessionContext();

    // Only Admin sessions can reach this point
    var metrics = await _sessionService.GetMetricsAsync();

    return Ok(metrics);
}
```

### Pattern 3: Conditional Logic Based on Capabilities

```csharp
[HttpGet("research/{id}")]
[PrismEncryptedChannelConnection<EmptyRequest>]
[PrismAuthenticatedSession]
public async Task<IActionResult> GetResearch(Guid id)
{
    var session = HttpContext.GetSessionContext();

    // Load research data
    var research = await _researchService.GetByIdAsync(id);

    // Conditional visibility based on capabilities
    if (session.Capabilities.Contains(NodeAccessTypeEnum.Admin))
    {
        // Return full data (including sensitive fields)
        return Ok(research);
    }
    else
    {
        // Return limited data (public fields only)
        return Ok(new
        {
            research.Id,
            research.Title,
            research.Description
            // Exclude: ParticipantData, RawResults, etc.
        });
    }
}
```

### Pattern 4: Session Renewal

```csharp
[HttpPost("session/renew")]
[PrismEncryptedChannelConnection<RenewRequest>]
[PrismAuthenticatedSession]
public async Task<IActionResult> RenewSession()
{
    var session = HttpContext.GetSessionContext();

    // Extend session TTL by 1 hour
    var newExpiresAt = await _sessionService.RenewSessionAsync(session.SessionToken);

    return Ok(new
    {
        sessionToken = session.SessionToken,
        expiresAt = newExpiresAt,
        message = "Session renewed successfully"
    });
}
```

### Pattern 5: Manual Session Validation (Advanced)

```csharp
[HttpPost("custom")]
[PrismEncryptedChannelConnection<CustomRequest>]
public async Task<IActionResult> CustomEndpoint()
{
    var request = HttpContext.GetDecryptedRequest<CustomRequest>();

    // Manual session validation (bypass middleware)
    if (!string.IsNullOrEmpty(request.SessionToken))
    {
        var session = await _sessionService.ValidateSessionAsync(request.SessionToken);
        if (session == null)
        {
            return Unauthorized(new { error = "Invalid session" });
        }

        // Proceed with authenticated logic
        return Ok(new { message = "Authenticated" });
    }

    // Proceed with unauthenticated logic
    return Ok(new { message = "Unauthenticated access" });
}
```

---

## Error Handling

### Middleware Error Responses

All middleware errors return **JSON responses** with consistent structure:

```json
{
  "error": "Human-readable error message",
  "code": "ERROR_CODE",
  "timestamp": "2025-10-24T10:00:00Z"
}
```

### Error Response Encryption

**IMPORTANT**: Error responses from middleware are **automatically encrypted** if a valid channel exists.

**Flow**:
1. Middleware detects error (e.g., missing session token)
2. Generate error response JSON
3. If `ChannelContext` exists in `HttpContext.Items`: Encrypt response
4. If no channel: Return plain JSON (only for Phase 1 endpoints)
5. Set HTTP status code
6. Return response

### Common Error Patterns

#### Pattern 1: Missing X-Channel-Id Header

**Scenario**: Client forgets to include `X-Channel-Id` header.

**Middleware**: `PrismEncryptedChannelConnection`

**Response**:
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": "X-Channel-Id header is required"
}
```

**Fix**:
```typescript
// ✅ Correct
const response = await fetch('/api/session/whoami', {
  headers: {
    'X-Channel-Id': 'channel-12345678',
    'X-Session-Id': 'session-87654321'
  }
});

// ❌ Incorrect (missing X-Channel-Id)
const response = await fetch('/api/session/whoami', {
  headers: {
    'X-Session-Id': 'session-87654321'
  }
});
```

#### Pattern 2: Channel Expired

**Scenario**: Channel TTL (2 hours) has elapsed.

**Middleware**: `PrismEncryptedChannelConnection`

**Response**:
```http
HTTP/1.1 410 Gone
Content-Type: application/json

{
  "error": "Channel has expired"
}
```

**Fix**: Re-establish channel (Phase 1):
```typescript
// Start new handshake from Phase 1
const channelResponse = await fetch('/api/channel/open', {
  method: 'POST',
  body: JSON.stringify({ publicKey: clientPublicKey })
});
```

#### Pattern 3: Session Expired

**Scenario**: Session TTL (1 hour) has elapsed.

**Middleware**: `PrismAuthenticatedSession`

**Response**:
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "error": "Session has expired"
}
```

**Fix**: Re-authenticate (Phase 3):
```typescript
// Request new challenge and authenticate
const challengeResponse = await fetch('/api/node/challenge', {
  method: 'POST',
  headers: { 'X-Channel-Id': channelId },
  body: encryptedPayload
});

// Complete authentication to get new session
const authResponse = await fetch('/api/node/authenticate', {
  method: 'POST',
  headers: { 'X-Channel-Id': channelId },
  body: encryptedSignedChallenge
});
```

#### Pattern 4: Rate Limit Exceeded

**Scenario**: More than 60 requests in 1 minute.

**Middleware**: `PrismAuthenticatedSession`

**Response**:
```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/json
Retry-After: 60

{
  "error": "Rate limit exceeded",
  "retryAfter": 60
}
```

**Fix**: Implement exponential backoff:
```typescript
async function requestWithRetry(url, options, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    const response = await fetch(url, options);

    if (response.status !== 429) {
      return response;
    }

    // Exponential backoff: 1s, 2s, 4s, ...
    const delay = Math.pow(2, i) * 1000;
    await new Promise(resolve => setTimeout(resolve, delay));
  }

  throw new Error('Rate limit exceeded after retries');
}
```

---

## Best Practices

### 1. Always Use Both Middleware Together

**✅ Correct**:
```csharp
[PrismEncryptedChannelConnection<TRequest>]
[PrismAuthenticatedSession]
public IActionResult MyAction() { ... }
```

**❌ Incorrect** (missing encryption):
```csharp
[PrismAuthenticatedSession]  // Session token in body won't be accessible
public IActionResult MyAction() { ... }
```

### 2. Use X-Session-Id Header (v0.10.0+)

**✅ Recommended** (header-based):
```http
POST /api/session/whoami HTTP/1.1
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321
```

**❌ Deprecated** (body-based, will be removed in v0.11.0):
```http
POST /api/session/whoami HTTP/1.1
X-Channel-Id: channel-12345678

{
  "SessionToken": "session-87654321",
  "Timestamp": "2025-10-24T10:00:00Z"
}
```

### 3. Handle HttpContext.Items Safely

**✅ Safe**:
```csharp
var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;
if (request == null)
{
    return BadRequest(new { error = "Invalid request" });
}
```

**❌ Unsafe**:
```csharp
var request = (WhoAmIRequest)HttpContext.Items["DecryptedRequest"];  // May throw
```

### 4. Use Capability Hierarchy

**✅ Efficient**:
```csharp
// Admin can access ReadWrite endpoints
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]
public IActionResult SubmitData() { ... }  // Admin sessions allowed
```

**❌ Redundant**:
```csharp
// Don't create separate Admin-only endpoints for ReadWrite operations
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public IActionResult AdminSubmitData() { ... }  // Unnecessary duplication
```

### 5. Monitor Redis Performance

**Metrics to Track**:
- Average latency for `GET channel:*` operations
- Average latency for `GET session:*` operations
- Rate limiting operation latency (`ZREMRANGEBYSCORE`, `ZADD`)
- Redis connection pool utilization

**Monitoring Commands**:
```bash
# Redis latency
redis-cli --latency

# Command statistics
redis-cli INFO commandstats

# Key count by pattern
redis-cli --scan --pattern "channel:*" | wc -l
redis-cli --scan --pattern "session:*" | wc -l
```

---

## Troubleshooting

### Issue 1: "DecryptedRequest is null in controller"

**Cause**: Request DTO type mismatch between middleware and controller.

**Diagnosis**:
```csharp
// Middleware expects WhoAmIRequest
[PrismEncryptedChannelConnection<WhoAmIRequest>]

// Controller tries to cast to different type
var request = HttpContext.Items["DecryptedRequest"] as DifferentRequest;  // null
```

**Fix**: Ensure type consistency:
```csharp
[PrismEncryptedChannelConnection<WhoAmIRequest>]
public IActionResult WhoAmI()
{
    var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;  // ✅
    // ...
}
```

### Issue 2: "Session validated but capabilities not enforced"

**Cause**: Missing `RequiredCapability` parameter in middleware attribute.

**Diagnosis**:
```csharp
// This allows any authenticated session (ReadOnly, ReadWrite, Admin)
[PrismAuthenticatedSession]
public IActionResult AdminOperation() { ... }
```

**Fix**: Specify capability requirement:
```csharp
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public IActionResult AdminOperation() { ... }
```

### Issue 3: "Rate limiting not working"

**Causes**:
1. Redis not enabled (`UseRedisForSessions: false`)
2. Redis connection failure
3. Multiple node instances with separate in-memory stores

**Diagnosis**:
```bash
# Check Redis connection
docker exec -it irn-redis-node-a redis-cli -a password PING

# Check rate limit keys
docker exec -it irn-redis-node-a redis-cli -a password KEYS "rate-limit:*"

# Check feature flag
grep "UseRedisForSessions" appsettings.json
```

**Fix**:
```json
{
  "FeatureFlags": {
    "UseRedisForSessions": true  // ✅ Enable Redis
  }
}
```

### Issue 4: "Middleware order causing errors"

**Symptom**: Session token cannot be extracted from body.

**Cause**: Middleware declared in wrong order.

**Diagnosis**:
```csharp
// ❌ Incorrect order
[PrismAuthenticatedSession]                   // Tries to read body first
[PrismEncryptedChannelConnection<TRequest>]   // Decrypts body second (too late)
```

**Fix**:
```csharp
// ✅ Correct order
[PrismEncryptedChannelConnection<TRequest>]   // Decrypt first
[PrismAuthenticatedSession]                   // Validate session second
```

### Issue 5: "High latency on authenticated endpoints"

**Causes**:
1. Redis network latency
2. Inefficient rate limiting algorithm
3. Missing connection pooling

**Diagnosis**:
```bash
# Measure Redis latency
docker exec -it irn-redis-node-a redis-cli --latency

# Check connection pool stats
# Add logging in IRedisConnectionService
```

**Fixes**:
1. **Enable connection pooling**:
   ```csharp
   // StackExchange.Redis uses connection pooling by default
   services.AddSingleton<IConnectionMultiplexer>(sp =>
   {
       var config = ConfigurationOptions.Parse(connectionString);
       config.ConnectTimeout = 5000;
       config.SyncTimeout = 5000;
       return ConnectionMultiplexer.Connect(config);
   });
   ```

2. **Use Redis pipelining** for multiple commands:
   ```csharp
   var batch = redis.CreateBatch();
   var getSessionTask = batch.StringGetAsync($"session:{sessionToken}");
   var rateCheckTask = batch.SortedSetLengthAsync($"rate-limit:session:{sessionToken}");
   batch.Execute();

   var session = await getSessionTask;
   var requestCount = await rateCheckTask;
   ```

3. **Local caching** for hot sessions (with TTL):
   ```csharp
   private static readonly MemoryCache _sessionCache = new MemoryCache(new MemoryCacheOptions());

   public async Task<SessionData> GetSessionAsync(string sessionToken)
   {
       if (_sessionCache.TryGetValue(sessionToken, out SessionData cached))
       {
           return cached;
       }

       var session = await _redis.GetSessionAsync(sessionToken);
       _sessionCache.Set(sessionToken, session, TimeSpan.FromMinutes(5));
       return session;
   }
   ```

---

## Related Documentation

- **User & Session Architecture**: `docs/architecture/USER_SESSION_ARCHITECTURE.md`
- **Security Overview**: `docs/SECURITY_OVERVIEW.md`
- **Phase 4 Session Management**: `docs/workflows/PHASE4_SESSION_FLOW.md`
- **Manual Testing Guide**: `docs/testing/manual-testing-guide.md`

---

**Document Version**: 1.0.0
**Last Reviewed**: 2025-10-24
**Reviewers**: Claude Code
**Next Review**: 2025-11-24
