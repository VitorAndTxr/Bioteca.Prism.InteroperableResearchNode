# Phase 3: Mutual Authentication - Implementation Plan

**Status**: 📋 Planning
**Created**: 2025-10-03
**Target**: Next development phase

## Overview

Phase 3 implements challenge/response mutual authentication between nodes, proving possession of private keys without exposing them. This phase builds on the encrypted channel (Phase 1) and node identification (Phase 2).

## Prerequisites

- ✅ Phase 1 Complete: Encrypted channel with ECDH P-384 + AES-256-GCM
- ✅ Phase 2 Complete: Node identification with X.509 certificates + RSA-2048 signatures
- ✅ Encrypted payload handling via `PrismEncryptedChannelConnectionAttribute<T>`

## Goals

1. **Challenge/Response Protocol**: Implement bidirectional authentication
2. **Private Key Proof**: Verify key possession without exposing keys
3. **Replay Attack Prevention**: Ensure challenges cannot be reused
4. **Mutual Trust**: Both nodes verify each other's identity
5. **Session Key Derivation**: Generate session-specific keys for Phase 4

## Architecture

### New Components

```
Bioteca.Prism.Domain/
├── Requests/Node/
│   ├── AuthChallengeRequest.cs       # Challenge from initiator
│   ├── AuthResponseRequest.cs        # Response from receiver
│   └── AuthVerifyRequest.cs          # Verification from initiator
└── Responses/Node/
    ├── AuthChallengeResponse.cs      # Receiver's challenge
    ├── AuthResponseResponse.cs       # Verification result
    └── AuthCompleteResponse.cs       # Final authentication status

Bioteca.Prism.Service/Services/Node/
├── IAuthenticationService.cs         # Authentication interface
└── AuthenticationService.cs          # Challenge/response logic

Bioteca.Prism.InteroperableResearchNode/Controllers/
└── ChannelController.cs              # New Phase 3 endpoints
```

### Attributes

All Phase 3 endpoints will use:
- `[PrismEncryptedChannelConnection<TRequest>]` - Encrypted payload handling

## Protocol Flow

### Step 1: Initiator Sends Challenge (Node A → Node B)

**Endpoint**: `POST /api/channel/challenge`

**Request** (encrypted with channel key):
```json
{
  "challengeId": "uuid-v4",
  "nodeId": "node-a-uuid",
  "nonce": "base64-random-32-bytes",
  "timestamp": "2025-10-03T12:00:00Z",
  "signature": "base64-rsa-signature-of-nonce"
}
```

**Signature Data** (for initiator):
```
{nodeId}|{nonce}|{timestamp}
```

**Response** (encrypted):
```json
{
  "challengeId": "uuid-v4",
  "status": "accepted",
  "recipientChallenge": {
    "nodeId": "node-b-uuid",
    "nonce": "base64-random-32-bytes",
    "timestamp": "2025-10-03T12:00:01Z",
    "signature": "base64-rsa-signature-of-nonce"
  },
  "nextPhase": "phase3_respond"
}
```

### Step 2: Initiator Responds to Receiver's Challenge (Node A → Node B)

**Endpoint**: `POST /api/channel/authenticate`

**Request** (encrypted):
```json
{
  "challengeId": "uuid-v4",
  "nodeId": "node-a-uuid",
  "responseToChallenge": "base64-rsa-signature-of-received-nonce",
  "timestamp": "2025-10-03T12:00:02Z"
}
```

**Response** (encrypted):
```json
{
  "challengeId": "uuid-v4",
  "status": "authenticated",
  "sessionId": "uuid-v4-session",
  "sessionExpiresAt": "2025-10-03T13:00:02Z",
  "capabilities": [
    "biosignal-storage",
    "metadata-query",
    "federated-search"
  ],
  "nextPhase": "phase4_session"
}
```

## Implementation Details

### 1. Domain Models

**AuthChallengeRequest.cs**:
```csharp
public class AuthChallengeRequest
{
    public string ChallengeId { get; set; }
    public string NodeId { get; set; }
    public string Nonce { get; set; }  // Base64, 32 bytes minimum
    public DateTime Timestamp { get; set; }
    public string Signature { get; set; }  // RSA signature of {NodeId}|{Nonce}|{Timestamp}
}
```

