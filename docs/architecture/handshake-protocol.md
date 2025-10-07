# IRN Node Handshake Protocol

**Status**: ✅ Phase 4 Complete (Handshake Protocol Finalized)
**Last Updated**: 2025-10-07
**Owner**: Development Team

## Overview

The handshake protocol establishes trust and mutual authentication between two IRN instances before allowing research data exchange.

## Objectives

1. **Create Secure Communication Channel**: The requesting node must request channel creation by offering its public key
2. **Identification and Authorization**: Verify if the requesting node is known and authorized in the network
3. **Mutual Authentication**: Both nodes must verify each other's identity
4. **Establish Trust**: Validate certificates and credentials
5. **Capability Negotiation**: Identify features supported by each node
6. **Create Secure Session**: Establish encrypted channel for communication

## Identifier Architecture

The system uses a **dual-identifier architecture** to separate protocol-level and database-level concerns:

### NodeId (String)
- **Purpose**: Protocol-level identifier used in all request/response DTOs
- **Format**: Free-form string (typically institution ID, UUID, or human-readable identifier)
- **Usage**: Included in all Phase 2+ communication payloads
- **Example**: `"IRN-Hospital-XYZ"`, `"uuid-of-node-a"`
- **Stored in**: Request/Response DTOs (`NodeIdentifyRequest.NodeId`, `NodeStatusResponse.NodeId`)

### RegistrationId (Guid)
- **Purpose**: Internal database primary key for node records
- **Format**: GUID/UUID (e.g., `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`)
- **Usage**: Administrative operations (updating node status, internal lookups)
- **Returned in**: `NodeStatusResponse.RegistrationId` (only present if node is known)
- **Stored in**: `ResearchNode.Id`, `ChannelContext.IdentifiedNodeId`

### Certificate Fingerprint (Natural Key)
- **Purpose**: The true unique identifier for node authentication
- **Format**: SHA-256 hash of the X.509 certificate (hex string)
- **Usage**: Node lookups during registration and identification
- **Key Property**: Nodes are uniquely identified by their certificate, not by NodeId string
- **Stored in**: `ResearchNode.CertificateFingerprint`, `ChannelContext.CertificateFingerprint`

### When to Use Each Identifier

| Operation | Identifier Used | Reason |
|-----------|----------------|--------|
| Phase 2 Identification Request | NodeId (string) | Protocol-level identifier in DTOs |
| Phase 2 Identification Response | Both NodeId + RegistrationId | Return both for client use |
| Certificate-based node lookup | Certificate Fingerprint | Natural key for authentication |
| Administrative status updates | RegistrationId (Guid) | Database primary key |
| Session management | RegistrationId (Guid) | Links session to database record |
| Channel context storage | RegistrationId (Guid) | After Phase 2 identification |

### Identifier Flow Across Phases

```
Phase 1 (Channel): No identifiers yet (ephemeral keys only)
                ↓
Phase 2 (Identification):
  Client sends: NodeId (string) + Certificate
  Server calculates: Certificate Fingerprint (SHA-256)
  Server looks up: ResearchNode by Certificate Fingerprint
  Server stores in ChannelContext: IdentifiedNodeId (Guid) + CertificateFingerprint
  Server responds: NodeId (string) + RegistrationId (Guid)
                ↓
Phase 3 (Authentication):
  Uses: ChannelContext.IdentifiedNodeId (Guid) for session creation
  Creates: Session linked to Guid
                ↓
Phase 4 (Session Management):
  All operations: Use Session token (linked to Guid internally)
```

## Handshake Flow

### Phase 1: Opening the Encrypted Communication Channel

```
Node A (Initiator)                    Node B (Receiver)
     |                                    |
     |------ CHANNEL_OPEN --------------->|
     |       (channel public key)         |
     |                                    |
     |<----- CHANNEL_READY ---------------|
     |       (channel public key)         |
     |                                    |
```

