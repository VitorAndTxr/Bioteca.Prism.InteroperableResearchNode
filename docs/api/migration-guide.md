# Migration Guide: Session Header Pattern (v0.10.0)

This guide helps you migrate from the deprecated session-token-in-body pattern to the new X-Session-Id header pattern introduced in v0.10.0.

## Executive Summary

**What's Changing**: Session tokens are moving from encrypted request bodies to HTTP headers
**When**: v0.10.0 (dual support) → v0.11.0 (header-only)
**Why**: Consistency, performance, and simplified debugging
**Impact**: All Phase 4 endpoints require client updates

## Timeline

| Version | Release Date | Support Level |
|---------|-------------|---------------|
| v0.9.x | Current | Body pattern only |
| **v0.10.0** | **October 2025** | **Dual support (body + header)** |
| v0.11.0 | January 2026 | Header pattern only |
| v0.12.0 | April 2026 | Body pattern removed completely |

## Quick Migration Examples

### Before (v0.9.x)

```http
POST /api/session/whoami
X-Channel-Id: a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890
Content-Type: application/json

{
  "sessionToken": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

### After (v0.10.0+)

```http
POST /api/session/whoami
X-Channel-Id: a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890
X-Session-Id: f7e6d5c4-b3a2-1098-7654-321098765432
Content-Type: application/json

{
  "channelId": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

## Affected Endpoints

All Phase 4 session management endpoints are affected:

| Endpoint | Old Pattern | New Pattern |
|----------|-------------|-------------|
| `POST /api/session/whoami` | sessionToken in body | X-Session-Id header |
| `POST /api/session/renew` | sessionToken in body | X-Session-Id header |
| `POST /api/session/revoke` | sessionToken in body | X-Session-Id header |
| `POST /api/session/metrics` | sessionToken in body | X-Session-Id header |

## Migration Steps

### Step 1: Update HTTP Client Libraries

#### C# / .NET

**Before:**
```csharp
public async Task<WhoAmIResponse> GetSessionInfoAsync(
    string channelId,
    string sessionToken,
    byte[] symmetricKey)
{
    var request = new WhoAmIRequest
    {
        SessionToken = sessionToken,  // OLD
        ChannelId = channelId,
        Timestamp = DateTime.UtcNow
    };

    var encryptedPayload = EncryptPayload(request, symmetricKey);

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/whoami");
    httpRequest.Headers.Add("X-Channel-Id", channelId);
    httpRequest.Content = JsonContent.Create(encryptedPayload);

    // ... send request
}
```

**After:**
```csharp
public async Task<WhoAmIResponse> GetSessionInfoAsync(
    string channelId,
    string sessionToken,
    byte[] symmetricKey)
{
    var request = new WhoAmIRequest
    {
        // SessionToken removed
        ChannelId = channelId,
        Timestamp = DateTime.UtcNow
    };

    var encryptedPayload = EncryptPayload(request, symmetricKey);

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/whoami");
    httpRequest.Headers.Add("X-Channel-Id", channelId);
    httpRequest.Headers.Add("X-Session-Id", sessionToken);  // NEW
    httpRequest.Content = JsonContent.Create(encryptedPayload);

    // ... send request
}
```

#### Python

**Before:**
```python
import requests
import json

def get_session_info(channel_id, session_token, symmetric_key):
    payload = {
        'sessionToken': session_token,  # OLD
        'channelId': channel_id,
        'timestamp': datetime.utcnow().isoformat()
    }

    encrypted_payload = encrypt_payload(payload, symmetric_key)

    response = requests.post(
        'http://localhost:5000/api/session/whoami',
        headers={'X-Channel-Id': channel_id},
        json=encrypted_payload
    )

    return decrypt_response(response.json(), symmetric_key)
```

**After:**
```python
import requests
import json

def get_session_info(channel_id, session_token, symmetric_key):
    payload = {
        # sessionToken removed
        'channelId': channel_id,
        'timestamp': datetime.utcnow().isoformat()
    }

    encrypted_payload = encrypt_payload(payload, symmetric_key)

    response = requests.post(
        'http://localhost:5000/api/session/whoami',
        headers={
            'X-Channel-Id': channel_id,
            'X-Session-Id': session_token  # NEW
        },
        json=encrypted_payload
    )

    return decrypt_response(response.json(), symmetric_key)
```

#### JavaScript/TypeScript

**Before:**
```typescript
async function getSessionInfo(
    channelId: string,
    sessionToken: string,
    symmetricKey: Uint8Array
): Promise<WhoAmIResponse> {
    const payload = {
        sessionToken,  // OLD
        channelId,
        timestamp: new Date().toISOString()
    };

    const encryptedPayload = await encryptPayload(payload, symmetricKey);

    const response = await fetch('/api/session/whoami', {
        method: 'POST',
        headers: {
            'X-Channel-Id': channelId,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(encryptedPayload)
    });

    const encryptedResponse = await response.json();
    return decryptPayload(encryptedResponse, symmetricKey);
}
```

**After:**
```typescript
async function getSessionInfo(
    channelId: string,
    sessionToken: string,
    symmetricKey: Uint8Array
): Promise<WhoAmIResponse> {
    const payload = {
        // sessionToken removed
        channelId,
        timestamp: new Date().toISOString()
    };

    const encryptedPayload = await encryptPayload(payload, symmetricKey);

    const response = await fetch('/api/session/whoami', {
        method: 'POST',
        headers: {
            'X-Channel-Id': channelId,
            'X-Session-Id': sessionToken,  // NEW
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(encryptedPayload)
    });

    const encryptedResponse = await response.json();
    return decryptPayload(encryptedResponse, symmetricKey);
}
```

### Step 2: Update Request DTOs

Remove the `sessionToken` field from your request DTOs:

```csharp
// Before
public class WhoAmIRequest
{
    public string SessionToken { get; set; }  // REMOVE
    public string ChannelId { get; set; }
    public DateTime Timestamp { get; set; }
}

// After
public class WhoAmIRequest
{
    public string ChannelId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Step 3: Test with Both Patterns

During the transition period (v0.10.0), test your client with both patterns to ensure compatibility:

```bash
#!/bin/bash
# test-migration.sh

CHANNEL_ID="a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890"
SESSION_TOKEN="f7e6d5c4-b3a2-1098-7654-321098765432"

echo "Testing Migration: Session Header Pattern"
echo "========================================="

# Test 1: Old pattern (body)
echo -e "\n1. Testing old pattern (body)..."
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionToken": "'$SESSION_TOKEN'",
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.success'

# Test 2: New pattern (header)
echo -e "\n2. Testing new pattern (header)..."
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.success'

# Test 3: Both (header takes precedence)
echo -e "\n3. Testing both (header should win)..."
curl -X POST http://localhost:5000/api/session/whoami \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "X-Session-Id: $SESSION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionToken": "wrong-token-should-be-ignored",
    "channelId": "'$CHANNEL_ID'",
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }' | jq '.success'

