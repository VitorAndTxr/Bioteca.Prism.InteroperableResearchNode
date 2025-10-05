# Phase 3 Testing Endpoints - Mutual Authentication

**Version**: 0.5.0
**Date**: 2025-10-03

This document demonstrates how to use the TestingController endpoints to manually execute the complete Phase 3 flow.

## 📋 Prerequisites

1. Node registered and **authorized** (status=Authorized)
2. Channel established (Phase 1)
3. Certificate with private key (for signing)
4. **Redis CLI** (optional - for persistence inspection) - included in Docker container
5. **Docker Compose** running with Redis containers (optional)

## 🔄 Complete Flow

### Step 1: Establish Channel (Phase 1)

```bash
curl -X POST http://localhost:5000/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

**Response:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "symmetricKey": "base64-key...",
  "selectedCipher": "AES-256-GCM"
}
```

**Save:** `channelId` for subsequent steps

---

### Step 2: Generate Certificate (if needed)

```bash
curl -X POST http://localhost:5000/api/testing/generate-certificate \
  -H "Content-Type: application/json" \
  -d '{
    "subjectName": "test-node-001",
    "validityYears": 2,
    "password": "test123"
  }'
```

**Response:**
```json
{
  "subjectName": "test-node-001",
  "certificate": "MIIC5TCCA...",
  "certificateWithPrivateKey": "MIIJCg...",
  "password": "test123",
  "thumbprint": "79B7FC808E5BAFC...",
  "usage": {
    "certificate": "Use this for registration (public key)",
    "certificateWithPrivateKey": "Use this to sign data (includes private key)",
    "password": "Password to load the PFX certificate"
  }
}
```

**Save:**
- `certificate` (for registration)
- `certificateWithPrivateKey` (for signing)
- `password`

---

### Step 3: Register Node (Phase 2)

⚠️ **IMPORTANT**: Payload must be encrypted. Use the `/api/testing/encrypt-payload` endpoint.

**3.1. Create registration payload:**
```json
{
  "nodeId": "test-node-001",
  "nodeName": "Test Node",
  "certificate": "MIIC5TCCA...",
  "contactInfo": "admin@test.com",
  "institutionDetails": "Test Institution",
  "nodeUrl": "http://test:8080",
  "requestedCapabilities": ["search", "retrieve"]
}
```

**3.2. Encrypt payload:**
```bash
curl -X POST http://localhost:5000/api/testing/encrypt-payload \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "payload": {
      "nodeId": "test-node-001",
      "nodeName": "Test Node",
      "certificate": "MIIC5TCCA...",
      "contactInfo": "admin@test.com",
      "institutionDetails": "Test Institution",
      "nodeUrl": "http://test:8080",
      "requestedCapabilities": ["search", "retrieve"]
    }
  }'
```

**3.3. Register with encrypted payload:**
```bash
curl -X POST http://localhost:5001/api/node/register \
  -H "Content-Type: application/json" \
  -H "X-Channel-Id: abc-123-def-456" \
  -d '{
    "encryptedData": "...",
    "iv": "...",
    "authTag": "..."
  }'
```

---

### Step 4: Approve Node (Admin)

```bash
curl -X PUT http://localhost:5001/api/node/test-node-001/status \
  -H "Content-Type: application/json" \
  -d '{"status": 1}'
```

**Response:**
```json
{
  "message": "Node status updated successfully",
  "nodeId": "test-node-001",
  "status": 1
}
```

---

### Step 5: ✨ Request Challenge (Phase 3)

**New test endpoint:**

```bash
curl -X POST http://localhost:5000/api/testing/request-challenge \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "nodeId": "test-node-001"
  }'
```

**Response:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "nodeId": "test-node-001",
  "challengeResponse": {
    "challengeData": "YXNkZmFzZGZhc2RmYXNkZg==",
    "challengeTimestamp": "2025-10-03T00:00:01Z",
    "challengeTtlSeconds": 300,
    "expiresAt": "2025-10-03T00:05:01Z"
  },
  "nextStep": {
    "action": "Sign the challengeData with your node's private key",
    "format": "{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}",
    "endpoint": "POST /api/testing/authenticate"
  }
}
```

**Save:** `challengeData` for next step

---

### Step 6: Sign Challenge

**6.1. Build data to sign:**

Format: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`

Example:
```
YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z
```

**6.2. Sign with certificate:**

