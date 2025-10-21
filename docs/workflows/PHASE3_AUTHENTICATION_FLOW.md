# Phase 3: Mutual Authentication Flow

**Phase**: 3 of 4
**Purpose**: Prove possession of the private key via challenge-response protocol
**Security**: RSA-2048 signature verification + One-time challenges + Replay protection

---

## Overview

Phase 3 implements mutual authentication using cryptographic challenge-response. The server generates a random challenge that the client must sign with their private key, proving possession without transmitting the key itself.

**Prerequisites**:
- Phase 1 encrypted channel established
- Phase 2 node identification complete (status: Authorized)

---

## Step-by-Step Flow

### 1. Request Challenge

**Client Action** (Node A):
```csharp
// Prepare challenge request
var request = new ChallengeRequest
{
    ChannelId = channelId,
    NodeId = "node-a",
    Timestamp = DateTime.UtcNow.ToString("O")
};

// Encrypt with channel symmetric key
var encrypted = _encryptionService.EncryptPayload(request, channelSymmetricKey);

// Send to server
var response = await httpClient.PostAsync(
    "https://node-b:5001/api/node/challenge",
    encrypted
);
```

**HTTP Request**:
```http
POST /api/node/challenge HTTP/1.1
Host: node-b:5001
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
  "nodeId": "node-a",
  "timestamp": "2025-10-21T10:30:00.0000000Z"
}
```

---

### 2. Server Generates Challenge

**Server Action** (Node B):
```csharp
// NodeConnectionController.Challenge()

// 1. [Automatic] Decrypt request via PrismEncryptedChannelConnectionAttribute
var request = HttpContext.Items["DecryptedRequest"] as ChallengeRequest;
var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

// 2. Validate channel exists
if (channelContext == null)
    return BadRequest("Channel not found");

// 3. Validate identified node ID exists (from Phase 2)
if (!channelContext.IdentifiedNodeId.HasValue)
    return BadRequest("Node not identified. Complete Phase 2 first.");

// 4. Validate node status is Authorized
var node = await _nodeRepository.GetByIdAsync(channelContext.IdentifiedNodeId.Value);
if (node == null || node.Status != AuthorizationStatus.Authorized)
    return Unauthorized("Node not authorized");

// 5. Generate cryptographically secure random challenge (32 bytes)
var challengeBytes = new byte[32];
using (var rng = RandomNumberGenerator.Create())
    rng.GetBytes(challengeBytes);

var challengeData = Convert.ToBase64String(challengeBytes);

// 6. Calculate expiration (5 minutes from now)
var expiresAt = DateTime.UtcNow.AddMinutes(5);

// 7. Store challenge with TTL
var challenge = new Challenge
{
    ChallengeData = challengeData,
    NodeId = request.NodeId,
    ChannelId = request.ChannelId,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = expiresAt,
    Used = false
};
await _challengeService.StoreAsync(challenge);

// 8. Return challenge to client
var response = new ChallengeResponse
{
    ChallengeData = challengeData,
    ExpiresAt = expiresAt,
    TtlSeconds = 300
};

var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
return Ok(encryptedResponse);
```

**HTTP Response** (encrypted):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}

# Decrypted payload:
{
  "challengeData": "q8F3k9L2m7N4p1R5s8T6v9W2x5Y8z1A3b6C9d2E5f8G1",
  "expiresAt": "2025-10-21T10:35:00.0000000Z",
  "ttlSeconds": 300
}
```

---

### 3. Client Signs Challenge

**Client Action** (Node A):
```csharp
// 1. Decrypt challenge response
var challengeResponse = _encryptionService.DecryptPayload<ChallengeResponse>(
    encryptedResponse,
    channelSymmetricKey
);

// 2. Construct signing data in EXACT format
// Format: {challengeData}{channelId}{nodeId}{timestamp:O}
var signingData = string.Concat(
    challengeResponse.ChallengeData,
    channelId,
    nodeId,
    timestamp.ToString("O")  // ISO 8601 round-trip format
);

// 3. Sign with private key
var dataToSign = Encoding.UTF8.GetBytes(signingData);
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(dataToSign);

using var rsa = certificate.GetRSAPrivateKey();
var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