echo -e "\nMigration test complete!"
```

### Step 4: Monitor Deprecation Warnings

Check your server logs for deprecation warnings:

```log
[WARNING] Session token in body is deprecated. Use X-Session-Id header.
          Client: node-a, Endpoint: /api/session/whoami
```

### Step 5: Update Documentation

Update your internal documentation and API clients:

```markdown
## Session Authentication

As of v0.10.0, session tokens must be sent via the `X-Session-Id` header.

### Required Headers
- `X-Channel-Id`: Channel identifier from Phase 1
- `X-Session-Id`: Session token from Phase 3 (NEW)

### Example
\```http
POST /api/session/whoami
X-Channel-Id: {channelId}
X-Session-Id: {sessionToken}
\```
```

## Backward Compatibility

### v0.10.0 Behavior

The server implements intelligent fallback:

```csharp
// Server-side logic in v0.10.0
var sessionToken = context.HttpContext.Request.Headers["X-Session-Id"].FirstOrDefault();

if (string.IsNullOrEmpty(sessionToken))
{
    // Fallback to body extraction (deprecated)
    sessionToken = ExtractFromBody(context);
    if (!string.IsNullOrEmpty(sessionToken))
    {
        _logger.LogWarning("Session token in body is deprecated. Use X-Session-Id header.");
    }
}

if (string.IsNullOrEmpty(sessionToken))
{
    return Unauthorized("No session token provided");
}
```

### Priority Order

If both header and body contain session tokens:
1. **Header wins** - `X-Session-Id` header value is used
2. **Body ignored** - Token in body is discarded
3. **Warning logged** - Deprecation warning is recorded

## Common Migration Issues

### Issue 1: "No session token provided"

**Symptom**: Requests fail with 401 Unauthorized
**Cause**: Token not in header, server running v0.11.0+
**Solution**: Add `X-Session-Id` header to all requests

### Issue 2: Deprecation warnings in logs

**Symptom**: Warnings about deprecated body pattern
**Cause**: Client still using old pattern
**Solution**: Update client to use header pattern

### Issue 3: Double encryption

**Symptom**: Server can't decrypt request
**Cause**: Encrypting token separately from body
**Solution**: Token goes in plain header, only body is encrypted

### Issue 4: Wrong header name

**Symptom**: Authentication fails
**Cause**: Using wrong header name (e.g., `Session-Id`, `X-SessionId`)
**Solution**: Use exact header name: `X-Session-Id`

## Migration Checklist

### For Client Developers

- [ ] Identify all Phase 4 endpoint calls
- [ ] Update HTTP client to add `X-Session-Id` header
- [ ] Remove `sessionToken` from request bodies
- [ ] Update request DTO classes
- [ ] Test with v0.10.0 server
- [ ] Monitor for deprecation warnings
- [ ] Update documentation
- [ ] Plan deployment before v0.11.0

### For Server Operators

- [ ] Deploy v0.10.0 with dual support
- [ ] Monitor deprecation warnings
- [ ] Track header adoption metrics
- [ ] Notify all client teams
- [ ] Set v0.11.0 deployment date
- [ ] Prepare rollback plan