![Sequence diagram of channel opening process](../img/protocoloAuth/fase1.png)

**Objective**: Establish an asymmetrically encrypted channel between nodes with disposable keys **before** any sensitive information exchange.

**CHANNEL_OPEN Payload:**
```json
{
  "protocolVersion": "1.0",
  "ephemeralPublicKey": "base64-encoded-ephemeral-public-key",
  "keyExchangeAlgorithm": "ECDH-P384",
  "supportedCiphers": ["AES-256-GCM", "ChaCha20-Poly1305"],
  "timestamp": "2025-10-01T12:00:00Z",
  "nonce": "random-nonce-123"
}
```

**CHANNEL_READY Payload:**
```json
{
  "protocolVersion": "1.0",
  "ephemeralPublicKey": "base64-encoded-ephemeral-public-key",
  "keyExchangeAlgorithm": "ECDH-P384",
  "selectedCipher": "AES-256-GCM",
  "timestamp": "2025-10-01T12:00:00Z",
  "nonce": "random-nonce-456"
}
```

**Technical Details**:
- Each node generates an **ephemeral (disposable) key pair** specifically for this session
- Ephemeral public keys are exchanged using **ECDH** (Elliptic Curve Diffie-Hellman)
- A **shared secret** is derived from the ephemeral keys
- The shared secret is used to derive symmetric keys (via HKDF)
- **Ephemeral keys are discarded** at the end of the session
- This provides **Perfect Forward Secrecy (PFS)**: even if permanent private keys are compromised in the future, past sessions remain secure

**✅ IMPLEMENTED - CRITICAL SECURITY REQUIREMENT**: From this point forward, **ALL subsequent messages** (Phases 2, 3, and 4) **ARE encrypted** using the symmetric keys derived from the channel established in Phase 1.

**Implementation via `PrismEncryptedChannelConnectionAttribute<T>`** (`IAsyncResourceFilter`):
- The `ChannelId` returned in the `X-Channel-Id` header of the `CHANNEL_READY` response is included in **all** subsequent requests
- Each request includes the header: `X-Channel-Id: {channelId}`
- The payload of all Phase 2-4 messages is encrypted with AES-256-GCM using the derived symmetric key
- The attribute validates that the `ChannelId` exists and hasn't expired before processing any request
- The attribute automatically decrypts the payload using the symmetric key associated with the channel
- For `NodeIdentifyRequest`, the attribute also verifies the node's RSA signature
- The decrypted request is stored in `HttpContext.Items["DecryptedRequest"]`
- Responses are encrypted using `ChannelEncryptionService.EncryptPayload()`

**Encrypted Payload Format**:
```json
{
  "encryptedData": "base64-encoded-encrypted-payload",
  "iv": "base64-encoded-initialization-vector",
  "authTag": "base64-encoded-authentication-tag"
}
```

### Phase 2: Identification and Authorization

```
Node A (Initiator)                    Node B (Receiver)
     |                                    |
     |------ NODE_IDENTIFY -------------->|
     |       (nodeId, credentials)        |
     |                                    |
     |<----- NODE_STATUS -----------------|
     |       (known/unknown + info)       |
     |                                    |
```

**Objective**: Verify if the requesting node is known and authorized in the network.

**⚠️ REQUIREMENT**: This request **MUST** be sent within the encrypted channel established in Phase 1. The `X-Channel-Id` header is mandatory.

**Required Headers**:
```
X-Channel-Id: {channelId-obtained-in-phase-1}
Content-Type: application/json
```

**NODE_IDENTIFY Payload (encrypted with channel key):**
```json
{
  "channelId": "channel-uuid",
  "nodeId": "IRN-Hospital-XYZ",
  "nodeName": "Hospital XYZ Research Node",
  "certificate": "base64-encoded-X509-certificate",
  "timestamp": "2025-10-01T12:00:01Z",
  "signature": "base64-rsa-signature"
}
```