// 4. Prepare authentication request
var authRequest = new AuthenticateRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = challengeResponse.ChallengeData,
    Signature = Convert.ToBase64String(signature),
    Timestamp = DateTime.UtcNow.ToString("O")
};
```

**Testing Endpoint** (sign challenge):
```http
POST /api/testing/sign-challenge HTTP/1.1
Content-Type: application/json

{
  "certificate": "MIIDXTCCAkWgAwIBAgIJAKZ...",
  "challengeData": "q8F3k9L2m7N4p1R5s8T6v9W2x5Y8z1A3b6C9d2E5f8G1",
  "channelId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "nodeId": "node-a",
  "timestamp": "2025-10-21T10:30:15.0000000Z"
}

# Response:
{
  "signature": "base64-encoded-rsa-signature",
  "signingData": "q8F3k9L2...f47ac10b...node-a2025-10-21T10:30:15.0000000Z",
  "algorithm": "SHA256withRSA"
}
```

---

### 4. Authenticate with Signed Challenge

**Client Action** (Node A):
```csharp
// Encrypt authentication request
var encrypted = _encryptionService.EncryptPayload(authRequest, channelSymmetricKey);

// Send to server
var authResponse = await httpClient.PostAsync(
    "https://node-b:5001/api/node/authenticate",
    encrypted
);
```

**HTTP Request**:
```http
POST /api/node/authenticate HTTP/1.1
Host: node-b:5001
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
  "nodeId": "node-a",
  "challengeData": "q8F3k9L2m7N4p1R5s8T6v9W2x5Y8z1A3b6C9d2E5f8G1",
  "signature": "base64-rsa-signature",
  "timestamp": "2025-10-21T10:30:15.0000000Z"
}
```

---

### 5. Server Verifies Signature

**Server Action** (Node B):
```csharp
// NodeConnectionController.Authenticate()

// 1. [Automatic] Decrypt request
var request = HttpContext.Items["DecryptedRequest"] as AuthenticateRequest;
var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

// 2. Retrieve stored challenge
var challenge = await _challengeService.GetAsync(request.ChallengeData, request.NodeId);
if (challenge == null)
    return Unauthorized("Challenge not found or expired");

// 3. Validate challenge data matches
if (challenge.ChallengeData != request.ChallengeData)
    return Unauthorized("Challenge data mismatch");

// 4. Validate challenge not expired
if (challenge.ExpiresAt < DateTime.UtcNow)
    return Unauthorized("Challenge expired");

// 5. Validate challenge not already used
if (challenge.Used)
    return Unauthorized("Challenge already used");

// 6. Validate channel ID matches
if (challenge.ChannelId != request.ChannelId)
    return Unauthorized("Channel ID mismatch");

// 7. Validate node is Authorized
var node = await _nodeRepository.GetByIdAsync(channelContext.IdentifiedNodeId.Value);
if (node == null || node.Status != AuthorizationStatus.Authorized)
    return Unauthorized("Node not authorized");

// 8. Reconstruct signing data (EXACT same format as client)
var signingData = string.Concat(
    request.ChallengeData,
    request.ChannelId,
    request.NodeId,
    request.Timestamp
);

// 9. Parse certificate and extract public key
var certBytes = Convert.FromBase64String(node.Certificate);
var cert = new X509Certificate2(certBytes);
var publicKey = cert.GetRSAPublicKey();

// 10. Verify RSA signature
var dataToVerify = Encoding.UTF8.GetBytes(signingData);
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(dataToVerify);

