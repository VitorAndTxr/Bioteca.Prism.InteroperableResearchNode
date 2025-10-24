# Phase 4: Session Management

Phase 4 provides session management capabilities for authenticated nodes. All endpoints require both channel encryption and session authentication.

## Overview

**Purpose**: Manage authenticated sessions for node operations
**Security**: Bearer token + encrypted channel + rate limiting
**Prerequisites**:
- Completed Phase 1-3 (channel + identification + authentication)
- Valid session token from Phase 3
**Session Duration**: 1 hour (renewable)

## ⚠️ BREAKING CHANGE in v0.10.0

**Session tokens are moving from request body to HTTP header!**

### Migration Timeline
- **v0.10.0** (Current): Dual support - both patterns work
- **v0.11.0** (Future): Header-only - body pattern removed

### Quick Migration Guide
```diff
POST /api/session/whoami
X-Channel-Id: abc-123
+ X-Session-Id: def-456  // NEW: Add this header

{
-  "sessionToken": "def-456",  // DEPRECATED: Remove from body
  "timestamp": "2025-10-23T10:00:00Z"
}
```

See [Migration Guide](migration-guide.md) for complete details.

## Authentication Requirements

All Phase 4 endpoints require:

### Headers (v0.10.0+)
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}  // NEW IN v0.10.0
Content-Type: application/json
```

### Headers (v0.9.x - Deprecated)
```http
X-Channel-Id: {channelId}
Content-Type: application/json
```

## Endpoints

### POST /api/session/whoami

Returns information about the current session.

#### Request

**Headers (v0.10.0+)**:
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Legacy Body (v0.9.x - Deprecated)**:
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",  // DEPRECATED
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

#### Response

**Status**: 200 OK

**Headers**:
```http
X-Session-Id: {sessionToken}  // Echo back for confirmation
```

**Body** (encrypted via channel):
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "expiresAt": "2025-10-23T11:00:00Z",
  "remainingSeconds": 3542,
  "capabilities": "ReadWrite",
  "requestCount": 15,
  "timestamp": "2025-10-23T10:00:58Z"
}
```

**Field Descriptions**:
- `sessionToken`: Current session identifier
- `nodeId`: Authenticated node's registration ID
- `channelId`: Associated encrypted channel
- `expiresAt`: Session expiration time
- `remainingSeconds`: Time until expiration
- `capabilities`: Access level (ReadOnly/ReadWrite/Admin)
- `requestCount`: Total requests in this session
- `timestamp`: Server timestamp

#### Error Responses

**401 Unauthorized** - No session context
```json
{
  "error": "ERR_NO_SESSION_CONTEXT"
}
```

**401 Unauthorized** - Session expired
```json
{
  "error": {
    "code": "ERR_SESSION_EXPIRED",
    "message": "Session has expired. Please re-authenticate.",
    "retryable": true
  }
}
```

#### curl Examples

**v0.10.0+ (With Header)**:
```bash
# New pattern - session token in header
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

**v0.9.x (Legacy)**:
```bash
# Old pattern - session token in body (DEPRECATED)
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionToken": "'$SESSION_TOKEN'",
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

#### C# Client Example

```csharp
// v0.10.0+ Pattern
public async Task<WhoAmIResponse> GetSessionInfoAsync(
    string channelId,
    string sessionToken,
    byte[] symmetricKey)
{
    var request = new WhoAmIRequest
    {
        ChannelId = channelId,
        Timestamp = DateTime.UtcNow
        // Note: sessionToken removed from body
    };

    var encryptedPayload = EncryptPayload(request, symmetricKey);

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/whoami");
    httpRequest.Headers.Add("X-Channel-Id", channelId);
    httpRequest.Headers.Add("X-Session-Id", sessionToken);  // NEW
    httpRequest.Content = JsonContent.Create(encryptedPayload);

    var response = await httpClient.SendAsync(httpRequest);

    var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
    return DecryptPayload<WhoAmIResponse>(encryptedResponse, symmetricKey);
}
```

---

### POST /api/session/renew

Extends the session TTL by the specified duration.

#### Request