**NodeId Field**: This is the protocol-level string identifier. It can be any format (institution name, UUID, etc.) and is used for display purposes.

**Certificate-Based Lookup**: The server extracts and calculates the SHA-256 fingerprint of the provided certificate. This fingerprint is used to look up the node in the database:

```csharp
// Server-side processing
var certFingerprint = CertificateHelper.CalculateFingerprint(request.Certificate);
var node = await _nodeRegistry.GetNodeByCertificateAsync(certFingerprint);
```

**ChannelContext Storage**: After successful identification, the server stores the identified node's Guid and certificate fingerprint in the channel context:

```csharp
channelContext.IdentifiedNodeId = node.Id;  // Guid
channelContext.CertificateFingerprint = certFingerprint;
```

**NODE_STATUS Response - Known Node (encrypted):**
```json
{
  "isKnown": true,
  "status": "Authorized",
  "nodeId": "IRN-Hospital-XYZ",
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nodeName": "Hospital XYZ Research Node",
  "timestamp": "2025-10-01T12:00:02Z",
  "nextPhase": "phase3_authenticate"
}
```

**RegistrationId Field**: This Guid is the internal database identifier for the node. Clients should store this for administrative operations (e.g., checking status, updating configuration).

**NODE_STATUS Response - Unknown Node (encrypted):**
```json
{
  "isKnown": false,
  "status": "Unknown",
  "nodeId": "IRN-Hospital-XYZ",
  "registrationId": null,
  "message": "Node not registered in the network",
  "registrationUrl": "https://node-b.example.com/api/node/register",
  "timestamp": "2025-10-01T12:00:02Z"
}
```

**Flow for Unknown Node**:
1. Node B returns `NODE_STATUS` with `isKnown: false`
2. Node A must initiate registration process through the `registrationUrl`
3. After registration, Node A awaits approval/authorization by Node B administrator
4. Node A can attempt new handshake after approval

**Re-registration Behavior**: If a node with the same certificate fingerprint already exists, the registration process updates the existing record instead of creating a duplicate. The NodeId string can change, but the certificate fingerprint is the immutable natural key.

### Phase 3: Mutual Authentication ✅ IMPLEMENTED

**Prerequisite**: This phase only occurs if the Node returned `status: "Authorized"` in Phase 2.

**✅ IMPLEMENTED**: All messages in this phase **ARE encrypted** using the channel established in Phase 1. The `X-Channel-Id` header is mandatory in all requests.

```
Node A (Initiator)                    Node B (Receiver)
     |                                    |
     |------ CHALLENGE_REQUEST ---------->|
     |       (NodeId, Timestamp)          |
     |                                    |
     |<----- CHALLENGE_RESPONSE ----------|
     |       (32-byte random challenge)   |
     |                                    |
     |------ AUTHENTICATE --------------->|
     |       (signed challenge)           |
     |                                    |
     |<----- AUTHENTICATION_RESPONSE -----|
     |       (session token, capabilities)|
     |                                    |
```

**CHALLENGE_REQUEST (encrypted via AES-256-GCM):**
```json
{
  "channelId": "channel-uuid",
  "nodeId": "IRN-Hospital-XYZ",
  "timestamp": "2025-10-03T00:00:00Z"
}
```

**CHALLENGE_RESPONSE (encrypted via AES-256-GCM):**
```json
{
  "challengeData": "base64-encoded-32-byte-random-value",
  "challengeTimestamp": "2025-10-03T00:00:01Z",
  "challengeTtlSeconds": 300,
  "expiresAt": "2025-10-03T00:05:01Z"
}
```

**AUTHENTICATE (encrypted via AES-256-GCM):**
```json
{
  "channelId": "channel-uuid",
  "nodeId": "IRN-Hospital-XYZ",
  "challengeData": "base64-encoded-32-byte-random-value",
  "signature": "base64-rsa-signature-of-challenge+channelId+nodeId+timestamp",
  "timestamp": "2025-10-03T00:00:02Z"
}
```