```bash
curl -X POST http://localhost:5000/api/testing/sign-data \
  -H "Content-Type: application/json" \
  -d '{
    "data": "YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z",
    "certificateWithPrivateKey": "MIIJCg...",
    "password": "test123"
  }'
```

**Response:**
```json
{
  "data": "YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z",
  "signature": "as8xW2gPKcRnPDma...",
  "algorithm": "RSA-SHA256"
}
```

**Save:** `signature`

---

### Step 7: ✨ Authenticate with Signed Challenge

**New test endpoint:**

```bash
curl -X POST http://localhost:5000/api/testing/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "nodeId": "test-node-001",
    "challengeData": "YXNkZmFzZGZhc2RmYXNkZg==",
    "signature": "as8xW2gPKcRnPDma...",
    "timestamp": "2025-10-03T00:00:02.1234567Z"
  }'
```

**Success Response:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "nodeId": "test-node-001",
  "authenticationResponse": {
    "authenticated": true,
    "sessionToken": "a1b2c3d4e5f67890abcdef1234567890",
    "sessionExpiresAt": "2025-10-03T01:00:02Z",
    "grantedCapabilities": ["search", "retrieve"],
    "message": "Authentication successful",
    "nextPhase": "phase4_session",
    "timestamp": "2025-10-03T00:00:02Z"
  },
  "usage": {
    "sessionToken": "Use this token in subsequent authenticated requests",
    "ttl": "Session expires in 1 hour",
    "capabilities": ["search", "retrieve"]
  }
}
```

**🎉 Success!** Session token received!

---

### Step 8: 🗄️ Verify Redis Persistence (Optional)

**If Redis is enabled** (FeatureFlags:UseRedisForSessions=true, UseRedisForChannels=true):

#### **8.1. Verify Channel in Redis:**

```bash
# List channels
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "channel:*"

# Inspect metadata
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "channel:abc-123-def-456"

# Check TTL (should be ~1800 seconds)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "channel:abc-123-def-456"
```

#### **8.2. Verify Session in Redis:**

```bash
# List sessions
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b KEYS "session:*"

# Inspect session data
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b GET "session:{sessionToken}"

# Check TTL (should be ~3600 seconds = 1 hour)
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b TTL "session:{sessionToken}"

# Check rate limiting (sorted set)
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b ZRANGE "session:ratelimit:{sessionToken}" 0 -1 WITHSCORES
```

**Expected result:**
- Channel stored with 30-minute TTL
- Session stored with 1-hour TTL
- Rate limit sorted set created (empty initially)

#### **8.3. Persistence Test:**

```bash
# 1. Restart Node B (Redis keeps running)
docker restart irn-node-b

# 2. Wait for Node B to come online
docker logs -f irn-node-b | grep "Now listening"

# 3. Verify session still exists
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b EXISTS "session:{sessionToken}"

# Result: 1 (exists) - Session survived restart!
```

**💡 Redis Observations:**
- Each node has its own isolated Redis instance
- Sessions and channels have automatic TTL managed by Redis
- Rate limiting uses Sorted Sets with sliding window (60 req/min)
- Data persists even if node restarts (as long as Redis keeps running)
- For more details, see [Redis Testing Guide](./redis-testing-guide.md)

---

## ❌ Error Handling

### Error: Node Not Authorized

```json
{
  "success": false,
  "error": "Failed to request challenge",
  "message": "Challenge request failed: Node status is Pending. Only authorized nodes can authenticate.",
  "hint": "Make sure the node is registered and authorized (status=Authorized)"
}
```

**Solution:** Approve node via `PUT /api/node/{nodeId}/status` with `status: 1`

---

### Error: Challenge Expired

```json
{
  "success": false,
  "error": "Failed to authenticate",
  "message": "Authentication failed: Challenge response verification failed",
  "possibleCauses": [
    "Challenge has expired (TTL: 5 minutes)",
    "Challenge data does not match the one generated",
    "Invalid signature (wrong private key or wrong data format)",
    "Challenge was already used (one-time use only)"
  ]
}
```

**Solution:** Request new challenge via `POST /api/testing/request-challenge`

---

### Error: Invalid Signature

**Common causes:**

1. **Incorrect timestamp format:** Must be `{DateTime:O}` (ISO 8601 with offset)
   - ✅ Correct: `2025-10-03T00:00:02.1234567Z`
   - ❌ Wrong: `2025-10-03 00:00:02`

2. **Incorrect field order:**
   - ✅ Correct: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
   - ❌ Wrong: `{NodeId}{ChallengeData}{ChannelId}{Timestamp}`

3. **Wrong certificate:** Make sure to use `certificateWithPrivateKey` (PFX with private key)

4. **Different timestamp:** The timestamp used in signing must be **exactly** the same as sent in request

---

## 🔧 Helper Endpoints

### Verify Channel Information

```bash
curl http://localhost:5000/api/testing/channel-info/abc-123-def-456
```

### Decrypt Payload (Debug)

```bash
curl -X POST http://localhost:5000/api/testing/decrypt-payload \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "encryptedPayload": {
      "encryptedData": "...",
      "iv": "...",
      "authTag": "..."
    }
  }'
