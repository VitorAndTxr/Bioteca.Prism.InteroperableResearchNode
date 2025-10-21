# PRISM Security Overview

**Document Version**: 1.0
**Last Updated**: October 2025
**Security Classification**: Public Technical Documentation

---

## Security Model

The PRISM framework implements a **defense-in-depth** security strategy with multiple layers of protection:

```
Layer 1: Transport Security (TLS 1.3, Bluetooth Encryption)
    ↓
Layer 2: Channel Security (ECDH + AES-256-GCM + PFS)
    ↓
Layer 3: Node Authentication (X.509 + RSA-2048 Signatures)
    ↓
Layer 4: Session Management (Bearer Tokens + Rate Limiting)
    ↓
Layer 5: Data Validation (Input Sanitization + Integrity Checks)
```

---

## Phase 1: Encrypted Channel Establishment

### Purpose
Establish a secure, authenticated communication channel between research nodes before exchanging sensitive information.

### Cryptographic Algorithms

**Ephemeral Key Exchange**: ECDH (Elliptic Curve Diffie-Hellman)
- **Curve**: NIST P-384 (secp384r1)
- **Key Size**: 384 bits
- **Security Level**: ~192-bit symmetric equivalent
- **Rationale**: Resistant to quantum attacks (larger margin than P-256)

**Key Derivation**: HKDF-SHA256
- **Hash Function**: SHA-256
- **Salt**: None (not required for ephemeral keys)
- **Info String**: "PRISM-Channel-Key-v1"
- **Output**: 256-bit symmetric key

**Symmetric Encryption**: AES-256-GCM
- **Mode**: Galois/Counter Mode (authenticated encryption)
- **Key Size**: 256 bits
- **IV Size**: 96 bits (12 bytes, randomly generated per message)
- **Tag Size**: 128 bits (16 bytes, for authentication)
- **Rationale**: Provides both confidentiality and integrity

### Protocol Flow

```
Client (Node A)                     Server (Node B)
─────────────────                   ─────────────────

1. Generate ephemeral key pair (ECDH P-384)
   Private Key: 384 bits
   Public Key: ECPoint (X, Y)

2. POST /api/channel/open
   Body: { publicKey: "base64-encoded-public-key" }
                                →
                                    3. Receive client public key
                                    4. Generate server ephemeral key pair
                                    5. Compute shared secret (ECDH)
                                    6. Derive symmetric key (HKDF-SHA256)
                                    7. Store channel context (in-memory/Redis)

                                ←   Response:
                                    Headers: X-Channel-Id: "uuid"
                                    Body: { publicKey: "server-public-key" }

8. Receive server public key
9. Compute shared secret (ECDH)
10. Derive symmetric key (HKDF-SHA256)
11. Store channel context

All subsequent communication encrypted with AES-256-GCM
```

### Perfect Forward Secrecy (PFS)

**Definition**: Compromise of long-term keys (certificates) does NOT compromise past session keys.

**Implementation**:
- Ephemeral ECDH keys generated fresh for each channel
- Private keys discarded after channel closure
- Symmetric keys derived uniquely per channel
- No reuse of key material across sessions

**Result**: Even if an attacker obtains the node's certificate private key, they CANNOT decrypt past communications.

### Storage and Lifecycle

**In-Memory Storage** (Default):
- `ConcurrentDictionary<string, ChannelContext>`
- Automatic cleanup after TTL (30 minutes)
- Lost on node restart (channels must be re-established)

**Redis Storage** (Production):
- Key Pattern: `channel:{uuid}`
- Automatic TTL: 1800 seconds (30 minutes)
- Persistence across node restarts
- Distributed access for load-balanced nodes

---

## Phase 2: Node Identification

### Purpose
Identify the remote node using X.509 certificates and establish its authorization status before granting access.

### Cryptographic Algorithms

**Certificate Standard**: X.509 v3
- **Public Key Algorithm**: RSA-2048
- **Signature Algorithm**: SHA-256 with RSA Encryption (sha256RSA)
- **Validity Period**: 1 year (development), longer for production
- **Subject Name**: CN={NodeId}, O=Research Institution, C=BR

**Certificate Fingerprint**: SHA-256
- **Input**: Complete DER-encoded certificate bytes
- **Output**: 256-bit hash (32 bytes, hex-encoded = 64 characters)
- **Purpose**: Natural key for database lookups (unique constraint)

**Digital Signature**: RSA-2048
- **Padding**: PKCS#1 v1.5 (RSA_PKCS1_PADDING)
- **Hash Function**: SHA-256
- **Signature Size**: 256 bytes (2048 bits)

### Protocol Flow

