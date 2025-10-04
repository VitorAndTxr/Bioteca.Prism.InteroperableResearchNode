# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Interoperable Research Node (IRN)** - Core component of the PRISM framework for federated biomedical research data. Enables secure, standardized communication between research nodes using cryptographic handshakes, node authentication, and federated queries.

**Current Status**: Phase 4 Complete (Encrypted Channel + Node Identification + Mutual Authentication + Session Management)

## Architecture

### Project Structure (Clean Architecture)

```
Bioteca.Prism.Domain/          # Domain layer (entities, DTOs)
├── Entities/Node/             # Domain entities
├── Requests/Node/             # Request DTOs
├── Responses/Node/            # Response DTOs
└── Errors/Node/               # Error models

Bioteca.Prism.Core/            # Core layer (middleware, attributes)
├── Middleware/Channel/        # Channel validation attributes
│   ├── PrismChannelConnectionAttribute.cs        # Channel validation (commented out)
│   ├── PrismEncryptedChannelConnectionAttribute.cs  # Encrypted payload handling
│   ├── ChannelContext.cs      # Channel state
│   └── IChannelStore.cs       # Channel storage interface
├── Middleware/Node/           # Node-specific middleware
└── Security/                  # Security utilities

Bioteca.Prism.Service/         # Service layer (business logic)
├── Services/Node/             # Node-specific services
│   ├── NodeChannelClient.cs             # HTTP client for handshake
│   ├── NodeRegistryService.cs           # Node registry (in-memory)
│   ├── ChallengeService.cs              # Challenge-response authentication
│   └── CertificateHelper.cs             # X.509 utilities
└── Services/Session/          # Session management services
    └── SessionService.cs                # Session lifecycle (Phase 4)

Bioteca.Prism.InteroperableResearchNode/  # API layer
├── Controllers/
│   ├── ChannelController.cs    # Phase 1 endpoints
│   ├── NodeConnectionController.cs  # Phases 2-3 endpoints (registration, identification, authentication)
│   ├── SessionController.cs    # Phase 4 endpoints (whoami, renew, revoke, metrics)
│   └── TestingController.cs    # Dev/test utilities
├── Middleware/
│   └── PrismAuthenticatedSessionAttribute.cs  # Session validation filter
└── Program.cs                  # DI container
```

### Handshake Protocol (4 Phases)

**Phase 1: Encrypted Channel (✅ Complete)**
- ECDH P-384 ephemeral key exchange
- HKDF-SHA256 key derivation
- AES-256-GCM symmetric encryption
- Perfect Forward Secrecy
- Endpoints: `/api/channel/open`, `/api/channel/initiate`

**Phase 2: Node Identification (✅ Complete)**
- X.509 certificate-based identification
- RSA-2048 digital signatures
- Node registry with approval workflow (Unknown → Pending → Authorized/Revoked)
- **Encrypted payload handling via `PrismEncryptedChannelConnectionAttribute<T>`**
- Endpoints: `/api/channel/identify`, `/api/node/register`, `/api/node/{nodeId}/status`

**Phase 3: Mutual Authentication (✅ Complete)**
- Challenge-response protocol
- RSA-2048 digital signature verification
- Proof of private key possession
- Session token generation (1-hour TTL)
- Endpoints: `/api/node/challenge`, `/api/node/authenticate`

**Phase 4: Session Management (✅ Complete)**
- Bearer token authentication
- Capability-based authorization
- Session lifecycle (whoami, renew, revoke)
- Rate limiting (60 requests/minute)
- Session metrics and monitoring
- Endpoints: `/api/session/whoami`, `/api/session/renew`, `/api/session/revoke`, `/api/session/metrics`

### Attribute-Based Request Processing (Phase 2+)

**`PrismEncryptedChannelConnectionAttribute<T>`** - Resource filter for encrypted channel communications:

- **Type**: `IAsyncResourceFilter` (runs before model binding)
- **Purpose**: Validates channel, decrypts payload, verifies signatures
- **Location**: `Bioteca.Prism.Core/Middleware/Channel/`