```

---

## 📝 Complete PowerShell Script

```powershell
# Phase 1: Establish Channel
$channel = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $channel.channelId
Write-Host "Channel ID: $channelId"

# Phase 2: Generate Certificate
$cert = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-certificate" `
  -Method Post -ContentType "application/json" `
  -Body '{"subjectName": "test-node-001", "validityYears": 2, "password": "test123"}'

# Phase 2: Register Node (simplified - requires encryption)
# ... (see complete documentation)

# Phase 2: Approve Node
Invoke-RestMethod -Uri "http://localhost:5001/api/node/test-node-001/status" `
  -Method Put -ContentType "application/json" `
  -Body '{"status": 1}'

# Phase 3: Request Challenge
$challenge = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/request-challenge" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    channelId = $channelId
    nodeId = "test-node-001"
  } | ConvertTo-Json)

$challengeData = $challenge.challengeResponse.challengeData
Write-Host "Challenge Data: $challengeData"

# Phase 3: Sign Challenge
$timestamp = Get-Date -Format "o"
$dataToSign = "$challengeData$channelId" + "test-node-001$timestamp"

$signResult = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/sign-data" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    data = $dataToSign
    certificateWithPrivateKey = $cert.certificateWithPrivateKey
    password = "test123"
  } | ConvertTo-Json)

$signature = $signResult.signature

# Phase 3: Authenticate
$authResult = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/authenticate" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    channelId = $channelId
    nodeId = "test-node-001"
    challengeData = $challengeData
    signature = $signature
    timestamp = $timestamp
  } | ConvertTo-Json)

Write-Host "✅ Authenticated!"
Write-Host "Session Token: $($authResult.authenticationResponse.sessionToken)"
Write-Host "Expires At: $($authResult.authenticationResponse.sessionExpiresAt)"
Write-Host "Capabilities: $($authResult.authenticationResponse.grantedCapabilities -join ', ')"
```

---

## 🎯 Phase 3 Endpoints Summary

| Endpoint | Method | Description |
|----------|--------|-----------|
| `/api/testing/request-challenge` | POST | Request challenge for authentication |
| `/api/testing/authenticate` | POST | Authenticate with signed challenge |
| `/api/testing/sign-data` | POST | Sign data with certificate |
| `/api/testing/encrypt-payload` | POST | Encrypt payload for channel |
| `/api/testing/decrypt-payload` | POST | Decrypt payload (debug) |

---

## ✅ Testing Checklist

- [ ] Channel established (Phase 1)
- [ ] Node registered (Phase 2)
- [ ] Node authorized (status=Authorized)
- [ ] Challenge requested via `/api/testing/request-challenge`
- [ ] Challenge received with `challengeData` and 5-minute TTL
- [ ] Data built correctly: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
- [ ] Signature generated with correct certificate
- [ ] Authentication performed via `/api/testing/authenticate`
- [ ] Session token received with 1-hour TTL
- [ ] Capabilities included in response
- [ ] **(Optional)** Channel verified in Redis with correct TTL
- [ ] **(Optional)** Session verified in Redis with correct TTL
- [ ] **(Optional)** Persistence test executed (node restart)

---

## 📚 Related Documentation

- [Manual Testing Guide](./manual-testing-guide.md) - Complete manual testing guide
- [Handshake Protocol](../architecture/handshake-protocol.md) - Protocol specification
- [Phase 3 Implementation Plan](../development/phase3-authentication-plan.md) - Implementation plan
- [Redis Testing Guide](./redis-testing-guide.md) - **🆕 Complete Redis persistence testing guide**
- [Docker Compose Quick Start](./docker-compose-quick-start.md) - Docker Compose quick start guide