**Headers (v0.10.0+)**:
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "additionalSeconds": 3600,
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Legacy Body (v0.9.x - Deprecated)**:
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",  // DEPRECATED
  "additionalSeconds": 3600,
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Field Descriptions**:
- `additionalSeconds`: Time to add (default: 3600)
- `timestamp`: Current UTC timestamp

#### Response

**Status**: 200 OK

**Body** (encrypted via channel):
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "expiresAt": "2025-10-23T12:00:00Z",
  "remainingSeconds": 7200,
  "message": "Session renewed for 3600 seconds",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

#### Error Response

**401 Unauthorized** - Renewal failed
```json
{
  "error": "ERR_SESSION_RENEWAL_FAILED",
  "message": "Session could not be renewed"
}
```

#### curl Example

```bash
# v0.10.0+ (With Header)
curl -X POST http://localhost:5000/api/session/renew \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "additionalSeconds": 3600,
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

---

### POST /api/session/revoke

Revokes the current session (logout).

#### Request

**Headers (v0.10.0+)**:
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "reason": "Normal logout",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Legacy Body (v0.9.x - Deprecated)**:
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",  // DEPRECATED
  "reason": "Normal logout",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

#### Response

**Status**: 200 OK

**Body** (encrypted via channel):
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "revoked": true,
  "message": "Session revoked successfully",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

#### Error Response

**400 Bad Request** - Revocation failed
```json
{
  "error": "ERR_SESSION_REVOCATION_FAILED",
  "message": "Session could not be revoked"
}
```

#### curl Example

```bash
# v0.10.0+ (With Header)
curl -X POST http://localhost:5000/api/session/revoke \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Normal logout",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

---

### POST /api/session/metrics

Returns session metrics for the specified node (requires Admin capability).

#### Request

**Headers (v0.10.0+)**:
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Field Descriptions**:
- `nodeId`: Target node ID (optional, defaults to current)
- `timestamp`: Current UTC timestamp

#### Response

**Status**: 200 OK

**Body** (encrypted via channel):
```json
{
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "activeSessions": 3,
  "totalSessions": 47,
  "sessionsLast24h": 12,
  "averageSessionDuration": 2847,
  "requestsPerMinute": {
    "current": 8,
    "average": 5.4,
    "peak": 23
  },
  "sessionsByCapability": {
    "ReadOnly": 1,
    "ReadWrite": 2,
    "Admin": 0
  },
  "timestamp": "2025-10-23T10:00:00Z"
}
```

#### Error Response

**403 Forbidden** - Insufficient privileges
```json
{
  "error": {
    "code": "ERR_INSUFFICIENT_CAPABILITY",
    "message": "Admin capability required",
    "requiredCapability": "Admin",
    "currentCapability": "ReadWrite"
  }
}
```

#### curl Example

```bash
# v0.10.0+ (With Header) - Requires Admin capability
curl -X POST http://localhost:5000/api/session/metrics \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $ADMIN_SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nodeId": "'$TARGET_NODE_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

---

## Rate Limiting

Phase 4 endpoints implement sliding window rate limiting:

### Configuration
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 60,
    "WindowSizeMinutes": 1,
    "Implementation": "Redis Sorted Sets"
  }
}
```

### Rate Limit Response
**Status**: 429 Too Many Requests

**Headers**:
```http
Retry-After: 15
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 2025-10-23T10:01:15Z
```

**Body**:
```json
{
  "error": {
    "code": "ERR_RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Please retry after 15 seconds.",
    "retryable": true,
    "retryAfter": "2025-10-23T10:01:15Z"
  }
}
```

## Session Storage

Sessions are stored in Redis with automatic TTL:

### Redis Key Format
```
session:{sessionToken}
```

### Session Data Structure
```json
{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeId": "a8b7c6d5-e4f3-2109-8765-432109876543",
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "nodeAccessLevel": "ReadWrite",
  "createdAt": "2025-10-23T10:00:00Z",
  "expiresAt": "2025-10-23T11:00:00Z",
  "lastActivityAt": "2025-10-23T10:30:00Z",
  "requestCount": 42
}
```

## Capability-Based Authorization

### Access Levels

| Capability | Description | Allowed Operations |
|------------|-------------|-------------------|
| `ReadOnly` | Read access only | Query data, view status |
| `ReadWrite` | Read and write access | All ReadOnly + modify data |
| `Admin` | Administrative access | All ReadWrite + system management |