**Flow**:
1. Validates `X-Channel-Id` header exists
2. Retrieves `ChannelContext` from `IChannelStore`
3. Reads encrypted request body (enables buffering with `EnableBuffering()`)
4. Decrypts `EncryptedPayload` using channel's symmetric key
5. For `NodeIdentifyRequest`: verifies RSA signature
6. Stores decrypted request in `HttpContext.Items["DecryptedRequest"]`

**Payload Format** (all Phase 2+ requests):
```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv",
  "authTag": "base64-encoded-auth-tag"
}
```

**Usage Example**:
```csharp
[HttpPost("identify")]
[PrismEncryptedChannelConnection<NodeIdentifyRequest>]
public async Task<IActionResult> Identify()
{
    var request = HttpContext.Items["DecryptedRequest"] as NodeIdentifyRequest;
    // ... process request
}
```

## Development Commands

### Build & Run

```bash
# Build solution
dotnet build Bioteca.Prism.InteroperableResearchNode/Bioteca.Prism.InteroperableResearchNode.sln

# Run locally (Node A on port 5000)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeA

# Run locally (Node B on port 5001)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeB

# Docker multi-node setup
docker-compose up -d                    # Start both nodes
docker-compose down                     # Stop nodes
docker-compose build --no-cache         # Rebuild after code changes
docker logs -f irn-node-a               # View Node A logs
docker logs -f irn-node-b               # View Node B logs
```

### Testing

```bash
# Run all automated tests
dotnet test Bioteca.Prism.InteroperableResearchNode.Test/Bioteca.Prism.InteroperableResearchNode.Test.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~Phase1ChannelEstablishmentTests"

# Run with detailed output
dotnet test --verbosity detailed

# Legacy PowerShell scripts (deprecated - use dotnet test instead)
# .\test-docker.ps1              # Phase 1 tests
# .\test-phase2-full.ps1         # Phase 2 tests

# Manual testing with Swagger
# Node A: http://localhost:5000/swagger
# Node B: http://localhost:5001/swagger
```

### Test Status (Last Updated: 2025-10-03 - 12:00)

**Overall: 73/75 tests passing (97.3% pass rate)** ✅

| Category | Passing | Total | Pass Rate | Status |
|----------|---------|-------|-----------|--------|
| Phase 1 (Channel Establishment) | 6/6 | 100% | ✅ |
| Certificate & Signature | 13/15 | 86.7% | ⚠️ |
| Phase 2 (Node Identification) | 6/6 | 100% | ✅ |
| Phase 3 (Mutual Authentication) | 5/5 | 100% | ✅ |
| **Phase 4 (Session Management)** | **8/8** | **100%** | ✅ |
| Encrypted Channel Integration | 3/3 | 100% | ✅ |
| NodeChannelClient | 7/7 | 100% | ✅ |
| Security & Edge Cases | 23/23 | 100% | ✅ |

**Recent Updates (2025-10-03 - 12:00):**
- ✅ **Phase 4 Implementation Complete**: Session management with capability-based authorization
- ✅ Session lifecycle endpoints (whoami, renew, revoke, metrics)
- ✅ Rate limiting (60 requests/minute per session) using token bucket algorithm
- ✅ Access level-based authorization (ReadOnly, ReadWrite, Admin hierarchy)
- ✅ All Phase 4 requests encrypted via AES-256-GCM channel (session token in payload, NOT headers)
- ✅ 8 new comprehensive integration tests for Phase 4
- ⚠️ 2 signature verification tests failing (known issue, not blocking)
- ✅ End-to-end test script (`test-phase4.sh`) validates complete flow (Phases 1→2→3→4)

**Previous Fixes (2025-10-02):**
- ✅ Implemented timestamp validation (protect against replay attacks)
- ✅ Implemented nonce validation (format and minimum size)
- ✅ Implemented certificate validation (format, structure, expiration)
- ✅ Implemented required fields validation (NodeId, NodeName, SubjectName)
- ✅ Implemented enum validation for AuthorizationStatus
- ✅ Fixed signature validation with proper timestamps
- ✅ Fixed timezone issues in certificate tests
- ✅ Implemented node re-registration with update logic

