# User Session Architecture

**Last Updated**: 2025-10-24
**Version**: 0.10.0
**Status**: Production

---

## Table of Contents

1. [Overview](#overview)
2. [Two Authentication Systems](#two-authentication-systems)
3. [User Authentication Flow](#user-authentication-flow)
4. [Node Session Flow (Phase 4)](#node-session-flow-phase-4)
5. [Integration with Encryption Channels](#integration-with-encryption-channels)
6. [Middleware Architecture](#middleware-architecture)
7. [Storage Architecture](#storage-architecture)
8. [Security Mechanisms](#security-mechanisms)
9. [Configuration](#configuration)
10. [API Reference](#api-reference)
11. [Common Patterns](#common-patterns)
12. [Troubleshooting](#troubleshooting)

---

## Overview

PRISM implements **two separate authentication systems** serving different purposes:

1. **User Authentication** - For human researchers accessing the system via web interfaces or mobile apps
2. **Node Session Authentication** - For federated node-to-node communication in the research network

These systems operate **independently** and have different security requirements, token formats, and validation mechanisms.

### Key Differences at a Glance

| Aspect | User Authentication | Node Session Authentication |
|--------|-------------------|---------------------------|
| **Purpose** | Human researchers accessing the system | Node-to-node federated communication |
| **Token Type** | JWT Bearer token | Session GUID (stored in Redis) |
| **Header Pattern** | `Authorization: Bearer {jwt}` | `X-Session-Id: {sessionToken}` |
| **Duration** | 1 hour (configurable) | 1 hour (renewable) |
| **Signature** | RS256 (RSA-2048) | RSA-2048 + ECDH P-384 |
| **Storage** | Client-side (localStorage) | Server-side (Redis) |
| **Phases** | Single login endpoint | 4-phase handshake protocol |
| **Encryption Required** | No (HTTPS only) | Yes (AES-256-GCM channel) |
| **Rate Limiting** | Application-level | 60 req/min via Redis |
| **Authorization** | Role-based (User, Admin) | Capability-based (ReadOnly, ReadWrite, Admin) |

---

## Two Authentication Systems

### 1. User Authentication System

**Purpose**: Authenticate human researchers who need to access the PRISM system through web interfaces or mobile applications.

**Target Users**:
- Researchers conducting studies
- Data analysts reviewing research data
- System administrators managing the node

**Key Features**:
- JWT-based authentication (RS256 signature)
- SHA512 password hashing
- Role-based access control
- Token refresh mechanism
- Master password override (development only)

**Endpoints**:
- `POST /api/userauth/login` - User login with credentials
- `POST /api/userauth/refreshtoken` - Refresh JWT token
- `POST /api/userauth/encrypt` - Utility for password hashing

**Use Cases**:
- Web dashboard access
- Mobile app authentication
- API access for data submission
- Administrative operations

### 2. Node Session Authentication System

**Purpose**: Authenticate remote research nodes for federated data exchange in the PRISM network.

**Target Users**:
- Other PRISM nodes in the federation
- Federated query engines
- Inter-institutional data exchanges

**Key Features**:
- 4-phase handshake protocol
- End-to-end encryption (AES-256-GCM)
- Challenge-response mutual authentication
- Capability-based authorization
- Automatic rate limiting (60 req/min)
- Redis-backed session persistence

**Endpoints**:
- Phase 1: `/api/channel/open`, `/api/channel/initiate`
- Phase 2: `/api/channel/identify`, `/api/node/register`
- Phase 3: `/api/node/challenge`, `/api/node/authenticate`
- Phase 4: `/api/session/whoami`, `/api/session/renew`, `/api/session/revoke`, `/api/session/metrics`

**Use Cases**:
- Cross-institutional data queries
- Federated research collaboration
- Secure node-to-node communication
- Distributed clinical trial coordination

---

## User Authentication Flow

### Step 1: Initial Login

```http
POST /api/userauth/login HTTP/1.1
Content-Type: application/json

{
  "username": "researcher@institution.edu",
  "password": "Base64EncodedPassword",
  "researchId": "optional-research-project-guid"
}
```

**Server-Side Processing**:
1. Decode Base64 password
2. Query `User` entity by username
3. Compute SHA512 hash of submitted password
4. Compare with stored `PasswordHash`
5. Optional: Validate researcher access to specific research project
6. Generate JWT token with RS256 signature

**Response**:
```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-24T15:30:00Z"
}
```

### Step 2: JWT Token Structure

```json
{
  "sub": "00000000-0000-0000-0000-000000000000",
  "login": "researcher@institution.edu",
  "name": "Dr. Jane Researcher",
  "email": "researcher@institution.edu",
  "orcid": "0000-0001-2345-6789",
  "researches": ["research-guid-1", "research-guid-2"],
  "exp": 1729783800,
  "iss": "PRISM-IRN",
  "aud": "PRISM-IRN"
}
```

### Step 3: Authenticated Requests

```http
GET /api/research/12345678-1234-1234-1234-123456789012 HTTP/1.1
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Server-Side Validation**:
1. Extract token from `Authorization` header
2. Verify JWT signature using RSA public key
3. Validate issuer (`PRISM-IRN`) and audience
4. Check expiration timestamp
5. Extract user claims (sub, login, researches)
6. Enforce role-based authorization

### Step 4: Token Refresh

```http
POST /api/userauth/refreshtoken HTTP/1.1
Content-Type: application/json
Authorization: Bearer {current_jwt}

{
  "researchId": "12345678-1234-1234-1234-123456789012"
}
```

**Response**: New JWT token with extended expiration.

---

## Node Session Flow (Phase 4)

Node sessions represent the **final phase** of the 4-phase handshake protocol. They assume that Phases 1-3 have been successfully completed:

- **Phase 1** ✅ - Encrypted channel established (AES-256-GCM)
- **Phase 2** ✅ - Node identified (X.509 certificate + RSA signature)
- **Phase 3** ✅ - Mutual authentication completed (challenge-response)
- **Phase 4** 🔄 - Session management (this section)

### Prerequisites

Before entering Phase 4, you must have:
1. **ChannelId** - From Phase 1 channel establishment
2. **Symmetric Key** - Derived from ECDH P-384 key exchange
3. **NodeId** - Your node's protocol identifier
4. **RegistrationId** - Database Guid from Phase 2 identification
5. **Session Token** - Guid received after Phase 3 authentication

### Step 1: Session Creation (Automatic in Phase 3)

Sessions are **automatically created** during Phase 3 authentication:

```http
POST /api/node/authenticate HTTP/1.1
X-Channel-Id: channel-12345678
Content-Type: application/json

{
  "NodeId": "node-a",
  "ChallengeId": "challenge-guid",
  "Signature": "Base64EncodedRSASignature",
  "Timestamp": "2025-10-24T10:00:00Z"
}
```

**Response (Encrypted)**:
```json
{
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "expiresAt": "2025-10-24T11:00:00Z",
  "capabilities": ["ReadOnly", "ReadWrite"],
  "channelId": "channel-12345678"
}
```

**Session Storage** (Redis):
```
Key: session:session-87654321-4321-4321-4321-876543210987
Value: {
  "SessionToken": "session-87654321-4321-4321-4321-876543210987",
  "NodeId": "node-registration-guid",
  "ChannelId": "channel-12345678",
  "CreatedAt": "2025-10-24T10:00:00Z",
  "ExpiresAt": "2025-10-24T11:00:00Z",
  "LastAccessedAt": "2025-10-24T10:00:00Z",
  "AccessLevel": "ReadWrite",
  "RequestCount": 0,
  "IpAddress": "192.168.1.100"
}
TTL: 3600 seconds (auto-expires)
```

### Step 2: Using Session Token

**RECOMMENDED: X-Session-Id Header (v0.10.0+)**:
```http
POST /api/session/whoami HTTP/1.1
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
Content-Type: application/json

{
  "Timestamp": "2025-10-24T10:05:00Z"
}
```

**DEPRECATED: Body-based token (will remove in v0.11.0)**:
```http
POST /api/session/whoami HTTP/1.1
X-Channel-Id: channel-12345678
Content-Type: application/json

{
  "ChannelId": "channel-12345678",
  "SessionToken": "session-87654321-4321-4321-4321-876543210987",
  "Timestamp": "2025-10-24T10:05:00Z"
}
```

**Server-Side Processing**:
1. **PrismEncryptedChannelConnection Middleware**:
   - Validates `X-Channel-Id` header
   - Retrieves symmetric key from Redis
   - Decrypts JSON payload using AES-256-GCM
   - Stores decrypted request in `HttpContext.Items["DecryptedRequest"]`

2. **PrismAuthenticatedSession Middleware**:
   - Extracts session token from `X-Session-Id` header (or body for backward compatibility)
   - Queries Redis for session: `session:{sessionToken}`
   - Validates expiration timestamp
   - Checks capability requirements
   - Applies rate limiting (60 req/min via Redis Sorted Sets)
   - Updates `LastAccessedAt` timestamp
   - Increments `RequestCount`
   - Stores session context in `HttpContext.Items["SessionContext"]`
   - Adds `X-Session-Id` response header

3. **Controller Action Execution**:
   - Access session info via `HttpContext.Items["SessionContext"]`
   - Execute business logic
   - Return response (automatically encrypted by middleware)

**Response**:
```json
{
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "nodeId": "node-registration-guid",
  "channelId": "channel-12345678",
  "createdAt": "2025-10-24T10:00:00Z",
  "expiresAt": "2025-10-24T11:00:00Z",
  "lastAccessedAt": "2025-10-24T10:05:00Z",
  "capabilities": ["ReadOnly", "ReadWrite"],
  "requestCount": 5
}
```

### Step 3: Session Renewal

```http
POST /api/session/renew HTTP/1.1
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
Content-Type: application/json

{
  "Timestamp": "2025-10-24T10:50:00Z"
}
```

**Response**:
```json
{
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "expiresAt": "2025-10-24T11:50:00Z",
  "message": "Session renewed successfully"
}
```

**Server-Side Processing**:
1. Validate current session token
2. Check if session is renewable (not expired, not revoked)
3. Extend `ExpiresAt` by 1 hour (default)
4. Update Redis TTL
5. Return new expiration timestamp

### Step 4: Session Revocation (Logout)

```http
POST /api/session/revoke HTTP/1.1
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
Content-Type: application/json

{
  "Timestamp": "2025-10-24T11:00:00Z"
}
```

**Server-Side Processing**:
1. Validate session token
2. Remove session from Redis: `DEL session:{sessionToken}`
3. Session becomes immediately invalid
4. All subsequent requests with this token will fail

**Response**:
```json
{
  "message": "Session revoked successfully",
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "revokedAt": "2025-10-24T11:00:00Z"
}
```

---

## Integration with Encryption Channels

### Channel Lifecycle

**Phase 1: Channel Establishment**:
1. Initiator generates ECDH P-384 ephemeral key pair
2. Responder generates ECDH P-384 ephemeral key pair
3. ECDH key exchange produces shared secret
4. HKDF-SHA256 derives AES-256-GCM symmetric key
5. Channel persists in Redis with TTL

**Channel Storage** (Redis):
```
Key: channel:{channelId}
Value: {
  "ChannelId": "channel-12345678",
  "SymmetricKey": "Base64EncodedKey",
  "CreatedAt": "2025-10-24T10:00:00Z",
  "ExpiresAt": "2025-10-24T12:00:00Z",
  "InitiatorNodeId": "node-a",
  "ResponderNodeId": "node-b"
}
TTL: 7200 seconds (2 hours)
```

### Encryption/Decryption Flow

**Outgoing Request (Client)**:
1. Serialize request DTO to JSON
2. Retrieve channel symmetric key from local storage
3. Generate random 12-byte nonce (IV)
4. Encrypt JSON with AES-256-GCM (key, nonce, JSON)
5. Encode ciphertext as Base64
6. Send: `X-Channel-Id` header + encrypted payload

**Incoming Request (Server)**:
1. Extract `X-Channel-Id` from header
2. Query Redis for channel: `channel:{channelId}`
3. Validate channel expiration
4. Extract symmetric key
5. Decode Base64 ciphertext
6. Decrypt with AES-256-GCM (key, nonce, ciphertext)
7. Deserialize JSON to request DTO
8. Store in `HttpContext.Items["DecryptedRequest"]`

**Outgoing Response (Server)**:
1. Serialize response DTO to JSON
2. Retrieve channel symmetric key from Redis
3. Generate random 12-byte nonce
4. Encrypt JSON with AES-256-GCM
5. Return encrypted response (middleware handles automatically)

**Incoming Response (Client)**:
1. Retrieve channel symmetric key
2. Decode Base64 ciphertext from response
3. Decrypt with AES-256-GCM
4. Deserialize JSON to response DTO

### Why User Auth Doesn't Use Channels

**User authentication does NOT require encrypted channels** because:

1. **Different Threat Model**:
   - User auth: Browser/mobile ↔ Single node (HTTPS provides transport security)
   - Node auth: Node ↔ Node ↔ Node (federation requires end-to-end encryption)

2. **Trust Boundary**:
   - Users trust their local node (institutional control)
   - Nodes don't trust other federation members (inter-institutional)

3. **Performance**:
   - User requests are interactive (low latency required)
   - Node requests are batch/query operations (encryption overhead acceptable)

4. **Complexity**:
   - User auth: Standard OAuth2/OIDC flows (browser-compatible)
   - Node auth: Custom federated protocol (server-to-server)

**User requests rely on HTTPS/TLS 1.3** for transport security, which is sufficient for institutional access control.

---

## Middleware Architecture

### Middleware Execution Order

PRISM uses two custom middleware attributes that execute in a specific order:

```csharp
[PrismEncryptedChannelConnection<TRequest>]  // 1. Decrypt payload
[PrismAuthenticatedSession]                   // 2. Validate session
public IActionResult MyAction() { ... }
```

### 1. PrismEncryptedChannelConnection Middleware

**Location**: `Bioteca.Prism.Core/Middleware/Channel/PrismEncryptedChannelConnectionAttribute.cs`

**Purpose**: Decrypt incoming payloads using the symmetric key from Phase 1 channel.

**Execution Flow**:
```
1. Extract X-Channel-Id from request header
2. Validate header presence
3. Query IChannelStore for channel data
4. Validate channel expiration
5. Read request body stream
6. Deserialize to EncryptedPayload DTO
7. Decode Base64 ciphertext
8. Decrypt using AES-256-GCM (key from channel)
9. Deserialize decrypted JSON to TRequest type
10. Store in HttpContext.Items["DecryptedRequest"]
11. Store in HttpContext.Items["ChannelContext"]
12. Call next middleware
```

**Error Scenarios**:
- Missing `X-Channel-Id` header → 400 Bad Request
- Channel not found in Redis → 404 Not Found
- Channel expired → 410 Gone
- Decryption failure → 400 Bad Request (invalid payload)
- Deserialization failure → 400 Bad Request (malformed JSON)

**HttpContext Items**:
```csharp
HttpContext.Items["ChannelContext"] = new ChannelContext {
    ChannelId = "channel-12345678",
    SymmetricKey = byte[],
    ExpiresAt = DateTime
};

HttpContext.Items["DecryptedRequest"] = TRequest object;
```

### 2. PrismAuthenticatedSession Middleware

**Location**: `Bioteca.Prism.InteroperableResearchNode/Middleware/PrismAuthenticatedSessionAttribute.cs`

**Purpose**: Validate session token and enforce capability-based authorization.

**Execution Flow**:
```
1. Extract session token from:
   a. X-Session-Id header (RECOMMENDED, v0.10.0+)
   b. Decrypted request body SessionToken property (DEPRECATED)
2. Validate session token presence
3. Query ISessionStore for session data
4. Validate session expiration
5. Check capability requirements (if specified)
6. Apply rate limiting (60 req/min via Redis)
7. Update LastAccessedAt timestamp
8. Increment RequestCount
9. Store SessionContext in HttpContext.Items
10. Add X-Session-Id response header
11. Call controller action
```

**Error Scenarios**:
- Missing session token → 401 Unauthorized
- Session not found → 401 Unauthorized
- Session expired → 401 Unauthorized
- Insufficient capabilities → 403 Forbidden
- Rate limit exceeded → 429 Too Many Requests

**HttpContext Items**:
```csharp
HttpContext.Items["SessionContext"] = new SessionContext {
    SessionToken = "session-guid",
    NodeId = Guid,
    ChannelId = "channel-id",
    Capabilities = [ReadOnly, ReadWrite],
    CreatedAt = DateTime,
    ExpiresAt = DateTime,
    RequestCount = 42
};
```

**Capability Enforcement**:
```csharp
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
public IActionResult AdminOnlyAction() { ... }

[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]
public IActionResult WriteAction() { ... }

[PrismAuthenticatedSession] // No capability requirement = any authenticated session
public IActionResult ReadAction() { ... }
```

### Rate Limiting Implementation

**Redis Sorted Set Pattern**:
```
Key: rate-limit:session:{sessionToken}
Members: {timestamp1}: {timestamp1}, {timestamp2}: {timestamp2}, ...
Score: Unix timestamp (milliseconds)
TTL: 60 seconds
```

**Algorithm**:
1. Current window start: `now - 60 seconds`
2. Remove expired entries: `ZREMRANGEBYSCORE key -inf {window_start}`
3. Count remaining entries: `ZCARD key`
4. If count >= 60: Reject request (429 Too Many Requests)
5. Add new entry: `ZADD key {now} {now}`
6. Set TTL: `EXPIRE key 60`

**Benefits**:
- Sliding window (more accurate than fixed window)
- Distributed (works across multiple node instances)
- Automatic cleanup (Redis TTL)
- Per-session isolation

### Complete Request Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client (Node A)                           │
│                                                                  │
│  1. Serialize request DTO to JSON                               │
│  2. Encrypt JSON with AES-256-GCM (channel key)                 │
│  3. Add X-Channel-Id and X-Session-Id headers                   │
│  4. Send HTTPS POST request                                     │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTPS (TLS 1.3)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Server (Node B)                             │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  ASP.NET Core Middleware Pipeline                          │ │
│  │                                                              │ │
│  │  5. Extract X-Channel-Id header                             │ │
│  │  6. Query Redis: channel:{channelId}                        │ │
│  │  7. Decrypt payload using channel symmetric key             │ │
│  │  8. Deserialize JSON to TRequest DTO                        │ │
│  │  9. Store in HttpContext.Items["DecryptedRequest"]          │ │
│  │                                                              │ │
│  │  [PrismEncryptedChannelConnection]                          │ │
│  └────────────────────┬───────────────────────────────────────┘ │
│                       │                                           │
│  ┌────────────────────▼───────────────────────────────────────┐ │
│  │  10. Extract X-Session-Id header (or body SessionToken)    │ │
│  │  11. Query Redis: session:{sessionToken}                   │ │
│  │  12. Validate expiration timestamp                         │ │
│  │  13. Check capability requirements                         │ │
│  │  14. Apply rate limiting (60 req/min)                      │ │
│  │  15. Update LastAccessedAt, RequestCount                   │ │
│  │  16. Store SessionContext in HttpContext.Items             │ │
│  │  17. Add X-Session-Id response header                      │ │
│  │                                                              │ │
│  │  [PrismAuthenticatedSession]                                │ │
│  └────────────────────┬───────────────────────────────────────┘ │
│                       │                                           │
│  ┌────────────────────▼───────────────────────────────────────┐ │
│  │  18. Execute controller action                              │ │
│  │  19. Access SessionContext from HttpContext.Items           │ │
│  │  20. Execute business logic                                 │ │
│  │  21. Return response DTO                                    │ │
│  │                                                              │ │
│  │  [Controller Action]                                        │ │
│  └────────────────────┬───────────────────────────────────────┘ │
│                       │                                           │
│  ┌────────────────────▼───────────────────────────────────────┐ │
│  │  22. Serialize response DTO to JSON                         │ │
│  │  23. Encrypt JSON with AES-256-GCM (channel key)            │ │
│  │  24. Return encrypted response                              │ │
│  │                                                              │ │
│  │  [Response Encryption Middleware]                           │ │
│  └────────────────────┬───────────────────────────────────────┘ │
└────────────────────────┼────────────────────────────────────────┘
                         │ HTTPS (TLS 1.3)
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Client (Node A)                           │
│                                                                  │
│  25. Receive encrypted response                                 │
│  26. Decrypt using channel symmetric key                        │
│  27. Deserialize JSON to response DTO                           │
│  28. Process response                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Storage Architecture

### Redis-Based Storage (Production)

**Channel Storage**:
```
Key pattern: channel:{channelId}
Value: JSON-serialized ChannelData
TTL: 2 hours (7200 seconds)
```

**Session Storage**:
```
Key pattern: session:{sessionToken}
Value: JSON-serialized SessionData
TTL: 1 hour (3600 seconds), extended on renewal
```

**Rate Limiting Storage**:
```
Key pattern: rate-limit:session:{sessionToken}
Value: Sorted set (timestamp: timestamp)
TTL: 60 seconds
```

**Redis Connection Management**:
- `IRedisConnectionService` - Singleton service
- Lazy connection initialization
- Connection string from `appsettings.json`
- Automatic retry on connection failure

### In-Memory Storage (Development/Testing)

**Fallback when Redis is disabled**:
```json
{
  "FeatureFlags": {
    "UseRedisForSessions": false,
    "UseRedisForChannels": false
  }
}
```

**Implementations**:
- `InMemoryChannelStore` - ConcurrentDictionary<string, ChannelData>
- `InMemorySessionStore` - ConcurrentDictionary<string, SessionData>
- Manual TTL management via background cleanup task

**Limitations**:
- Per-process storage (not distributed)
- Lost on application restart
- No rate limiting (not implemented in-memory)

### PostgreSQL Storage (Node Registry Only)

**Node entities** are stored in PostgreSQL:
- `NodeRegistration` table - Node metadata (NodeId, RegistrationId, Certificate, etc.)
- `NodeChallenge` table - Challenge-response records (Phase 3)

**Session and channel data is NOT stored in PostgreSQL** (Redis only).

---

## Security Mechanisms

### User Authentication Security

**Password Hashing**:
- Algorithm: SHA512
- Encoding: UTF-8
- Salt: Not implemented (should be added for production)
- Master password: Development-only override (disable in production)

**JWT Signature**:
- Algorithm: RS256 (RSA-2048)
- Key pair: Generated per node instance
- Public key: Shared for token verification
- Private key: Never leaves the node

**Token Validation**:
```csharp
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "PRISM-IRN",
    ValidateAudience = true,
    ValidAudience = "PRISM-IRN",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
    IssuerSigningKey = rsaPublicKey
};
```

**Security Recommendations**:
- Use PBKDF2 or Argon2 for password hashing (not SHA512)
- Implement salt + pepper for password storage
- Rotate JWT signing keys periodically
- Implement token revocation list (blacklist)
- Add 2FA for administrative users

### Node Session Security

**Phase 1 - Encrypted Channel**:
- ECDH P-384: Elliptic curve key exchange
- HKDF-SHA256: Key derivation function
- AES-256-GCM: Authenticated encryption (confidentiality + integrity)
- 12-byte random nonce (IV) per message
- Perfect Forward Secrecy: Ephemeral keys discarded after session

**Phase 2 - Node Identification**:
- X.509 certificates: Node identity proof
- SHA-256 fingerprint: Natural key for node lookup
- RSA-2048 signature: Request authentication
- Certificate validation: Issuer, validity period, revocation status

**Phase 3 - Mutual Authentication**:
- 32-byte random challenge: Cryptographically secure (CSPRNG)
- 5-minute TTL: Prevents replay attacks
- One-time use: Challenge deleted after successful auth
- RSA-2048 signature: Proves private key possession

**Phase 4 - Session Management**:
- Session token: GUID (128-bit random)
- Server-side storage: Redis (not client-side)
- Automatic TTL: 1 hour, renewable
- Rate limiting: 60 req/min via Redis Sorted Sets
- Capability enforcement: ReadOnly, ReadWrite, Admin
- Audit logging: All session operations logged

**Security Properties**:
- Confidentiality: AES-256-GCM encryption
- Integrity: GCM authentication tag
- Authenticity: RSA-2048 signatures
- Forward Secrecy: Ephemeral ECDH keys
- Replay Protection: One-time challenges + timestamps
- Denial of Service Protection: Rate limiting
- Authorization: Capability-based access control

---

## Configuration

### User Authentication Configuration

**appsettings.json**:
```json
{
  "Jwt": {
    "Expiration": {
      "Minutes": 60
    },
    "PrivateKey": "Certificates/node-a-private.key",
    "PublicKey": "Certificates/node-a-public.key"
  },
  "BiotecaAuth": {
    "Password": {
      "Master": "$SHA512$00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"
    }
  }
}
```

**Environment-Specific Overrides** (appsettings.NodeA.json):
```json
{
  "Jwt": {
    "PrivateKey": "Certificates/node-a-private.key",
    "PublicKey": "Certificates/node-a-public.key"
  }
}
```

### Node Session Configuration

**appsettings.json**:
```json
{
  "RateLimiting": {
    "RequestsPerMinute": 60,
    "WindowSizeMinutes": 1,
    "Implementation": "Redis Sorted Sets"
  },
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true,
    "UsePostgreSqlForNodes": true
  },
  "Redis": {
    "ConnectionStrings": {
      "NodeA": "irn-redis-node-a:6379,password=prism-redis-password-node-a",
      "NodeB": "irn-redis-node-b:6380,password=prism-redis-password-node-b"
    }
  },
  "HttpClient": {
    "TimeoutSeconds": 300
  }
}
```

### Service Registration (DI Container)

**User Authentication Services**:
```csharp
// Scoped (per-request instance)
services.AddScoped<IUserAuthService, UserAuthService>();
services.AddScoped<IUserRepository, UserRepository>();
```

**Node Session Services**:
```csharp
// Singleton (shared state)
services.AddSingleton<ISessionService, SessionService>();
services.AddSingleton<ISessionStore>(sp => {
    var useRedis = configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions");
    if (useRedis) {
        var redis = sp.GetRequiredService<IRedisConnectionService>();
        return new RedisSessionStore(redis);
    }
    return new InMemorySessionStore();
});

services.AddSingleton<IChannelStore>(sp => {
    var useRedis = configuration.GetValue<bool>("FeatureFlags:UseRedisForChannels");
    if (useRedis) {
        var redis = sp.GetRequiredService<IRedisConnectionService>();
        return new RedisChannelStore(redis);
    }
    return new InMemoryChannelStore();
});
```

---

## API Reference

### User Authentication Endpoints

#### POST /api/userauth/login

**Description**: Authenticate user with username and password, receive JWT token.

**Request**:
```json
{
  "username": "researcher@institution.edu",
  "password": "Base64EncodedPassword",
  "researchId": "optional-guid"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-24T15:30:00Z"
}
```

**Errors**:
- 400 Bad Request: Missing username or password
- 401 Unauthorized: Invalid credentials
- 403 Forbidden: User does not have access to specified research project

#### POST /api/userauth/refreshtoken

**Description**: Refresh JWT token before expiration.

**Headers**:
```
Authorization: Bearer {current_jwt}
```

**Request**:
```json
{
  "researchId": "optional-guid"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-24T16:30:00Z"
}
```

**Errors**:
- 401 Unauthorized: Invalid or expired JWT
- 403 Forbidden: User does not have access to specified research project

#### POST /api/userauth/encrypt

**Description**: Utility endpoint to hash passwords (development only).

**Request**:
```json
{
  "password": "PlainTextPassword"
}
```

**Response** (200 OK):
```json
{
  "hash": "$SHA512$abcdef0123456789..."
}
```

### Node Session Endpoints

#### POST /api/session/whoami

**Description**: Get current session information.

**Headers**:
```
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
```

**Request** (encrypted):
```json
{
  "Timestamp": "2025-10-24T10:05:00Z"
}
```

**Response** (200 OK, encrypted):
```json
{
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "nodeId": "node-registration-guid",
  "channelId": "channel-12345678",
  "createdAt": "2025-10-24T10:00:00Z",
  "expiresAt": "2025-10-24T11:00:00Z",
  "lastAccessedAt": "2025-10-24T10:05:00Z",
  "capabilities": ["ReadOnly", "ReadWrite"],
  "requestCount": 5
}
```

**Errors**:
- 400 Bad Request: Missing X-Channel-Id header or invalid encrypted payload
- 401 Unauthorized: Invalid or expired session token
- 404 Not Found: Channel not found
- 410 Gone: Channel expired
- 429 Too Many Requests: Rate limit exceeded (60 req/min)

#### POST /api/session/renew

**Description**: Extend session expiration by 1 hour.

**Headers**:
```
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
```

**Request** (encrypted):
```json
{
  "Timestamp": "2025-10-24T10:50:00Z"
}
```

**Response** (200 OK, encrypted):
```json
{
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "expiresAt": "2025-10-24T11:50:00Z",
  "message": "Session renewed successfully"
}
```

**Errors**:
- Same as `/api/session/whoami`

#### POST /api/session/revoke

**Description**: Revoke (logout) current session.

**Headers**:
```
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
```

**Request** (encrypted):
```json
{
  "Timestamp": "2025-10-24T11:00:00Z"
}
```

**Response** (200 OK, encrypted):
```json
{
  "message": "Session revoked successfully",
  "sessionToken": "session-87654321-4321-4321-4321-876543210987",
  "revokedAt": "2025-10-24T11:00:00Z"
}
```

**Errors**:
- Same as `/api/session/whoami`

#### POST /api/session/metrics

**Description**: Get session statistics (admin only).

**Headers**:
```
X-Channel-Id: channel-12345678
X-Session-Id: session-87654321-4321-4321-4321-876543210987
```

**Request** (encrypted):
```json
{
  "Timestamp": "2025-10-24T11:00:00Z"
}
```

**Response** (200 OK, encrypted):
```json
{
  "totalSessions": 42,
  "activeSessions": 15,
  "expiredSessions": 27,
  "totalRequests": 1337,
  "averageRequestsPerSession": 31.8,
  "sessionsByCapability": {
    "ReadOnly": 10,
    "ReadWrite": 20,
    "Admin": 12
  }
}
```

**Errors**:
- 403 Forbidden: Requires Admin capability
- Same as `/api/session/whoami`

---

## Common Patterns

### Pattern 1: User Login Flow (Mobile App)

```typescript
// 1. User submits credentials
const loginResponse = await fetch('https://node-a.prism.local/api/userauth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'researcher@institution.edu',
    password: btoa('MySecurePassword123'),  // Base64 encode
    researchId: 'optional-research-guid'
  })
});

const { token, expiresAt } = await loginResponse.json();

// 2. Store token locally
localStorage.setItem('prism_jwt', token);
localStorage.setItem('prism_jwt_expires', expiresAt);

// 3. Use token for subsequent requests
const researchResponse = await fetch('https://node-a.prism.local/api/research/12345678', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const researchData = await researchResponse.json();
```

### Pattern 2: Node Session Flow (Federated Query)

```typescript
// Prerequisites: Phases 1-3 completed, have channelId and sessionToken

// 1. Prepare request DTO
const request = {
  Timestamp: new Date().toISOString()
};

// 2. Encrypt request
const encryptedPayload = await encryptWithChannel(channelId, request);

// 3. Send request with headers
const response = await fetch('https://node-b.prism.local/api/session/whoami', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-Channel-Id': channelId,
    'X-Session-Id': sessionToken
  },
  body: JSON.stringify({ encryptedData: encryptedPayload })
});

// 4. Decrypt response
const encryptedResponse = await response.json();
const decryptedResponse = await decryptWithChannel(channelId, encryptedResponse.encryptedData);

console.log('Session info:', decryptedResponse);
```

### Pattern 3: Token Refresh (Proactive)

```typescript
// Check expiration before each request
function isTokenExpiringSoon(expiresAt: string): boolean {
  const expiresDate = new Date(expiresAt);
  const now = new Date();
  const minutesRemaining = (expiresDate.getTime() - now.getTime()) / 1000 / 60;
  return minutesRemaining < 5;  // Refresh if < 5 minutes remaining
}

async function ensureValidToken(): Promise<string> {
  const token = localStorage.getItem('prism_jwt');
  const expiresAt = localStorage.getItem('prism_jwt_expires');

  if (!token || !expiresAt) {
    throw new Error('Not authenticated');
  }

  if (isTokenExpiringSoon(expiresAt)) {
    // Refresh token
    const refreshResponse = await fetch('https://node-a.prism.local/api/userauth/refreshtoken', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({})
    });

    const { token: newToken, expiresAt: newExpiresAt } = await refreshResponse.json();
    localStorage.setItem('prism_jwt', newToken);
    localStorage.setItem('prism_jwt_expires', newExpiresAt);
    return newToken;
  }

  return token;
}

// Use before each authenticated request
const token = await ensureValidToken();
```

### Pattern 4: Session Renewal (Node)

```typescript
// Renew session before expiration
async function renewSessionIfNeeded(channelId: string, sessionToken: string, expiresAt: string): Promise<string> {
  const expiresDate = new Date(expiresAt);
  const now = new Date();
  const minutesRemaining = (expiresDate.getTime() - now.getTime()) / 1000 / 60;

  if (minutesRemaining < 10) {  // Renew if < 10 minutes remaining
    const request = {
      Timestamp: new Date().toISOString()
    };

    const encryptedPayload = await encryptWithChannel(channelId, request);

    const response = await fetch('https://node-b.prism.local/api/session/renew', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Channel-Id': channelId,
        'X-Session-Id': sessionToken
      },
      body: JSON.stringify({ encryptedData: encryptedPayload })
    });

    const encryptedResponse = await response.json();
    const { expiresAt: newExpiresAt } = await decryptWithChannel(channelId, encryptedResponse.encryptedData);

    return newExpiresAt;
  }

  return expiresAt;
}
```

### Pattern 5: Graceful Session Revocation

```typescript
// Logout and cleanup
async function logout(channelId: string, sessionToken: string): Promise<void> {
  try {
    const request = {
      Timestamp: new Date().toISOString()
    };

    const encryptedPayload = await encryptWithChannel(channelId, request);

    await fetch('https://node-b.prism.local/api/session/revoke', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Channel-Id': channelId,
        'X-Session-Id': sessionToken
      },
      body: JSON.stringify({ encryptedData: encryptedPayload })
    });
  } catch (error) {
    console.error('Failed to revoke session:', error);
    // Proceed with local cleanup anyway
  } finally {
    // Clean up local state
    localStorage.removeItem('channelId');
    localStorage.removeItem('sessionToken');
    localStorage.removeItem('sessionExpiresAt');
  }
}
```

---

## Troubleshooting

### User Authentication Issues

#### Issue: "401 Unauthorized" on login

**Causes**:
- Incorrect username or password
- Password not Base64-encoded
- User account disabled or deleted

**Solutions**:
```bash
# 1. Verify user exists in database
docker exec -it irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry
SELECT "Id", "Login", "Role", "CreatedAt" FROM "Users" WHERE "Login" = 'researcher@institution.edu';

# 2. Generate correct password hash
curl -X POST http://localhost:5000/api/userauth/encrypt \
  -H "Content-Type: application/json" \
  -d '{"password": "MySecurePassword123"}'

# 3. Update user password in database
UPDATE "Users" SET "PasswordHash" = '$SHA512$abcdef...' WHERE "Login" = 'researcher@institution.edu';

# 4. Verify master password override (dev only)
grep "BiotecaAuth:Password:Master" appsettings.json
```

#### Issue: "403 Forbidden" - User cannot access research project

**Cause**: User's associated researcher does not have access to the requested research project.

**Solution**:
```sql
-- 1. Find user's researcher
SELECT r."Id", r."Name", r."Email"
FROM "Researchers" r
JOIN "Users" u ON u."ResearcherId" = r."Id"
WHERE u."Login" = 'researcher@institution.edu';

-- 2. Grant researcher access to research project
INSERT INTO "ResearcherResearches" ("ResearcherId", "ResearchId")
VALUES ('researcher-guid', 'research-guid');
```

#### Issue: JWT signature validation fails

**Causes**:
- Public key mismatch
- Token generated by different node
- Key rotation without updating clients

**Solutions**:
```bash
# 1. Verify public key matches private key
openssl rsa -in Certificates/node-a-private.key -pubout | sha256sum
openssl pkey -in Certificates/node-a-public.key -pubin -outform DER | sha256sum

# 2. Check JWT configuration
grep -A 5 "Jwt" appsettings.json

# 3. Decode JWT to inspect claims (use jwt.io)
echo "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..." | base64 -d
```

### Node Session Issues

#### Issue: "404 Not Found" - Channel not found

**Causes**:
- Channel expired (2-hour TTL)
- Redis connection failure
- Wrong X-Channel-Id header

**Solutions**:
```bash
# 1. Check Redis connection
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a PING

# 2. List active channels
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "channel:*"

# 3. Inspect channel details
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "channel:channel-12345678"

# 4. Check TTL
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "channel:channel-12345678"

# 5. If channel expired, restart handshake from Phase 1
```

#### Issue: "401 Unauthorized" - Session not found

**Causes**:
- Session expired (1-hour TTL)
- Session revoked
- Redis data loss
- Wrong X-Session-Id header

**Solutions**:
```bash
# 1. List active sessions
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "session:*"

# 2. Inspect session details
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "session:session-87654321"

# 3. Check TTL
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "session:session-87654321"

# 4. If session expired, re-authenticate (Phase 3)
```

#### Issue: "429 Too Many Requests" - Rate limit exceeded

**Cause**: More than 60 requests in 1-minute window.

**Solutions**:
```bash
# 1. Check current rate limit state
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a ZCARD "rate-limit:session:session-87654321"

# 2. View request timestamps
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a ZRANGE "rate-limit:session:session-87654321" 0 -1 WITHSCORES

# 3. Clear rate limit (emergency only)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a DEL "rate-limit:session:session-87654321"

# 4. Implement exponential backoff in client
```

#### Issue: "400 Bad Request" - Decryption failure

**Causes**:
- Wrong symmetric key (channel mismatch)
- Malformed encrypted payload
- Nonce reuse (GCM authentication failure)
- Corrupted ciphertext

**Solutions**:
```bash
# 1. Verify channel symmetric key
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "channel:channel-12345678"

# 2. Check payload structure (should have nonce + ciphertext)
# Valid encrypted payload format:
# {
#   "encryptedData": "Base64String",
#   "nonce": "Base64String"  # 12 bytes
# }

# 3. Re-establish channel (Phase 1)
```

#### Issue: "403 Forbidden" - Insufficient capabilities

**Cause**: Session does not have required capability (e.g., Admin).

**Solutions**:
```bash
# 1. Check session capabilities
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "session:session-87654321" | jq '.AccessLevel'

# 2. Check endpoint capability requirement
grep -r "RequiredCapability" Controllers/

# 3. Grant higher access level to node
# In PostgreSQL:
UPDATE "NodeRegistrations" SET "AccessLevel" = 2 WHERE "Id" = 'node-registration-guid';

# 4. Re-authenticate to get new session with updated capabilities (Phase 3)
```

### Middleware Debugging

#### Enable detailed logging

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Bioteca.Prism.Core.Middleware": "Trace",
      "Bioteca.Prism.InteroperableResearchNode.Middleware": "Trace"
    }
  }
}
```

#### View middleware execution order

```bash
# Watch application logs
docker logs -f irn-node-a | grep -E "(PrismEncryptedChannelConnection|PrismAuthenticatedSession)"

# Expected log sequence:
# [PrismEncryptedChannelConnection] Validating X-Channel-Id header
# [PrismEncryptedChannelConnection] Channel found: channel-12345678
# [PrismEncryptedChannelConnection] Decrypting payload
# [PrismEncryptedChannelConnection] Request decrypted successfully
# [PrismAuthenticatedSession] Extracting session token
# [PrismAuthenticatedSession] Session found: session-87654321
# [PrismAuthenticatedSession] Validating expiration
# [PrismAuthenticatedSession] Checking capabilities
# [PrismAuthenticatedSession] Applying rate limiting
# [PrismAuthenticatedSession] Session validated successfully
# [Controller] Executing action: WhoAmI
```

#### Inspect HttpContext.Items

```csharp
// In controller action
var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
var decryptedRequest = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;

Console.WriteLine($"Channel: {channelContext?.ChannelId}");
Console.WriteLine($"Session: {sessionContext?.SessionToken}");
Console.WriteLine($"Capabilities: {string.Join(", ", sessionContext?.Capabilities ?? [])}");
```

---

## Related Documentation

- **4-Phase Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Phase 4 Session Management**: `docs/workflows/PHASE4_SESSION_FLOW.md`
- **User Authentication API**: `docs/api/user-authentication.md`
- **Security Overview**: `docs/SECURITY_OVERVIEW.md`
- **Manual Testing Guide**: `docs/testing/manual-testing-guide.md`
- **Redis Testing Guide**: `docs/testing/redis-testing-guide.md`

---

**Document Version**: 1.0.0
**Last Reviewed**: 2025-10-24
**Reviewers**: Claude Code
**Next Review**: 2025-11-24
