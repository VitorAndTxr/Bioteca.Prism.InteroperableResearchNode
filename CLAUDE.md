# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Interoperable Research Node (IRN)** - Core component of the PRISM framework for federated biomedical research data. Enables secure, standardized communication between research nodes using cryptographic handshakes, node authentication, and federated queries.

**Current Status**: Phase 2 Complete (Encrypted Channel + Node Identification)

## Architecture

### Project Structure (Clean Architecture)

```
Bioteca.Prism.Domain/          # Domain layer (entities, DTOs)
├── Entities/Node/             # Domain entities
├── Requests/Node/             # Request DTOs
├── Responses/Node/            # Response DTOs
└── Errors/Node/               # Error models

Bioteca.Prism.Service/         # Service layer (business logic)
└── Services/Node/             # Node-specific services
    ├── EphemeralKeyService.cs           # ECDH P-384 ephemeral keys
    ├── ChannelEncryptionService.cs      # HKDF + AES-256-GCM
    ├── NodeChannelClient.cs             # HTTP client for handshake
    ├── NodeRegistryService.cs           # Node registry (in-memory)
    └── CertificateHelper.cs             # X.509 utilities

Bioteca.Prism.InteroperableResearchNode/  # API layer
├── Controllers/
│   ├── ChannelController.cs    # Phases 1-2 endpoints
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
- Endpoints: `/api/channel/identify`, `/api/node/register`, `/api/node/{nodeId}/status`

**Phase 3: Mutual Authentication (📋 Planned)**
- Challenge/response authentication
- Proof of private key possession

**Phase 4: Session Establishment (📋 Planned)**
- Session tokens and capabilities
- Rate limiting and metrics

### Critical Security Requirement

⚠️ **IMPORTANT**: Once a channel is established (Phase 1), **ALL subsequent communications (Phases 2-4) MUST be encrypted** using the channel's symmetric key. This is currently NOT implemented.

See implementation plan: `docs/development/channel-encryption-plan.md`

Current implementation incorrectly sends Phase 2+ payloads in plaintext. The payload format should be:
```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv",
  "authTag": "base64-encoded-auth-tag"
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

### Test Status (Last Updated: 2025-10-02)

**Overall: 34/56 tests passing (61% pass rate)**

| Category | Passing | Total | Status |
|----------|---------|-------|--------|
| Phase 1 (Channel Establishment) | 6/6 | 100% | ✅ |
| Certificate & Signature | 14/15 | 93% | ✅ |
| Phase 2 (Node Identification) | 5/6 | 83% | ✅ |
| Encrypted Channel Integration | 2/3 | 67% | ⚠️ |
| NodeChannelClient | 1/7 | 14% | ❌ |
| Security & Edge Cases | 6/17 | 36% | ❌ |

**Key Issues:**
- NodeChannelClient tests need architectural fix (IHttpClientFactory vs in-memory test servers)
- Security tests validate unimplemented features (timestamp/nonce validation, etc.)
- Some encryption edge cases need investigation

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

### Channel Flow

1. **Client initiates**: POST `/api/channel/initiate` → calls `NodeChannelClient.OpenChannelAsync(remoteUrl)`
2. **Client sends**: POST to remote `/api/channel/open` with ephemeral public key
3. **Server processes**: `ChannelController.OpenChannel()` generates server keys, derives shared secret
4. **Server responds**: Returns server public key + channel ID in `X-Channel-Id` header
5. **Both derive**: Same symmetric key from ECDH shared secret using HKDF
6. **Channel stored**: In-memory `ConcurrentDictionary<string, ChannelContext>` (30 min TTL)

### Node Identification Flow

1. **Generate certificate**: POST `/api/testing/generate-certificate` (dev only)
2. **Sign data**: POST `/api/testing/sign-data` with certificate + data
3. **Identify**: POST `/api/channel/identify` with NodeId + certificate + signature
4. **Response**:
   - Unknown node → `isKnown=false`, registration URL provided
   - Known, Pending → `isKnown=true`, `status=Pending`, `nextPhase=null`
   - Known, Authorized → `isKnown=true`, `status=Authorized`, `nextPhase="phase3_authenticate"`

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
- `test-phase2-full.ps1` - Complete automated test (Phases 1+2)
- `test-docker.ps1` - Phase 1 only

## Development Guidelines (from .cursorrules)

### Biomedical Data Standards
- Follow HL7 FHIR standards for health data
- Implement rigorous validation for biosignals
- Always include capture and processing metadata
- Maintain detailed access and modification logs

### Security Requirements
- LGPD/GDPR compliance for privacy
- Encryption for sensitive data (currently missing in Phase 2+!)
- Role-based access control
- Audit all data operations

### Naming Conventions
- Services: `I{Name}Service` interface, `{Name}Service` implementation
- DTOs: `{Action}Request`, `{Action}Response`
- Endpoints: `/api/{resource}/v1/{action}` (versioning planned, not yet implemented)

## Known Issues & Warnings

### Test Suite Issues (22 failing tests)