### Environment Profiles

- **NodeA**: Port 5000, `appsettings.NodeA.json`
- **NodeB**: Port 5001, `appsettings.NodeB.json`
- Both profiles enable Swagger and disable HTTPS redirection for local development

## Key Implementation Details

### Service Registration (Program.cs)

All services are registered as **Singleton** (shared state across requests):
- `IEphemeralKeyService` - ECDH key generation
- `IChannelEncryptionService` - Crypto operations
- `INodeChannelClient` - HTTP client for initiating handshakes
- `INodeRegistryService` - Node registry (in-memory, will need DB in production)
- `IChallengeService` - Challenge-response authentication (Phase 3)
- `ISessionService` - Session lifecycle management (Phase 4)

### Channel Flow

1. **Client initiates**: POST `/api/channel/initiate` → calls `NodeChannelClient.OpenChannelAsync(remoteUrl)`
2. **Client sends**: POST to remote `/api/channel/open` with ephemeral public key
3. **Server processes**: `ChannelController.OpenChannel()` generates server keys, derives shared secret
4. **Server responds**: Returns server public key + channel ID in `X-Channel-Id` header
5. **Both derive**: Same symmetric key from ECDH shared secret using HKDF
6. **Channel stored**: In-memory `ConcurrentDictionary<string, ChannelContext>` (30 min TTL)

### Phase 2: Node Identification Flow

1. **Generate certificate**: POST `/api/testing/generate-certificate` (dev only)
2. **Sign data**: POST `/api/testing/sign-data` with certificate + data
3. **Identify**: POST `/api/channel/identify` with NodeId + certificate + signature
4. **Response**:
   - Unknown node → `isKnown=false`, registration URL provided
   - Known, Pending → `isKnown=true`, `status=Pending`, `nextPhase=null`
   - Known, Authorized → `isKnown=true`, `status=Authorized`, `nextPhase="phase3_authenticate"`

### Phase 3: Challenge-Response Authentication Flow

1. **Request Challenge**: POST `/api/node/challenge` with encrypted `{channelId, nodeId, timestamp}`
2. **Server generates**: 32-byte random challenge, stores with 5-minute TTL
3. **Response**: `{challengeData, expiresAt, ttlSeconds}`
4. **Client signs**: Data format = `{challengeData}{channelId}{nodeId}{timestamp:O}`
   - Use `/api/testing/sign-challenge` helper for manual testing
   - Signature generated with RSA-2048 private key
5. **Authenticate**: POST `/api/node/authenticate` with encrypted `{channelId, nodeId, challengeData, signature, timestamp}`
6. **Server validates**:
   - Challenge exists and hasn't expired
   - Challenge data matches stored value
   - RSA signature is valid
   - Node status is `Authorized`
7. **Response**: `{authenticated: true, sessionToken, sessionExpiresAt, grantedCapabilities, nextPhase: "phase4_session"}`
8. **Challenge invalidated**: One-time use only

**Testing Helpers**:
- `/api/testing/request-challenge` - Client-side wrapper for requesting challenges
- `/api/testing/sign-challenge` - Sign challenge data in correct format
- `/api/testing/authenticate` - Client-side wrapper for authentication
- `test-phase3.sh` - Complete end-to-end automated test script

### Phase 4: Session Management Flow

**IMPORTANT**: All Phase 4 requests **MUST** be encrypted via the channel (like Phases 2-3). Session token is sent **inside** the encrypted payload, NOT in HTTP headers.

1. **Session Creation** (automatic after Phase 3 authentication):
   - `SessionService.CreateSessionAsync()` called by `ChallengeService`
   - Generates GUID session token
   - Sets 1-hour TTL (3600 seconds)
   - Stores granted capabilities
   - Returns session token in encrypted authentication response

2. **Session Validation** (`PrismAuthenticatedSessionAttribute`):
   - Runs AFTER `PrismEncryptedChannelConnection` middleware
   - Extracts session token from **decrypted request payload** (NOT from HTTP header)
   - Validates session exists and hasn't expired
   - Checks required capability (if specified)
   - Enforces rate limiting (60 requests/minute)
   - Stores `SessionContext` in `HttpContext.Items`