**AUTHENTICATION_RESPONSE (encrypted via AES-256-GCM):**
```json
{
  "authenticated": true,
  "nodeId": "IRN-Hospital-XYZ",
  "sessionToken": "session-token-guid",
  "sessionExpiresAt": "2025-10-03T01:00:02Z",
  "grantedCapabilities": ["query:read", "data:write"],
  "message": "Authentication successful",
  "nextPhase": "phase4_session",
  "timestamp": "2025-10-03T00:00:02Z"
}
```

**Technical Details**:
- **Challenge TTL**: 5 minutes (300 seconds)
- **Session Token TTL**: 1 hour (3600 seconds)
- **Signature Format**: RSA-2048 signature of `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
- **Challenge Storage**: In-memory or Redis `ConcurrentDictionary<string, ChallengeData>` (key: `{ChannelId}:{NodeId}`)
- **One-time Use**: Challenge is invalidated after successful authentication or expiration
- **Verification**: Uses node's registered certificate public key for signature verification
- **Session Creation**: Session is created using `ChannelContext.IdentifiedNodeId` (Guid), not the NodeId string

**Production Endpoints**:
- `POST /api/node/challenge` (decorated with `[PrismEncryptedChannelConnection<ChallengeRequest>]`)
- `POST /api/node/authenticate` (decorated with `[PrismEncryptedChannelConnection<ChallengeResponseRequest>]`)

**Testing Helper Endpoints** (Development/NodeA/NodeB environments only):
- `POST /api/testing/request-challenge` - Client-side wrapper that calls `NodeChannelClient.RequestChallengeAsync()`
  - Input: `{channelId, nodeId}`
  - Output: Challenge response with instructions for next step
- `POST /api/testing/sign-challenge` - Helper to sign challenge data in correct format
  - Input: `{challengeData, channelId, nodeId, certificateWithPrivateKey, password, timestamp}`
  - Output: `{signature, signedData}` - signature in correct format for authentication
  - Eliminates manual formatting errors when testing
- `POST /api/testing/authenticate` - Client-side wrapper that calls `NodeChannelClient.AuthenticateAsync()`
  - Input: `{channelId, nodeId, challengeData, signature, timestamp}`
  - Output: Authentication response with session token

**Manual Testing Script**:
- `test-phase3.sh` - Complete end-to-end Bash script that tests Phases 1→2→3
  - Establishes encrypted channel
  - Generates certificate
  - Registers node
  - Approves node
  - Requests challenge
  - Signs challenge
  - Authenticates and obtains session token

### Phase 4: Session Establishment ✅ IMPLEMENTED

**Prerequisite**: This phase only occurs after successful mutual authentication (Phase 3).

**⚠️ REQUIREMENT**: All messages in this phase **MUST** be encrypted using the channel established in Phase 1. The `X-Channel-Id` header is mandatory in all requests.

```
Node A (Initiator)                    Node B (Receiver)
     |                                    |
     |==== Encrypted Channel Active =====|
     |                                    |
     |------ SESSION_WHOAMI ------------->|
     |       (encryptedPayload)           |
     |       {sessionToken, channelId}    |
     |                                    |
     |<----- SESSION_INFO ----------------|
     |       (encryptedPayload)           |
     |       {nodeId, capabilities, ...}  |
     |                                    |
     |------ SESSION_RENEW -------------->|
     |       (encryptedPayload)           |
     |                                    |
     |<----- SESSION_RENEWED --------------|
     |       (encryptedPayload)           |
     |                                    |