**AuthChallengeResponse.cs**:
```csharp
public class AuthChallengeResponse
{
    public string ChallengeId { get; set; }
    public string Status { get; set; }  // "accepted" | "rejected"
    public RecipientChallenge RecipientChallenge { get; set; }
    public string NextPhase { get; set; }
}

public class RecipientChallenge
{
    public string NodeId { get; set; }
    public string Nonce { get; set; }
    public DateTime Timestamp { get; set; }
    public string Signature { get; set; }
}
```

**AuthResponseRequest.cs**:
```csharp
public class AuthResponseRequest
{
    public string ChallengeId { get; set; }
    public string NodeId { get; set; }
    public string ResponseToChallenge { get; set; }  // RSA signature of received nonce
    public DateTime Timestamp { get; set; }
}
```

**AuthCompleteResponse.cs**:
```csharp
public class AuthCompleteResponse
{
    public string ChallengeId { get; set; }
    public string Status { get; set; }  // "authenticated" | "failed"
    public string SessionId { get; set; }
    public DateTime SessionExpiresAt { get; set; }
    public List<string> Capabilities { get; set; }
    public string NextPhase { get; set; }
}
```

### 2. Authentication Service

**IAuthenticationService.cs**:
```csharp
public interface IAuthenticationService
{
    // Generate challenge for initiator
    Task<RecipientChallenge> GenerateChallengeAsync(string nodeId);

    // Verify challenge signature from remote node
    Task<bool> VerifyChallengeSignatureAsync(AuthChallengeRequest request, string publicKeyCertificate);

    // Verify response to our challenge
    Task<bool> VerifyResponseSignatureAsync(string challengeId, string nodeId, string responseSignature);

    // Create authenticated session
    Task<string> CreateAuthenticatedSessionAsync(string channelId, string nodeId);

    // Get challenge by ID
    Task<RecipientChallenge> GetChallengeAsync(string challengeId);
}
```

**AuthenticationService.cs**:
```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly INodeRegistryService _nodeRegistry;
    private readonly ConcurrentDictionary<string, RecipientChallenge> _challenges;
    private readonly ConcurrentDictionary<string, AuthenticatedSession> _sessions;

    public async Task<RecipientChallenge> GenerateChallengeAsync(string nodeId)
    {
        var nonce = GenerateSecureNonce(32); // 32 bytes
        var challenge = new RecipientChallenge
        {
            NodeId = nodeId,
            Nonce = Convert.ToBase64String(nonce),
            Timestamp = DateTime.Now
        };

        // Sign with node's private key
        var dataToSign = $"{challenge.NodeId}|{challenge.Nonce}|{challenge.Timestamp:O}";
        challenge.Signature = SignData(dataToSign, GetNodePrivateKey(nodeId));

        // Store challenge for later verification
        _challenges[Guid.NewGuid().ToString()] = challenge;

        return challenge;
    }

    public async Task<bool> VerifyChallengeSignatureAsync(
        AuthChallengeRequest request,
        string publicKeyCertificate)
    {
        var dataToVerify = $"{request.NodeId}|{request.Nonce}|{request.Timestamp:O}";
        return VerifySignature(dataToVerify, request.Signature, publicKeyCertificate);
    }

    // ... other implementations
}
```

### 3. Controller Endpoints