var signatureBytes = Convert.FromBase64String(request.Signature);
var isValid = publicKey.VerifyHash(hash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

if (!isValid)
    return Unauthorized("Invalid signature");

// 11. Mark challenge as used (one-time use)
challenge.Used = true;
await _challengeService.UpdateAsync(challenge);

// 12. Create session token (Phase 4)
var sessionToken = await _sessionService.CreateSessionAsync(new SessionContext
{
    SessionToken = Guid.NewGuid().ToString(),
    NodeId = request.NodeId,
    RegistrationId = node.Id,
    ChannelId = request.ChannelId,
    NodeAccessLevel = node.NodeAccessLevel,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddHours(1)
});

// 13. Update last authenticated timestamp
node.LastAuthenticatedAt = DateTime.UtcNow;
await _nodeRepository.UpdateAsync(node);

// 14. Return session token
var response = new AuthenticationResponse
{
    Authenticated = true,
    SessionToken = sessionToken.SessionToken,
    SessionExpiresAt = sessionToken.ExpiresAt,
    GrantedCapabilities = GetCapabilities(node.NodeAccessLevel),
    NextPhase = "phase4_session"
};

var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
return Ok(encrypted);
```

**HTTP Response** (encrypted):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}

# Decrypted payload:
{
  "authenticated": true,
  "sessionToken": "a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890",
  "sessionExpiresAt": "2025-10-21T11:30:15.0000000Z",
  "grantedCapabilities": ["query:read", "data:write"],
  "nextPhase": "phase4_session"
}
```

---

## Challenge Storage

### In-Memory Storage (Default)

```csharp
// Bioteca.Prism.Service/Services/Node/ChallengeService.cs
private readonly ConcurrentDictionary<string, Challenge> _challenges = new();

public async Task StoreAsync(Challenge challenge)
{
    var key = $"{challenge.NodeId}:{challenge.ChallengeData}";
    _challenges[key] = challenge;

    // Schedule automatic cleanup after expiration
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        _challenges.TryRemove(key, out _);
    });
}

public async Task<Challenge?> GetAsync(string challengeData, string nodeId)
{
    var key = $"{nodeId}:{challengeData}";
    if (_challenges.TryGetValue(key, out var challenge))
    {
        if (challenge.ExpiresAt > DateTime.UtcNow && !challenge.Used)
            return challenge;

        // Expired or used, remove
        _challenges.TryRemove(key, out _);
    }
    return null;
}
```

---

### Redis Storage (Production)

```csharp
// Key Pattern: challenge:{nodeId}:{challengeData}
public async Task StoreAsync(Challenge challenge)
{
    var db = _redis.GetDatabase();
    var key = $"challenge:{challenge.NodeId}:{challenge.ChallengeData}";

    var json = JsonSerializer.Serialize(challenge);
    await db.StringSetAsync(key, json, TimeSpan.FromMinutes(5));
}

public async Task<Challenge?> GetAsync(string challengeData, string nodeId)
{
    var db = _redis.GetDatabase();
    var key = $"challenge:{nodeId}:{challengeData}";

    var json = await db.StringGetAsync(key);
    if (!json.HasValue) return null;

    return JsonSerializer.Deserialize<Challenge>((string)json);
}
```

---

## Signing Data Format (Critical)

**IMPORTANT**: Client and server MUST use EXACT same format.

```
Format: {challengeData}{channelId}{nodeId}{timestamp:O}

Example:
challengeData: "q8F3k9L2m7N4p1R5s8T6v9W2x5Y8z1A3b6C9d2E5f8G1"
channelId: "f47ac10b-58cc-4372-a567-0e02b2c3d479"
nodeId: "node-a"
timestamp: "2025-10-21T10:30:15.0000000Z"

Concatenated:
"q8F3k9L2m7N4p1R5s8T6v9W2x5Y8z1A3b6C9d2E5f8G1f47ac10b-58cc-4372-a567-0e02b2c3d479node-a2025-10-21T10:30:15.0000000Z"

NO SEPARATORS, NO SPACES, NO JSON
```

**Timestamp Format**: ISO 8601 round-trip format (`DateTime.ToString("O")`)
- Example: `2025-10-21T10:30:15.0000000Z`
- Must include all precision (7 decimal places for fractions of a second)

---

## Security Properties

### Replay Attack Protection

**One-Time Challenges**:
- Each challenge valid for single authentication attempt
- Challenge marked as `Used = true` after successful authentication
- Subsequent attempts with same challenge rejected

**Expiration**:
- Challenges expire after 5 minutes
- Server validates `ExpiresAt < DateTime.UtcNow`
- Expired challenges automatically removed from storage

**Uniqueness**:
- 32 bytes = 2^256 possible challenges
- Cryptographically secure RNG ensures randomness
- Collision probability: negligible

---

### Proof of Private Key Possession

**Challenge-Response Mechanism**:
- Client proves possession of private key WITHOUT transmitting it
- Server verifies signature using public key from certificate
- Attacker cannot forge signature without private key

**RSA-2048 Security**:
- 2048-bit key size provides ~112-bit security level
- Resistant to current factorization algorithms
- SHA-256 hash prevents collision attacks

---

## Testing

### Automated Tests

**Phase 3 Tests** (5/5 passing):
```csharp
[Fact] public async Task RequestChallenge_ShouldReturnChallenge()
[Fact] public async Task Authenticate_ValidSignature_ShouldCreateSession()
[Fact] public async Task Authenticate_InvalidSignature_ShouldReject()
[Fact] public async Task Authenticate_ExpiredChallenge_ShouldReject()
[Fact] public async Task Authenticate_UsedChallenge_ShouldReject()
```

---

### Manual Testing with Swagger

**Step 1: Request Challenge**
```http
POST /api/node/challenge
Headers: X-Channel-Id: {channel-id}
Body: { "encryptedData": "...", "iv": "...", "authTag": "..." }
```

**Step 2: Sign Challenge** (use testing endpoint)
```http
POST /api/testing/sign-challenge
Body: {
  "certificate": "...",
  "challengeData": "...",
  "channelId": "...",
  "nodeId": "...",
  "timestamp": "..."
}
```

**Step 3: Authenticate**
```http
POST /api/node/authenticate
Headers: X-Channel-Id: {channel-id}
Body: { "encryptedData": "...", "iv": "...", "authTag": "..." }
```

---

### End-to-End Test Script

```bash
#!/bin/bash
# test-phase3.sh

echo "=== Phase 3: Mutual Authentication ==="

# Complete Phases 1+2 first
source test-phase1.sh
source test-phase2.sh

# Request challenge
CHALLENGE_RESPONSE=$(curl -s -X POST http://localhost:5000/api/node/challenge \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}")

echo "Challenge received"

# Sign challenge (using testing endpoint)
SIGNATURE=$(curl -s -X POST http://localhost:5000/api/testing/sign-challenge \
  -H "Content-Type: application/json" \
  -d "{\"certificate\":\"$CERT\",\"challengeData\":\"$CHALLENGE\",...}")

echo "Challenge signed"

# Authenticate
AUTH_RESPONSE=$(curl -s -X POST http://localhost:5000/api/node/authenticate \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "{\"encryptedData\":\"...\",\"iv\":\"...\",\"authTag\":\"...\"}")

echo "Authentication successful"
echo "Session Token: $SESSION_TOKEN"
echo "=== Phase 3 Complete ==="
```

---

## Common Issues

### Issue: "Invalid signature"

**Causes**:
1. Signing data format mismatch
2. Timestamp format incorrect (must use ISO 8601 round-trip)
3. Wrong private key used
4. Character encoding issues (UTF-8 required)

**Solution**:
```csharp
// Verify exact format
var signingData = string.Concat(
    challengeData,    // No modifications
    channelId,        // Full GUID with hyphens
    nodeId,           // Exact string from Phase 2
    timestamp         // Must use .ToString("O")
);

// Use UTF-8 encoding
var bytes = Encoding.UTF8.GetBytes(signingData);
```

---

### Issue: "Challenge expired"

**Cause**: More than 5 minutes elapsed since challenge issuance

**Solution**:
1. Request new challenge
2. Complete authentication within 5-minute window
3. Check clock synchronization (NTP)

---

### Issue: "Challenge already used"

**Cause**: Attempting to reuse same challenge for multiple authentication attempts

**Solution**: Request new challenge for each authentication

---

### Issue: "Challenge not found"

**Causes**:
1. Challenge expired and removed from storage
2. Node restart (in-memory storage lost)
3. Challenge data mismatch

**Solution**:
1. Request new challenge
2. Enable Redis storage for persistence
3. Verify challenge data transmitted correctly

---

## Next Phase

Once Phase 3 is complete (authentication successful, session token received), proceed to:

**Phase 4: Session Management** (`docs/workflows/PHASE4_SESSION_FLOW.md`)

All Phase 4 requests will include the session token in the encrypted payload.

---

## Documentation References

- **Security Details**: `docs/SECURITY_OVERVIEW.md`
- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Testing Guide**: `docs/testing/manual-testing-guide.md`
- **Phase 2 Flow**: `docs/workflows/PHASE2_IDENTIFICATION_FLOW.md`
- **Phase 4 Flow**: `docs/workflows/PHASE4_SESSION_FLOW.md`