```

**Objective**: Manage the authenticated session lifecycle with support for capabilities, rate limiting, and maintenance operations.

**IMPORTANT**: All Phase 4 endpoints **MUST** use the encrypted channel established in Phase 1. The session token is sent **inside** the encrypted payload, **NOT** in HTTP headers.

#### Session Endpoints

**1. WhoAmI (Session Information)**

Endpoint: `POST /api/session/whoami`

Request (encrypted):
```json
{
  "channelId": "channel-id",
  "sessionToken": "session-token-guid",
  "timestamp": "2025-10-03T10:30:00Z"
}
```

Response (encrypted):
```json
{
  "sessionToken": "session-token-guid",
  "nodeId": "IRN-Hospital-XYZ",
  "channelId": "channel-id",
  "expiresAt": "2025-10-03T11:30:00Z",
  "remainingSeconds": 3600,
  "capabilities": ["query:read", "data:write"],
  "requestCount": 5,
  "timestamp": "2025-10-03T10:30:01Z"
}
```

**2. Renew Session**

Endpoint: `POST /api/session/renew`

Request (encrypted):
```json
{
  "channelId": "channel-id",
  "sessionToken": "session-token-guid",
  "additionalSeconds": 1800,
  "timestamp": "2025-10-03T10:30:00Z"
}
```

Response (encrypted):
```json
{
  "sessionToken": "session-token-guid",
  "nodeId": "IRN-Hospital-XYZ",
  "expiresAt": "2025-10-03T12:00:00Z",
  "remainingSeconds": 5400,
  "message": "Session renewed for 1800 seconds",
  "timestamp": "2025-10-03T10:30:01Z"
}
```

**3. Revoke Session (Logout)**

Endpoint: `POST /api/session/revoke`

Request (encrypted):
```json
{
  "channelId": "channel-id",
  "sessionToken": "session-token-guid",
  "timestamp": "2025-10-03T10:30:00Z"
}
```

Response (encrypted):
```json
{
  "sessionToken": "session-token-guid",
  "nodeId": "IRN-Hospital-XYZ",
  "revoked": true,
  "message": "Session revoked successfully",
  "timestamp": "2025-10-03T10:30:01Z"
}
```

**4. Session Metrics (requires `NodeAccessTypeEnum.Admin`)**

Endpoint: `POST /api/session/metrics`

Request (encrypted):
```json
{
  "channelId": "channel-id",
  "sessionToken": "session-token-guid",
  "nodeId": "optional-target-node-id",
  "timestamp": "2025-10-03T10:30:00Z"
}
```

Response (encrypted):
```json
{
  "nodeId": "IRN-Hospital-XYZ",
  "activeSessions": 3,
  "totalRequests": 150,
  "lastAccessedAt": "2025-10-03T10:29:55Z",
  "nodeAccessLevel": "ReadWrite"
}
```

#### Access Levels (NodeAccessTypeEnum)

The system uses a hierarchical enum for access levels:

- `ReadOnly` (0) - Read/query federated data (basic access)
- `ReadWrite` (1) - Submit and modify research data
- `Admin` (2) - Full node administration and metrics access

**Hierarchy**: `Admin` > `ReadWrite` > `ReadOnly`

Endpoints verify `sessionContext.NodeAccessLevel >= RequiredCapability`

#### Rate Limiting

- **Algorithm**: Token bucket
- **Limit**: 60 requests per minute per session
- **Response**: HTTP 429 (Too Many Requests) when exceeded

#### Security Implementation

1. **Chained Attributes**:
```csharp
[HttpPost("whoami")]
[PrismEncryptedChannelConnection<WhoAmIRequest>]  // 1st: Decrypt payload
[PrismAuthenticatedSession]                        // 2nd: Validate session
public IActionResult WhoAmI() { ... }
```

2. **Session Token Extraction**:
   - Token extracted from decrypted payload via reflection
   - Existence and format validation
   - Expiration verification (1 hour TTL)

3. **Access Level-Based Authorization**:
```csharp
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
```

4. **Response Encryption**:
```csharp
var response = new { /* data */ };
var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
return Ok(encrypted);
```

#### Encrypted Payload Format

All endpoints use the same format as Phases 2-3:

Request/Response (HTTP Body):
```json
{
  "encryptedData": "base64-encoded-AES-256-GCM-ciphertext",
  "iv": "base64-encoded-initialization-vector",
  "authTag": "base64-encoded-authentication-tag"
}
```

#### Testing Helpers (development only)

No specific helpers for Phase 4. Use the `test-phase4.sh` script for automated end-to-end testing.

## Complete Flow Summary

### Scenario 1: Known and Authorized Node (Full Handshake)
1. **Phase 1**: Node A opens encrypted channel with Node B
2. **Phase 2**: Node A identifies itself → Node B responds with `isKnown: true`, `status: "Authorized"`, includes both `nodeId` (string) and `registrationId` (Guid)
3. **Phase 3**: Mutual authentication via challenge-response (session created using `registrationId`)
4. **Phase 4**: Session management with negotiated capabilities

**Result**: Session established, communication authorized.

### Scenario 2: Unknown Node (Requires Registration)
1. **Phase 1**: Node A opens encrypted channel with Node B
2. **Phase 2**: Node A identifies itself → Node B responds with `isKnown: false`, `status: "Unknown"`, provides registration endpoint
3. Node A initiates registration process (outside handshake)
4. Administrator of Node B approves/rejects registration
5. If approved, Node A can restart handshake (returns to Scenario 1)

**Result**: Handshake interrupted, registration required.

## Administrative Operations

### Update Node Status

Administrative endpoint for updating node authorization status:

**Endpoint**: `PUT /api/node/{id:guid}/status`

**Route Parameter**: `id` - The **RegistrationId** (Guid) of the node to update

**Request Body**:
```json
{
  "status": "Authorized"  // or "Pending", "Revoked"
}
```

**Response**:
```json
{
  "success": true,
  "nodeId": "IRN-Hospital-XYZ",
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newStatus": "Authorized"
}
```

**Usage**: Clients should store the `registrationId` received in the Phase 2 `NODE_STATUS` response for administrative operations.

## Security

### Cryptography

**Phase 1 - Channel Establishment (Ephemeral Keys)**:
- **Key Exchange**: ECDH (Elliptic Curve Diffie-Hellman) with P-384 curve
- **Ephemeral Keys**: Temporary key pairs generated for each session
- **Key Derivation**: HKDF-SHA256 to derive symmetric keys from shared secret
- **Perfect Forward Secrecy (PFS)**: Compromise of permanent keys doesn't affect past sessions

**Phases 2-4 - Communication over Channel**:
- **Symmetric**: AES-256-GCM (key derived from ECDH)
- **Digital Signature**: ECDSA P-384 or RSA 4096 (using permanent node keys)
- **Hash**: SHA-256 for integrity verification

**Permanent vs Ephemeral Keys**:
- **Permanent Keys**: Used only for digital signatures (authentication in Phase 3)
- **Ephemeral Keys**: Used for channel encryption (Phase 1) and discarded after session

### Certificates

- Each node must possess a valid X.509 certificate
- Support for institutional PKI or self-signed certificates (with prior registration)
- Revocation validation (CRL/OCSP)
- **Certificate Fingerprint**: SHA-256 hash used as natural key for node identification

### Attack Prevention

- **Replay Attack**: Unique nonces with timestamps in all messages
- **Man-in-the-Middle**:
  - Ephemeral keys with ECDH (Phase 1)
  - Signature validation with permanent keys (Phase 3)
  - X.509 certificate validation
  - Optional certificate pinning
- **Perfect Forward Secrecy**: Discarded ephemeral keys ensure future compromise doesn't affect past sessions
- **Key Compromise Impersonation**: Mutual authentication in Phase 3 with permanent keys
- **DoS**: Rate limiting and aggressive handshake timeout

### HTTP Client Configuration

**Configurable Timeout**: The HTTP client used for inter-node communication supports configurable timeout settings (default: 5 minutes / 300 seconds).

**Configuration** (in `appsettings.json`):
```json
{
  "HttpClient": {
    "TimeoutSeconds": 300
  }
}
```

## Error Handling

### Common Error Codes

| Code | Description | Phase | Action |
|------|-------------|-------|--------|
| `ERR_CHANNEL_FAILED` | Failed to establish encrypted channel | 1 | Verify cipher/ECDH support |
| `ERR_INVALID_EPHEMERAL_KEY` | Invalid or malformed ephemeral key | 1 | Regenerate ephemeral key pair |
| `ERR_KEY_DERIVATION_FAILED` | Shared key derivation failure | 1 | Verify ECDH/HKDF implementation |
| `ERR_INVALID_CERTIFICATE` | Invalid or expired certificate | 1-3 | Renew certificate |
| `ERR_UNKNOWN_NODE` | Node not registered in federation | 2 | Register node |
| `ERR_NODE_UNAUTHORIZED` | Known node but not authorized | 2 | Await approval |
| `ERR_INCOMPATIBLE_VERSION` | Incompatible protocol version | 1-2 | Update software |
| `ERR_AUTH_FAILED` | Authentication failure | 3 | Verify credentials |
| `ERR_TIMEOUT` | Timeout during handshake | All | Verify connectivity |
| `ERR_INVALID_SIGNATURE` | Invalid signature (permanent key) | 3 | Verify permanent keys |

### Error Response Examples

**ERR_UNKNOWN_NODE (Phase 2):**
```json
{
  "error": {
    "code": "ERR_UNKNOWN_NODE",
    "message": "Node not registered in the network",
    "details": {
      "registrationRequired": true,
      "registrationEndpoint": "https://node-b.example.com/api/node/register"
    },
    "retryable": true,
    "retryAfter": "after_registration"
  }
}
```

**ERR_INVALID_CERTIFICATE (Phase 1):**
```json
{
  "error": {
    "code": "ERR_INVALID_CERTIFICATE",
    "message": "Certificate has expired",
    "details": {
      "certificateExpiry": "2025-09-01T00:00:00Z",
      "currentTime": "2025-10-01T12:00:00Z"
    },
    "retryable": false
  }
}
```

**ERR_INVALID_EPHEMERAL_KEY (Phase 1):**
```json
{
  "error": {
    "code": "ERR_INVALID_EPHEMERAL_KEY",
    "message": "Ephemeral public key is invalid or malformed",
    "details": {
      "reason": "invalid_curve_point"
    },
    "retryable": true
  }
}
```

**ERR_AUTH_FAILED (Phase 3):**
```json
{
  "error": {
    "code": "ERR_AUTH_FAILED",
    "message": "Authentication challenge verification failed",
    "details": {
      "reason": "invalid_signature"
    },
    "retryable": false
  }
}
```

## Implementation

### Current State
✅ **Complete** - All 4 phases implemented and tested

### Service Registration (Program.cs)

All services are registered as **Singleton** (shared state across requests):
- `IEphemeralKeyService` - ECDH key generation
- `IChannelEncryptionService` - Crypto operations
- `INodeChannelClient` - HTTP client for initiating handshakes
- `INodeRegistryService` - Node registry (PostgreSQL or in-memory)
- `IChallengeService` - Challenge-response authentication (Phase 3)
- `ISessionService` - Session lifecycle management (Phase 4)
- `IRedisConnectionService` - Redis connection management (conditional)
- `ISessionStore` - Session persistence (Redis or in-memory based on feature flags)
- `IChannelStore` - Channel persistence (Redis or in-memory based on feature flags)

### Code References

Main implementation files:
- `Controllers/ChannelController.cs` - Phase 1 endpoints
- `Controllers/NodeConnectionController.cs` - Phases 2-3 endpoints (registration, identification, authentication)
- `Controllers/SessionController.cs` - Phase 4 endpoints (whoami, renew, revoke, metrics)
- `Services/Node/EphemeralKeyService.cs` - ECDH ephemeral key generation and management
- `Services/Node/ChannelEncryptionService.cs` - Symmetric key derivation (HKDF) and channel encryption (Phase 1)
- `Services/Node/NodeRegistryService.cs` - Known node management (Phase 2)
- `Services/Node/PostgreSqlNodeRegistryService.cs` - PostgreSQL-backed node registry
- `Services/Node/ChallengeService.cs` - Mutual authentication with permanent keys (Phase 3)
- `Services/Session/SessionService.cs` - Session management (Phase 4)
- `Domain/Requests/Node/` - All request DTOs
- `Domain/Responses/Node/` - All response DTOs
- `Domain/Entities/Node/ResearchNode.cs` - Node entity with dual identifiers

## Testing

### Test Scenarios

1. **Full Handshake (Known Node)**
   - Two known nodes with valid certificates
   - Verify all 4 phases execute correctly
   - Verify session creation

2. **Unknown Node**
   - Node A attempts to connect to Node B
   - Node B doesn't recognize Node A
   - Verify `isKnown: false` return with registration endpoint
   - Verify handshake is interrupted at Phase 2

3. **Encrypted Channel with Ephemeral Keys (Phase 1)**
   - Verify correct ECDH ephemeral key generation
   - Verify ephemeral public key exchange
   - Verify shared secret derivation (ECDH)
   - Verify symmetric key derivation (HKDF)
   - Test with incompatible ciphers
   - Verify sensitive data isn't transmitted before channel
   - Verify ephemeral key disposal at session end

4. **Expired Certificate**
   - Node with expired certificate
   - Verify appropriate rejection in Phase 1

5. **Authentication Failure (Phase 3)**
   - Invalid signature on challenge
   - Verify rejection and appropriate error code

6. **Incompatible Version**
   - Nodes with different protocol versions
   - Verify clear error in Phase 1

7. **Timeout**
   - Simulate network latency in each phase
   - Verify timeout behavior

8. **Replay Attack**
   - Resend handshake message
   - Verify rejection due to duplicate nonce

9. **Man-in-the-Middle**
   - Simulate channel interception
   - Verify sensitive data is encrypted
   - Verify ephemeral keys can't be reused

10. **Perfect Forward Secrecy**
    - Simulate permanent key compromise after session
    - Verify past session data can't be decrypted
    - Confirm ephemeral keys were discarded

11. **Dual Identifier Flow**
    - Verify NodeId (string) is used in protocol DTOs
    - Verify RegistrationId (Guid) is returned in responses
    - Verify ChannelContext stores Guid after Phase 2
    - Verify administrative operations use Guid

12. **Certificate Fingerprint Uniqueness**
    - Register node with certificate
    - Attempt re-registration with same certificate but different NodeId
    - Verify existing record is updated, not duplicated
    - Verify certificate fingerprint is the true unique key

### Test Status

**Overall: 73/75 tests passing (97.3% pass rate)** ✅

See `docs/PROJECT_STATUS.md` for detailed test results.

### Testing Scripts

- `test-phase4.sh` - **Complete end-to-end test (Phases 1+2+3+4)** with Bash - **Use this!**
- `test-phase3.sh` - End-to-end test (Phases 1+2+3) - deprecated, use phase4
- All automated tests: `dotnet test Bioteca.Prism.InteroperableResearchNode.Test`

## Dependencies

- This document depends on: `node-communication.md`
- This document is a dependency of: `session-management.md`

## References

- RFC 8446 (TLS 1.3) - Handshake mechanism inspiration
- OAuth 2.0 mTLS - Mutual authentication standard
- X.509 Certificate Standards
- ECDH (Elliptic Curve Diffie-Hellman) - Key exchange
- HKDF (HMAC-based Key Derivation Function) - RFC 5869