**ChannelController.cs** (additions):
```csharp
/// <summary>
/// Phase 3 - Step 1: Receive authentication challenge from initiator
/// </summary>
[HttpPost("challenge")]
[PrismEncryptedChannelConnection<AuthChallengeRequest>]
public async Task<IActionResult> Challenge()
{
    var request = HttpContext.Items["DecryptedRequest"] as AuthChallengeRequest;
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

    // Validate challenge
    var node = await _nodeRegistry.GetNodeByIdAsync(request.NodeId);
    if (node == null || node.Status != AuthorizationStatus.Authorized)
    {
        return BadRequest(new HandshakeError { /* ... */ });
    }

    // Verify signature
    var signatureValid = await _authService.VerifyChallengeSignatureAsync(
        request,
        node.Certificate
    );
    if (!signatureValid)
    {
        return BadRequest(new HandshakeError { /* ... */ });
    }

    // Generate our challenge
    var recipientChallenge = await _authService.GenerateChallengeAsync(
        GetCurrentNodeId()
    );

    var response = new AuthChallengeResponse
    {
        ChallengeId = request.ChallengeId,
        Status = "accepted",
        RecipientChallenge = recipientChallenge,
        NextPhase = "phase3_respond"
    };

    // Encrypt response
    var encryptedResponse = _encryptionService.EncryptPayload(
        response,
        channelContext.SymmetricKey
    );

    return Ok(encryptedResponse);
}

/// <summary>
/// Phase 3 - Step 2: Verify response to our challenge
/// </summary>
[HttpPost("authenticate")]
[PrismEncryptedChannelConnection<AuthResponseRequest>]
public async Task<IActionResult> Authenticate()
{
    var request = HttpContext.Items["DecryptedRequest"] as AuthResponseRequest;
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

    // Verify response to our challenge
    var verified = await _authService.VerifyResponseSignatureAsync(
        request.ChallengeId,
        request.NodeId,
        request.ResponseToChallenge
    );

    if (!verified)
    {
        return BadRequest(new HandshakeError { /* ... */ });
    }

    // Create authenticated session
    var sessionId = await _authService.CreateAuthenticatedSessionAsync(
        channelContext.ChannelId,
        request.NodeId
    );

    var node = await _nodeRegistry.GetNodeByIdAsync(request.NodeId);

    var response = new AuthCompleteResponse
    {
        ChallengeId = request.ChallengeId,
        Status = "authenticated",
        SessionId = sessionId,
        SessionExpiresAt = DateTime.Now.AddHours(1),
        Capabilities = node.Capabilities.ToList(),
        NextPhase = "phase4_session"
    };

    // Encrypt response
    var encryptedResponse = _encryptionService.EncryptPayload(
        response,
        channelContext.SymmetricKey
    );

    return Ok(encryptedResponse);
}
```

### 4. Client Implementation

**NodeChannelClient.cs** (additions):
```csharp
public async Task<AuthCompleteResponse> AuthenticateAsync(
    string remoteUrl,
    string channelId,
    string nodeId)
{
    // Step 1: Send challenge
    var challengeRequest = new AuthChallengeRequest
    {
        ChallengeId = Guid.NewGuid().ToString(),
        NodeId = nodeId,
        Nonce = Convert.ToBase64String(GenerateSecureNonce(32)),
        Timestamp = DateTime.Now,
        Signature = SignChallenge(nodeId, nonce, timestamp)
    };

    var encryptedChallenge = _encryptionService.EncryptPayload(
        challengeRequest,
        GetChannelKey(channelId)
    );

    var challengeResponse = await PostEncryptedAsync<AuthChallengeResponse>(
        $"{remoteUrl}/api/channel/challenge",
        encryptedChallenge,
        channelId
    );

    // Step 2: Respond to recipient's challenge
    var responseRequest = new AuthResponseRequest
    {
        ChallengeId = challengeRequest.ChallengeId,
        NodeId = nodeId,
        ResponseToChallenge = SignNonce(challengeResponse.RecipientChallenge.Nonce),
        Timestamp = DateTime.Now
    };

    var encryptedResponse = _encryptionService.EncryptPayload(
        responseRequest,
        GetChannelKey(channelId)
    );

    var authComplete = await PostEncryptedAsync<AuthCompleteResponse>(
        $"{remoteUrl}/api/channel/authenticate",
        encryptedResponse,
        channelId
    );

    return authComplete;
}
```

## Security Considerations

### Nonce Requirements
- **Minimum size**: 32 bytes (256 bits)
- **Generation**: Cryptographically secure random number generator
- **Uniqueness**: Must be unique per challenge
- **Storage**: Challenges stored temporarily (5 minutes TTL)

### Signature Verification
- **Algorithm**: RSA-2048 with SHA-256
- **Data format**: `{NodeId}|{Nonce}|{Timestamp:O}`
- **Certificate validation**: Verify certificate not expired
- **Timestamp validation**: ±5 minutes tolerance

