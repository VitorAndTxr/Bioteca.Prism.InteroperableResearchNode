# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Interoperable Research Node (IRN)** - Core component of the PRISM framework for federated biomedical research data. Enables secure, standardized communication between research nodes using cryptographic handshakes, node authentication, and federated queries.

**Current Status**: Phase 3 Complete (Encrypted Channel + Node Identification + Mutual Authentication)

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
└── Services/Node/             # Node-specific services
    ├── NodeChannelClient.cs             # HTTP client for handshake
    ├── NodeRegistryService.cs           # Node registry (in-memory)
    ├── ChallengeService.cs              # Challenge-response authentication
    └── CertificateHelper.cs             # X.509 utilities

Bioteca.Prism.InteroperableResearchNode/  # API layer
├── Controllers/
│   ├── ChannelController.cs    # Phase 1 endpoints
│   ├── NodeConnectionController.cs  # Phases 2-3 endpoints (registration, identification, authentication)
│   └── TestingController.cs    # Dev/test utilities
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

**Phase 4: Session Establishment (📋 Planned)**
- Session tokens and capabilities
- Rate limiting and metrics

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

### Test Status (Last Updated: 2025-10-03 - 06:00)

**Overall: 61/61 tests passing (100% pass rate)** ✅

| Category | Passing | Total | Status |
|----------|---------|-------|--------|
| Phase 1 (Channel Establishment) | 6/6 | 100% | ✅ |
| Certificate & Signature | 15/15 | 100% | ✅ |
| Phase 2 (Node Identification) | 6/6 | 100% | ✅ |
| **Phase 3 (Mutual Authentication)** | **5/5** | **100%** | ✅ |
| Encrypted Channel Integration | 3/3 | 100% | ✅ |
| NodeChannelClient | 7/7 | 100% | ✅ |
| Security & Edge Cases | 17/17 | 100% | ✅ |

**Recent Updates (2025-10-03 - 06:00):**
- ✅ **Phase 3 Implementation Complete**: Challenge-response authentication with RSA signature verification
- ✅ Session token generation (1-hour TTL)
- ✅ Challenge expiration (5-minute TTL)
- ✅ Integration with encrypted channel (all Phase 3 requests encrypted via AES-256-GCM)
- ✅ Comprehensive test coverage (5 new tests for Phase 3)
- ✅ **New Testing Helper**: `/api/testing/sign-challenge` endpoint for simplified manual testing
- ✅ Manual end-to-end test script (`test-phase3.sh`) for complete authentication flow verification

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
- `test-phase3.sh` - Complete end-to-end manual test (Phases 1+2+3) with Bash
- `test-phase3-manual.ps1` - PowerShell version (has formatting issues, use .sh instead)
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

### ✅ Phase 3 Complete - Ready for Phase 4

All Phase 3 features are implemented and tested (61/61 tests passing):
- ✅ Encrypted channel establishment (Phase 1)
- ✅ Node identification and registration (Phase 2)
- ✅ Challenge-response mutual authentication (Phase 3)
- ✅ Certificate-based identity verification
- ✅ RSA signature verification for authentication
- ✅ Session token generation (1-hour TTL)
- ✅ Complete validation suite (timestamps, nonces, certificates, fields, challenges)
- ✅ Security hardening (replay protection, input validation, one-time challenges)

### Phase 4 (Next - Ready to Implement)

**Session Management and Access Control**

Session tokens are already being generated in Phase 3. Phase 4 will implement:

1. **Session Validation Middleware**
   - `PrismAuthenticatedSessionAttribute` - Validate Bearer tokens
   - Extract and validate session from `Authorization: Bearer {token}` header
   - Verify session hasn't expired (1-hour TTL)
   - Load node capabilities into request context

2. **Session Service** (`ISessionService`)
   - `ValidateSessionAsync(token)` - Validate session token
   - `RenewSessionAsync(token)` - Extend session before expiration
   - `RevokeSessionAsync(token)` - Logout/invalidate session
   - `GetSessionMetricsAsync(nodeId)` - Usage statistics
   - `CleanupExpiredSessionsAsync()` - Background cleanup

3. **Protected Endpoints**
   - `/api/session/whoami` - Get current session info (test endpoint)
   - `/api/session/renew` - Renew session token
   - `/api/session/revoke` - Logout
   - `/api/query/execute` - Federated query (requires `query:read` capability)
   - `/api/data/submit` - Submit research data (requires `data:write` capability)

4. **Capabilities-Based Authorization**
   - `query:read` - Read/query federated data
   - `query:aggregate` - Aggregate queries across nodes
   - `data:write` - Submit research data
   - `data:delete` - Delete owned data
   - `admin:node` - Node administration

5. **Rate Limiting and Metrics**
   - Track requests per session
   - Implement rate limits per capability
   - Prometheus metrics for monitoring
   - Audit logging for all authenticated operations

**Architecture Documents:**
- See `docs/architecture/phase4-session-management.md` (to be created)

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