```
Client (Node A)                     Server (Node B)
─────────────────                   ─────────────────

1. Prepare identification request:
   Data = {
     nodeId: "node-a",
     nodeName: "Hospital Research Node A",
     certificate: "base64-encoded-cert",
     subjectName: "CN=node-a, O=Hospital",
     timestamp: "2025-10-21T10:30:00Z",
     nonce: "random-base64-string"
   }

2. Sign data with private key (RSA-2048)
   Signature = RSA_Sign(SHA256(JSON.stringify(data)), privateKey)

3. Encrypt request with channel key (AES-256-GCM)
   Encrypted = {
     encryptedData: "base64-ciphertext",
     iv: "base64-iv",
     authTag: "base64-tag"
   }

4. POST /api/channel/identify
   Headers: X-Channel-Id: "channel-uuid"
   Body: Encrypted payload
                                →
                                    5. Decrypt request with channel key
                                    6. Validate timestamp (±5 minutes tolerance)
                                    7. Validate nonce (base64, min 12 bytes)
                                    8. Parse X.509 certificate
                                    9. Verify certificate validity (not expired)
                                    10. Calculate certificate fingerprint (SHA-256)
                                    11. Verify RSA signature
                                    12. Look up node by fingerprint in database

                                    If NOT FOUND:
                                        - Create new node record (status: Unknown)
                                        - Return registration URL

                                    If FOUND:
                                        - Check authorization status
                                        - Store IdentifiedNodeId in ChannelContext

                                ←   Response (encrypted):
                                    {
                                      isKnown: true/false,
                                      registrationId: "guid",
                                      status: "Pending" | "Authorized" | "Revoked",
                                      nextPhase: "phase3_authenticate" | null
                                    }
```

### Node Registry and Approval Workflow

**Authorization States**:
1. **Unknown**: Node identified but not yet registered (auto-created)
2. **Pending**: Registration request submitted, awaiting approval
3. **Authorized**: Approved for data exchange (can proceed to Phase 3)
4. **Revoked**: Access revoked (cannot authenticate)

**Administrative Operations**:
- `POST /api/node/register` - Submit registration request (Pending)
- `PUT /api/node/{id:guid}/status` - Update authorization status (admin only)
- `GET /api/node/{id:guid}` - Get node details (admin only)

**Re-Registration**:
- If certificate fingerprint matches existing node → Update node record
- If fingerprint differs → Create new node entry
- Rationale: Certificate is the source of truth, not NodeId (which can change)

### Validation Rules

**Certificate Validation**:
- ✅ Base64 decodable
- ✅ Valid X.509 structure
- ✅ Not expired (NotBefore ≤ Now ≤ NotAfter)
- ✅ Contains SubjectName
- ✅ RSA public key present

**Timestamp Validation**:
- ✅ ISO 8601 format
- ✅ Within ±5 minutes of server time
- ✅ Purpose: Prevent replay attacks

**Nonce Validation**:
- ✅ Base64 format
- ✅ Minimum 12 bytes (16 characters base64)
- ✅ Purpose: Ensure request uniqueness

**Required Fields**:
- ✅ NodeId (string, non-empty)
- ✅ NodeName (string, non-empty)
- ✅ SubjectName (string, non-empty)
- ✅ Certificate (base64, valid X.509)

---

## Phase 3: Mutual Authentication

### Purpose
Prove possession of the private key corresponding to the certificate via challenge-response protocol.

### Cryptographic Algorithms

**Challenge Generation**:
- **Algorithm**: Cryptographically secure random number generator (RNGCryptoServiceProvider)
- **Size**: 32 bytes (256 bits)
- **Encoding**: Base64 for transmission
- **TTL**: 5 minutes (300 seconds)
- **Storage**: In-memory dictionary or Redis with automatic expiration

**Signature Verification**:
- **Algorithm**: RSA-2048 with SHA-256
- **Padding**: PKCS#1 v1.5
- **Data Format**: `{challengeData}{channelId}{nodeId}{timestamp:O}`
- **Verification**: Extract public key from certificate, verify signature

### Protocol Flow