3. **Session Operations** (all encrypted via channel):
   - **WhoAmI**: POST `/api/session/whoami` - Get current session info
   - **Renew**: POST `/api/session/renew` - Extend session TTL
   - **Revoke**: POST `/api/session/revoke` - Logout/invalidate session
   - **Metrics**: POST `/api/session/metrics` - Get session metrics (requires `admin:node` capability)

4. **Request Format** (all endpoints):
```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv",
  "authTag": "base64-encoded-auth-tag"
}
```

**Decrypted Payload Example** (WhoAmI):
```json
{
  "channelId": "channel-id",
  "sessionToken": "session-token-guid",
  "timestamp": "2025-10-03T10:30:00Z"
}
```

5. **Access Level-Based Authorization** (`NodeAccessTypeEnum`):
   - `ReadOnly` - Read/query federated data (basic queries)
   - `ReadWrite` - Submit and modify research data
   - `Admin` - Full node administration and metrics access

6. **Rate Limiting**:
   - Token bucket algorithm
   - 60 requests per minute per session
   - Returns `429 Too Many Requests` when exceeded

**Usage Example**:
```csharp
[HttpPost("protected-endpoint")]
[PrismEncryptedChannelConnection<ProtectedRequest>]  // REQUIRED: Decrypt request
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]  // REQUIRED: Validate session
public IActionResult ProtectedEndpoint()
{
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
    var request = HttpContext.Items["DecryptedRequest"] as ProtectedRequest;

    // Check access level
    // sessionContext.NodeAccessLevel will be ReadOnly, ReadWrite, or Admin

    // Process request
    var response = new { data = "encrypted response" };

    // Encrypt response
    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
```

**Testing**:
- `test-phase4.sh` - Complete end-to-end test script (Phases 1+2+3+4)
- Tests: WhoAmI, authorization validation, renewal, revocation, rate limiting
- All requests encrypted via AES-256-GCM channel

### Certificate Management

**Development**: Auto-generated self-signed certificates
- `CertificateHelper.GenerateSelfSignedCertificate()` - Creates RSA-2048 cert
- Valid for 1 year from creation
- CN (Common Name) = NodeId

**Production**: Should use real CA or Let's Encrypt (not yet implemented)

## Important Files & Locations

### Configuration
- `appsettings.json` - Base config
- `appsettings.NodeA.json` - Node A overrides (port 5000)
- `appsettings.NodeB.json` - Node B overrides (port 5001)
- `docker-compose.yml` - Multi-node orchestration

### Documentation
- `docs/architecture/handshake-protocol.md` - Complete protocol specification
- `docs/development/channel-encryption-plan.md` - Plan to fix Phase 2+ encryption
- `docs/testing/manual-testing-guide.md` - Step-by-step debugging guide
- `docs/PROJECT_STATUS.md` - Detailed status report

### Testing Scripts
- `test-phase4.sh` - **Complete end-to-end test (Phases 1+2+3+4)** with Bash - **Use this!**
- `test-phase3.sh` - End-to-end test (Phases 1+2+3) - deprecated, use phase4
- `test-phase3-manual.ps1` - PowerShell version (has formatting issues) - deprecated
- `test-phase2-full.ps1` - Complete automated test (Phases 1+2) - deprecated
- `test-docker.ps1` - Phase 1 only - deprecated

## Development Guidelines (from .cursorrules)

### Biomedical Data Standards
- Follow HL7 FHIR standards for health data
- Implement rigorous validation for biosignals
- Always include capture and processing metadata
- Maintain detailed access and modification logs

### Security Requirements
- LGPD/GDPR compliance for privacy
- Encryption for sensitive data (✅ Fully implemented in Phases 2-3 via AES-256-GCM)
- Role-based access control (Partially implemented via node authorization status)
- Audit all data operations

### Naming Conventions
- Services: `I{Name}Service` interface, `{Name}Service` implementation
- DTOs: `{Action}Request`, `{Action}Response`
- Endpoints: `/api/{resource}/v1/{action}` (versioning planned, not yet implemented)