### Capability Enforcement

```csharp
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public IActionResult GetMetrics()
{
    // Only accessible with Admin capability
}
```

## Complete Session Workflow

```csharp
public class SessionWorkflow
{
    public async Task<SessionContext> EstablishSessionAsync()
    {
        // Phase 1: Establish channel
        var channelResult = await OpenChannelAsync();

        // Phase 2: Identify node
        var identifyResult = await IdentifyNodeAsync(
            channelResult.ChannelId,
            channelResult.SymmetricKey
        );

        // Phase 3: Authenticate
        var authResult = await AuthenticateAsync(
            channelResult.ChannelId,
            channelResult.SymmetricKey
        );

        // Phase 4: Use session with new header pattern
        var sessionInfo = await GetSessionInfoAsync(
            channelResult.ChannelId,
            authResult.SessionToken,  // Now passed as header
            channelResult.SymmetricKey
        );

        return new SessionContext
        {
            ChannelId = channelResult.ChannelId,
            SessionToken = authResult.SessionToken,
            SymmetricKey = channelResult.SymmetricKey,
            Capabilities = sessionInfo.Capabilities,
            ExpiresAt = sessionInfo.ExpiresAt
        };
    }

    public void ConfigureHttpClient(HttpClient client, SessionContext context)
    {
        // v0.10.0+ pattern
        client.DefaultRequestHeaders.Add("X-Channel-Id", context.ChannelId);
        client.DefaultRequestHeaders.Add("X-Session-Id", context.SessionToken);
    }
}
```

## Testing

### Complete Phase 4 Test

```bash
#!/bin/bash
# test-phase4.sh

NODE_A="http://localhost:5000"
CHANNEL_ID="$1"      # From Phase 1
SESSION_TOKEN="$2"   # From Phase 3

echo "Testing Phase 4: Session Management"
echo "==================================="

# 1. Test whoami (new header pattern)
echo -e "\n1. Testing whoami with X-Session-Id header..."
curl -X POST $NODE_A/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.'

# 2. Test session renewal
echo -e "\n2. Testing session renewal..."
curl -X POST $NODE_A/api/session/renew \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "additionalSeconds": 1800,
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.'

# 3. Test metrics (requires admin)
echo -e "\n3. Testing metrics endpoint..."
curl -X POST $NODE_A/api/session/metrics \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.'

# 4. Test session revocation
echo -e "\n4. Testing session revocation..."
curl -X POST $NODE_A/api/session/revoke \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Test complete",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.'

echo -e "\nPhase 4 test complete!"
```

## Migration Checklist

### For Client Developers

- [ ] Update HTTP client to send `X-Session-Id` header
- [ ] Remove `sessionToken` from request bodies
- [ ] Test with both patterns during transition
- [ ] Monitor deprecation warnings in logs
- [ ] Plan migration before v0.11.0 release

### For Server Operators

- [ ] Deploy v0.10.0 with dual support
- [ ] Monitor adoption of header pattern
- [ ] Notify clients of deprecation timeline
- [ ] Schedule v0.11.0 deployment

## Common Issues

### Issue: "ERR_NO_SESSION_CONTEXT"
**Cause**: Missing or invalid session token
**Solution**: Include `X-Session-Id` header (v0.10.0+)

### Issue: Deprecation warnings in logs
**Cause**: Client still using body pattern
**Solution**: Update client to use header pattern

### Issue: "ERR_RATE_LIMIT_EXCEEDED"
**Cause**: Too many requests
**Solution**: Implement exponential backoff

### Issue: "ERR_INSUFFICIENT_CAPABILITY"
**Cause**: Operation requires higher privileges
**Solution**: Use session with appropriate capability

## Next Steps

After establishing a session:
1. Use the session token in `X-Session-Id` header
2. Implement automatic renewal before expiration
3. Handle rate limiting with retry logic
4. Clean up with session revocation on disconnect

---

**Related Documentation**:
- [Migration Guide](migration-guide.md) - Complete v0.10.0 migration details
- [Phase 3: Authentication](phase3-authentication.md)
- [Session Flow Workflow](../workflows/PHASE4_SESSION_FLOW.md)
- [Security Overview](../SECURITY_OVERVIEW.md)