```
Client (Node A)                     Server (Node B)
─────────────────                   ─────────────────

1. Request Challenge
   POST /api/node/challenge
   Body (encrypted): {
     channelId: "channel-uuid",
     nodeId: "node-a",
     timestamp: "2025-10-21T10:30:00Z"
   }
                                →
                                    2. Validate channel and node ID
                                    3. Generate 32-byte random challenge
                                    4. Store challenge with 5-minute TTL
                                    5. Calculate expiration time

                                ←   Response (encrypted):
                                    {
                                      challengeData: "base64-challenge",
                                      expiresAt: "2025-10-21T10:35:00Z",
                                      ttlSeconds: 300
                                    }

6. Receive challenge
7. Construct signing data:
   Data = challengeData + channelId + nodeId + timestamp (ISO 8601 round-trip format)
8. Sign with private key:
   Signature = RSA_Sign(SHA256(Data), privateKey)

9. Authenticate
   POST /api/node/authenticate
   Body (encrypted): {
     channelId: "channel-uuid",
     nodeId: "node-a",
     challengeData: "base64-challenge",
     signature: "base64-signature",
     timestamp: "2025-10-21T10:30:15Z"
   }
                                →
                                    10. Decrypt request
                                    11. Validate challenge exists (not expired)
                                    12. Validate challenge data matches
                                    13. Validate node is Authorized
                                    14. Reconstruct signing data (same format)
                                    15. Extract public key from certificate
                                    16. Verify RSA signature
                                    17. Invalidate challenge (one-time use)
                                    18. Create session token (Phase 4)

                                ←   Response (encrypted):
                                    {
                                      authenticated: true,
                                      sessionToken: "guid",
                                      sessionExpiresAt: "2025-10-21T11:30:15Z",
                                      grantedCapabilities: ["query:read", "data:write"],
                                      nextPhase: "phase4_session"
                                    }
```

### Replay Attack Protection

**One-Time Challenges**:
- Each challenge valid for single authentication attempt
- Challenge deleted after successful authentication
- Challenge deleted after expiration (5 minutes)

**Timestamp Validation**:
- Authenticate request timestamp must be within ±5 minutes
- Prevents replay of old authentication requests

**Challenge Uniqueness**:
- 32 bytes = 2^256 possible challenges
- Collision probability negligible
- Cryptographically secure RNG ensures randomness

---

## Phase 4: Session Management

### Purpose
Manage authenticated sessions with capability-based authorization and rate limiting.

### Session Token Generation

**Algorithm**: GUID (Globally Unique Identifier)
- **Version**: UUID v4 (random)
- **Size**: 128 bits (16 bytes)
- **Format**: `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`
- **Uniqueness**: 2^122 possible values (version/variant bits fixed)

### Session Lifecycle

**Creation** (during Phase 3 authentication):
1. Generate GUID session token
2. Store session context:
   - NodeId (string)
   - RegistrationId (Guid)
   - ChannelId (string)
   - Granted capabilities (NodeAccessTypeEnum)
   - Creation timestamp
   - Expiration timestamp (1 hour)
3. Return session token in encrypted authentication response

**Validation** (on every Phase 4 request):
1. Extract session token from **decrypted payload** (not HTTP header)
2. Look up session context (in-memory or Redis)
3. Check session exists
4. Check session not expired
5. Check required capability (if specified)
6. Enforce rate limiting (60 requests/minute)
7. Inject SessionContext into HttpContext.Items

**Renewal**:
- `POST /api/session/renew` (encrypted payload with session token)
- Extends expiration by 1 hour from current time
- Returns new expiration timestamp

**Revocation**:
- `POST /api/session/revoke` (encrypted payload with session token)
- Deletes session context immediately
- Returns confirmation

### Capability-Based Authorization

**Access Levels** (NodeAccessTypeEnum):
```
ReadOnly (Level 1):
├─ query:read - Execute federated queries
└─ Restrictions: Cannot modify data, no admin operations

ReadWrite (Level 2):
├─ Inherits: ReadOnly capabilities
├─ data:write - Submit research data
├─ data:update - Update owned data
└─ Restrictions: No admin operations

Admin (Level 3):
├─ Inherits: ReadWrite capabilities
├─ admin:node - Manage node settings
├─ admin:users - Manage user access
└─ session:metrics - View system metrics
```

**Hierarchy Enforcement**:
- Admin > ReadWrite > ReadOnly
- Lower levels cannot access higher-level capabilities
- Enforced via `PrismAuthenticatedSessionAttribute` filter

### Rate Limiting

**Algorithm**: Token Bucket (via Redis Sorted Sets)
- **Capacity**: 60 requests
- **Refill Rate**: 60 requests per minute (1 request/second)
- **Window**: Rolling 60-second window
- **Storage**: Redis Sorted Set with request timestamps
- **Key Pattern**: `session:ratelimit:{sessionToken}`

**Implementation**:
1. Current time = `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`
2. Remove expired timestamps (older than 60 seconds)
3. Count remaining timestamps in set
4. If count >= 60 → Reject with 429 Too Many Requests
5. If count < 60 → Add current timestamp to set, allow request

**Benefits**:
- Smooth rate limiting (not bursty)
- Distributed enforcement (Redis shared state)
- Automatic cleanup (expired timestamps removed)