**1. NodeChannelClient Architecture (7 failures) - NEEDS FIX**
- **Problem**: Tests use `INodeChannelClient` which internally uses `IHttpClientFactory.CreateClient()` to make real HTTP requests
- **Issue**: Cannot connect to in-memory `TestWebApplicationFactory` servers
- **Affected Tests**:
  - `InitiateChannel_WithValidRemoteUrl_EstablishesChannel`
  - `IdentifyNode_WithInvalidSignature_ReturnsError`
  - `IdentifyNode_UnknownNode_ReturnsNotKnown`
  - `FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd`
  - `RegisterNode_AfterChannelEstablished_SuccessfullyRegisters`
  - `IdentifyNode_AfterRegistration_ReturnsPending`
  - `InitiateChannel_WithInvalidUrl_ReturnsFailure` (fixed - now checks result instead of exception)
- **Solutions**:
  - Option A: Mock `IHttpClientFactory` to return `HttpClient` from `factory.CreateClient()`
  - Option B: Refactor `NodeChannelClient` to accept `HttpClient` directly
  - Option C: Skip these tests and rely on manual/integration testing

**2. Unimplemented Validation Features (11 failures) - FUTURE WORK**
- Tests for features not yet implemented (TDD approach)
- **Missing Features**:
  - Timestamp validation (future/old timestamps)
  - Nonce validation (short/invalid nonces)
  - Certificate expiration checking
  - Empty field validation (NodeId, NodeName)
- **Affected Tests**:
  - `OpenChannel_WithFutureTimestamp_ReturnsBadRequest`
  - `OpenChannel_WithOldTimestamp_ReturnsBadRequest`
  - `OpenChannel_WithInvalidNonce_ReturnsBadRequest`
  - `OpenChannel_WithShortNonce_ReturnsBadRequest`
  - `RegisterNode_WithExpiredCertificate_ReturnsBadRequest`
  - `RegisterNode_WithInvalidCertificateFormat_ReturnsBadRequest`
  - `RegisterNode_WithEmptyNodeId_ReturnsBadRequest`
  - `RegisterNode_WithEmptyNodeName_ReturnsBadRequest`
  - `UpdateNodeStatus_ToInvalidStatus_ReturnsBadRequest`
  - `GenerateCertificate_WithEmptySubjectName_ReturnsBadRequest`
  - `RegisterNode_Twice_SecondRegistrationUpdatesInfo`

**3. Encryption/Decryption Edge Cases (3 failures) - INVESTIGATE**
- **Symptoms**: "Failed to decrypt data - authentication failed"
- **Affected Tests**:
  - `FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize`
  - `IdentifyNode_UnknownNode_ReturnsNotKnown`
  - `IdentifyNode_PendingNode_ReturnsPending`
- **Possible Causes**:
  - Channel expiration during test
  - Key derivation mismatch in multi-step scenarios
  - Certificate signing issues

**4. Minor Bugs (1 failure)**
- `CertificateHelper_GenerateCertificate_ProducesValidCertificate` - Timezone handling issue

### Compiler Warnings
- `NodeRegistryService.cs:44` - Async method without await (intentional, will be fixed with DB implementation)

### Docker Health Checks
- May show "unhealthy" even when working (curl not in base image)
- Workaround: Remove health check or install curl in Dockerfile

## Next Steps

### Test Suite Fixes (Before Phase 3)

**Priority 1: Fix NodeChannelClient Tests (7 failures)**
1. Create `TestHttpClientFactory` that returns `HttpClient` from `TestWebApplicationFactory.CreateClient()`
2. Register in `TestWebApplicationFactory.ConfigureWebHost()`:
   ```csharp
   services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(remoteFactory));
   ```
3. Update NodeChannelClientTests to properly configure the factory
4. Alternative: Use `WebApplicationFactory<Program>.Server.CreateClient()` directly

**Priority 2: Investigate Encryption Failures (3 failures)**
1. Add debug logging to track channel lifecycle
2. Verify nonce generation consistency
3. Check if tests complete before channel expires (30 min TTL)
4. Review Phase2NodeIdentificationTests helper methods

**Priority 3: Implement Missing Validations (11 failures)**
1. Add timestamp validation in `ChannelController.ValidateChannelOpenRequest()`:
   - Reject timestamps > 5 minutes in future
   - Reject timestamps > 30 seconds in past
2. Add nonce validation (min 12 bytes)
3. Add certificate expiration check in `NodeConnectionController`
4. Add required field validation with `[Required]` attributes
5. Implement proper enum validation for `AuthorizationStatus`

**Priority 4: Fix Minor Issues (1 failure)**
1. Fix timezone handling in `CertificateHelper_GenerateCertificate_ProducesValidCertificate`

### Phase 3 Implementation
**After all tests pass, proceed with Phase 3:**
- Implement challenge/response authentication
- Verify private key possession
- Prevent replay attacks
- Add comprehensive tests for Phase 3

### Phase 4 (Planned)
- Session management with capabilities
- Token-based access control
- Rate limiting and metrics

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