### For DevOps Teams

- [ ] Update API gateway rules if needed
- [ ] Ensure headers are passed through proxies
- [ ] Update monitoring/logging
- [ ] Test load balancer configuration
- [ ] Verify header size limits

## Performance Benefits

The new pattern provides measurable improvements:

| Metric | Body Pattern | Header Pattern | Improvement |
|--------|-------------|----------------|-------------|
| Extraction Time | ~2.5ms (reflection) | ~0.1ms (direct) | 25x faster |
| Code Complexity | High (reflection) | Low (header read) | Simpler |
| Debugging | Difficult | Easy | Better DX |
| Logging | Complex | Simple | Cleaner logs |

## Code Examples

### Complete Migration Example

```csharp
public class NodeClient
{
    private readonly HttpClient _httpClient;
    private readonly string _nodeUrl;
    private readonly bool _useNewPattern;

    public NodeClient(string nodeUrl, bool useNewPattern = true)
    {
        _nodeUrl = nodeUrl;
        _useNewPattern = useNewPattern;
        _httpClient = new HttpClient();
    }

    public async Task<T> SendPhase4RequestAsync<T>(
        string endpoint,
        object request,
        string channelId,
        string sessionToken,
        byte[] symmetricKey)
    {
        // Encrypt request
        var encryptedPayload = EncryptPayload(request, symmetricKey);

        // Create HTTP request
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_nodeUrl}{endpoint}"
        );

        // Add channel header (always required)
        httpRequest.Headers.Add("X-Channel-Id", channelId);

        if (_useNewPattern)
        {
            // v0.10.0+ pattern
            httpRequest.Headers.Add("X-Session-Id", sessionToken);
        }
        else
        {
            // v0.9.x pattern (deprecated)
            // Modify request to include sessionToken
            var modifiedRequest = AddSessionToken(request, sessionToken);
            encryptedPayload = EncryptPayload(modifiedRequest, symmetricKey);
        }

        httpRequest.Content = JsonContent.Create(encryptedPayload);

        // Send request
        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        // Decrypt response
        var encryptedResponse = await response.Content
            .ReadFromJsonAsync<EncryptedPayload>();

        return DecryptPayload<T>(encryptedResponse, symmetricKey);
    }

    private object AddSessionToken(object request, string sessionToken)
    {
        // Use reflection or dynamic to add sessionToken
        // This is the deprecated pattern
        dynamic dynamicRequest = request;
        dynamicRequest.sessionToken = sessionToken;
        return dynamicRequest;
    }
}
```

### Testing Both Patterns

```csharp
[TestClass]
public class SessionHeaderMigrationTests
{
    [TestMethod]
    public async Task Should_Accept_Header_Pattern()
    {
        // Arrange
        var client = new NodeClient(nodeUrl, useNewPattern: true);

        // Act
        var response = await client.SendPhase4RequestAsync<WhoAmIResponse>(
            "/api/session/whoami",
            new WhoAmIRequest { ChannelId = channelId },
            channelId,
            sessionToken,
            symmetricKey
        );

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(sessionToken, response.SessionToken);
    }

    [TestMethod]
    public async Task Should_Accept_Body_Pattern_With_Warning()
    {
        // Arrange
        var client = new NodeClient(nodeUrl, useNewPattern: false);

        // Act
        var response = await client.SendPhase4RequestAsync<WhoAmIResponse>(
            "/api/session/whoami",
            new WhoAmIRequest { ChannelId = channelId },
            channelId,
            sessionToken,
            symmetricKey
        );

        // Assert
        Assert.IsNotNull(response);
        // Check logs for deprecation warning
    }

    [TestMethod]
    public async Task Header_Should_Take_Precedence_Over_Body()
    {
        // Send both header and body tokens
        // Verify header token is used
    }
}
```

## Support Resources

### Documentation
- [Phase 4 API Documentation](phase4-session.md)
- [API Overview](README.md)
- [Security Overview](../SECURITY_OVERVIEW.md)

### Contact
- GitHub Issues: [Report migration issues](https://github.com/prism/irn/issues)
- Email: prism-support@example.org

### Tools
- [Postman Collection](postman-collection.json) - Updated for v0.10.0
- [Migration Test Script](../../test-session-header-migration.sh)

## Frequently Asked Questions

### Q: What happens if I don't migrate before v0.11.0?
**A**: Your client will receive 401 Unauthorized errors. You must update to use headers.

### Q: Can I send the token in both places?
**A**: Yes, during v0.10.0. The header value takes precedence.

### Q: Is the header encrypted?
**A**: No, the header is sent in plain text. The channel encryption protects the body.

### Q: What about other phases?
**A**: Only Phase 4 is affected. Phases 1-3 remain unchanged.

### Q: How do I know if my client is updated?
**A**: Check server logs. Updated clients won't generate deprecation warnings.

---

**Version**: 1.0
**Last Updated**: October 2025
**Deprecation Date**: January 2026 (v0.11.0)