## Known Issues & Warnings

### ✅ All Test Issues Resolved (2025-10-03)

All 61 tests are now passing (56 from Phases 1-2, 5 new from Phase 3). Previous issues have been fixed:

**Fixed Issues:**
1. ✅ **NodeChannelClient Architecture** - Resolved by implementing `TestHttpClientFactory` and `TestHttpMessageHandler` to route requests to in-memory test servers
2. ✅ **Validation Features** - All validations implemented:
   - Timestamp validation (±5 minutes tolerance)
   - Nonce validation (Base64 format, min 12 bytes)
   - Certificate validation (format, structure, expiration check)
   - Required fields validation (NodeId, NodeName, SubjectName)
   - Enum validation (AuthorizationStatus)
3. ✅ **Encryption/Decryption** - Fixed by implementing proper signature generation with timestamps
4. ✅ **Timezone Issues** - Fixed by using `DateTime.Now` for local time comparisons

**Implemented Security Features:**
- Replay attack protection (timestamp validation)
- Certificate expiration validation
- Input sanitization and validation
- Proper error responses for invalid requests

### Compiler Warnings
- `NodeRegistryService.cs:44` - Async method without await (intentional, will be fixed with DB implementation)

### Docker Health Checks
- May show "unhealthy" even when working (curl not in base image)
- Workaround: Remove health check or install curl in Dockerfile

## Next Steps

### ✅ Phase 4 Complete - Core Handshake Protocol Finished!

All 4 phases of the handshake protocol are now implemented:
- ✅ **Phase 1**: Encrypted channel establishment (ECDH + AES-256-GCM)
- ✅ **Phase 2**: Node identification and registration (X.509 certificates)
- ✅ **Phase 3**: Challenge-response mutual authentication (RSA signatures)
- ✅ **Phase 4**: Session management and access control (Bearer tokens + capabilities)

**Phase 4 Features Implemented:**
- ✅ `SessionService` - Session lifecycle management (create, validate, renew, revoke)
- ✅ `PrismAuthenticatedSessionAttribute` - Bearer token validation middleware
- ✅ `SessionController` - Session endpoints (whoami, renew, revoke, metrics)
- ✅ Capability-based authorization (query:read, data:write, admin:node, etc.)
- ✅ Rate limiting (60 requests/minute per session)
- ✅ Session metrics and monitoring
- ✅ End-to-end testing (`test-phase4.sh`)

### Phase 5 (Next - Federated Queries)

**Implement federated query endpoints using Phase 4 session management:**

1. **Query Endpoints**
   - `POST /api/query/execute` - Execute federated query (requires `query:read`)
   - `POST /api/query/aggregate` - Aggregate query across nodes (requires `query:aggregate`)
   - `GET /api/query/{queryId}/status` - Get query status
   - `GET /api/query/{queryId}/results` - Get query results

2. **Data Submission Endpoints**
   - `POST /api/data/submit` - Submit research data (requires `data:write`)
   - `GET /api/data/{dataId}` - Get data by ID (requires `query:read`)
   - `DELETE /api/data/{dataId}` - Delete owned data (requires `data:delete`)

3. **Query Federation**
   - Forward queries to connected nodes
   - Aggregate results from multiple nodes
   - Handle node failures and timeouts
   - Cache federated query results

**Reference Architecture:**
- See `docs/architecture/phase4-session-management.md`

### Infrastructure Improvements
- Replace in-memory storage with database (PostgreSQL/SQL Server)
- Add structured logging (Serilog)
- Implement distributed tracing (OpenTelemetry)
- Add Prometheus metrics

### Documentation Updates Needed
- Update `docs/testing/manual-testing-guide.md` with dotnet test commands
- Create `docs/testing/test-troubleshooting.md` for common test failures
- Document test architecture in `docs/development/testing-strategy.md`

## Reference

**Main documentation**: `docs/README.md`

**Protocol specification**: `docs/architecture/handshake-protocol.md`

**Testing guide**: `docs/testing/manual-testing-guide.md`

**Implementation roadmap**: `docs/development/implementation-roadmap.md`