### Storage and Persistence

**Session Context**:
```csharp
public class SessionContext
{
    public string SessionToken { get; set; }         // GUID
    public string NodeId { get; set; }               // Protocol-level ID
    public Guid RegistrationId { get; set; }         // Database PK
    public string ChannelId { get; set; }            // Channel UUID
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }  // Capability
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**Redis Storage**:
- Key Pattern: `session:{sessionToken}`
- Value: JSON-serialized SessionContext
- TTL: 3600 seconds (1 hour), auto-renewed on renewal

**In-Memory Storage** (Fallback):
- `ConcurrentDictionary<string, SessionContext>`
- Manual expiration check on access
- Lost on node restart

---

## Encrypted Payload Format (Phases 2-4)

All requests after Phase 1 use encrypted payloads:

```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv (12 bytes)",
  "authTag": "base64-encoded-authentication-tag (16 bytes)"
}
```

**Encryption Process**:
1. Serialize request object to JSON
2. Generate random 96-bit IV
3. Encrypt JSON with AES-256-GCM using channel symmetric key
4. Base64-encode ciphertext, IV, and auth tag
5. Send as HTTP request body

**Decryption Process** (`PrismEncryptedChannelConnectionAttribute`):
1. Validate X-Channel-Id header
2. Retrieve ChannelContext from store
3. Read encrypted request body
4. Base64-decode ciphertext, IV, and auth tag
5. Decrypt with AES-256-GCM using channel symmetric key
6. Deserialize JSON to request DTO
7. Inject into HttpContext.Items["DecryptedRequest"]

---

## Security Requirements and Compliance

### LGPD (Lei Geral de Proteção de Dados) - Brazil
- **Data Minimization**: Only collect necessary data
- **Consent Management**: Explicit, informed consent
- **Right to Access**: Volunteers can request their data
- **Right to Deletion**: Data deletion on withdrawal
- **Data Breach Notification**: Within 72 hours

### GDPR (General Data Protection Regulation) - EU
- **Lawful Basis**: Research purposes with consent
- **Data Portability**: Export data in machine-readable format
- **Privacy by Design**: Built-in security from architecture
- **Data Protection Officer**: Designated for large-scale processing

### HIPAA (Health Insurance Portability and Accountability Act) - US (Planned)
- **PHI Protection**: Encrypt protected health information
- **Audit Controls**: Log all PHI access
- **Access Controls**: Role-based access
- **Transmission Security**: TLS 1.3 + encrypted channels

---

## Known Vulnerabilities and Mitigations

### Vulnerability: Timing Attacks on Signature Verification
**Risk**: Attacker measures response times to infer signature validity
**Mitigation**: Use constant-time signature verification (planned)
**Current Status**: Standard .NET RSA verification (not constant-time)

### Vulnerability: Certificate Expiration
**Risk**: Nodes lose access when certificates expire
**Mitigation**: Automated certificate renewal (planned with Let's Encrypt)
**Current Status**: Manual renewal required

### Vulnerability: In-Memory Storage Loss
**Risk**: Node restart loses all active sessions and channels
**Mitigation**: Redis persistence for sessions and channels
**Current Status**: Feature flags enable Redis (UseRedisForSessions, UseRedisForChannels)

### Vulnerability: Rate Limit Bypass (In-Memory)
**Risk**: Distributed nodes have separate rate limit counters
**Mitigation**: Redis-based rate limiting with shared state
**Current Status**: Redis implementation available

---

## Security Testing

### Automated Tests (73/75 passing)
- ✅ Phase 1: Encrypted channel establishment
- ✅ Phase 2: Node identification and registration
- ✅ Phase 3: Challenge-response authentication
- ✅ Phase 4: Session management and rate limiting
- ⚠️ 2 failing tests: RSA signature verification (known issue, non-blocking)

### Manual Penetration Testing (Planned)
- [ ] Replay attack attempts
- [ ] Man-in-the-middle attack simulations
- [ ] Certificate spoofing attempts
- [ ] Rate limit bypass attempts
- [ ] Session fixation attempts

### Security Audit (Planned)
- [ ] Third-party cryptographic review
- [ ] LGPD/GDPR compliance audit
- [ ] Vulnerability scanning (OWASP Top 10)

---

## Documentation References

For detailed implementation, see:

- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Phase 4 Sessions**: `docs/architecture/phase4-session-management.md`
- **Persistence**: `docs/development/PERSISTENCE_LAYER.md`
- **Workflows**: `docs/workflows/` (Phase 1-4 flows)
- **Testing Guide**: `docs/testing/manual-testing-guide.md`