### Replay Attack Prevention
- **Challenge TTL**: 5 minutes maximum
- **One-time use**: Challenges invalidated after verification
- **Timestamp validation**: Reject stale challenges
- **ChallengeId tracking**: Prevent duplicate challenge IDs

### Session Management
- **Session TTL**: 1 hour default (configurable)
- **Session storage**: In-memory (will move to database)
- **Session binding**: Tied to specific channel + node pair
- **Automatic cleanup**: Remove expired sessions

## Testing Strategy

### Unit Tests
- `AuthenticationServiceTests.cs`
  - Challenge generation
  - Signature verification
  - Session creation
  - Expiration handling

### Integration Tests
- `Phase3AuthenticationTests.cs`
  - Full challenge/response flow
  - Signature validation
  - Replay attack prevention
  - Invalid signature handling
  - Expired challenge handling
  - Session creation verification

### Security Tests
- `Phase3SecurityTests.cs`
  - Replay attack attempts
  - Signature forgery attempts
  - Timestamp manipulation
  - Expired certificate handling
  - Unauthorized node attempts

## Implementation Checklist

### Domain Layer
- [ ] Create `AuthChallengeRequest.cs`
- [ ] Create `AuthChallengeResponse.cs`
- [ ] Create `AuthResponseRequest.cs`
- [ ] Create `AuthCompleteResponse.cs`
- [ ] Create `RecipientChallenge.cs`
- [ ] Create `AuthenticatedSession.cs`

### Service Layer
- [ ] Create `IAuthenticationService.cs`
- [ ] Implement `AuthenticationService.cs`
  - [ ] `GenerateChallengeAsync()`
  - [ ] `VerifyChallengeSignatureAsync()`
  - [ ] `VerifyResponseSignatureAsync()`
  - [ ] `CreateAuthenticatedSessionAsync()`
  - [ ] `GetChallengeAsync()`
  - [ ] `CleanupExpiredChallengesAsync()`
  - [ ] `CleanupExpiredSessionsAsync()`

### API Layer
- [ ] Add Phase 3 endpoints to `ChannelController.cs`
  - [ ] `POST /api/channel/challenge`
  - [ ] `POST /api/channel/authenticate`
- [ ] Add Phase 3 methods to `NodeChannelClient.cs`
  - [ ] `AuthenticateAsync()`
  - [ ] Helper methods for signature generation

### Testing
- [ ] Create `AuthenticationServiceTests.cs`
- [ ] Create `Phase3AuthenticationTests.cs`
- [ ] Create `Phase3SecurityTests.cs`
- [ ] Update integration test infrastructure
- [ ] Add test scenarios to manual testing guide

### Documentation
- [ ] Update `CLAUDE.md` with Phase 3 details
- [ ] Update `handshake-protocol.md` with implementation notes
- [ ] Create `docs/testing/phase3-manual-testing.md`
- [ ] Update `PROJECT_STATUS.md`

### Configuration
- [ ] Add challenge TTL to `appsettings.json`
- [ ] Add session TTL to `appsettings.json`
- [ ] Configure cleanup intervals

## Migration from Phase 2

### Existing Nodes
- Phase 3 is **optional** initially
- Nodes can skip Phase 3 if both are already authorized
- Full authentication required for sensitive operations

### Backward Compatibility
- Phase 2 endpoints remain functional
- Phase 3 adds additional security layer
- Old clients work with Phase 2 only

## Success Criteria

- [ ] All unit tests passing (100%)
- [ ] All integration tests passing (100%)
- [ ] Security tests validate replay protection
- [ ] Manual testing guide complete
- [ ] Documentation updated
- [ ] Code review completed
- [ ] Performance benchmarks acceptable (<100ms per challenge)

## Next Steps After Phase 3

### Phase 4: Session Establishment
- Token-based access control
- Capability negotiation
- Rate limiting
- Metrics collection

### Infrastructure Improvements
- Move to database storage
- Add distributed caching (Redis)
- Implement session replication
- Add monitoring/alerting

## References

- [NIST SP 800-63B: Digital Identity Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)
- [Challenge-Response Authentication](https://en.wikipedia.org/wiki/Challenge%E2%80%93response_authentication)
- [RFC 2617: HTTP Authentication](https://tools.ietf.org/html/rfc2617)
