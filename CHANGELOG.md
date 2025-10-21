# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.0] - 2025-10-07

### ✨ Added
- **PostgreSQL Node Registry (Complete)**: Production-ready persistence for node registry with relational database
  - Multi-instance PostgreSQL 18 Alpine (one database per node)
  - Entity Framework Core 8.0.10 with Npgsql provider
  - Guid-based primary keys with automatic UUID generation
  - Certificate fingerprint as unique natural key with constraint
  - 4 EF Core migrations successfully applied
  - Connection resiliency with retry policy (3 retries, 5-second delay)
  - pgAdmin 4 integration for database management
- **Clinical Data Model (Complete)**: 28-table relational schema with HL7 FHIR alignment
  - 10 clinical entities: Conditions, Events, Medications, Allergies, Vital Signs
  - SNOMED CT integration: Severity codes, body structures, laterality, topographical modifiers
  - 6 clinical services: ClinicalConditionService, ClinicalEventService, MedicationService, AllergyIntoleranceService, VitalSignsService, VolunteerClinicalService
  - Complete CRUD operations with domain-specific methods

### 🔧 Fixed
- **Database Migration Issue**: Cleaned PostgreSQL databases and reapplied CompleteSchema migration
- **Guid-based Architecture**: Removed `node_id` column from database (backward compatibility cleanup)
  - Dual-identifier system: NodeId (string protocol) + RegistrationId (Guid database)
  - All repository methods now use Guid Id
  - Administrative endpoints use `{id:guid}` route parameters
  - Certificate fingerprint-based node lookup

### 📚 Documentation
- Deleted 13 redundant/outdated documentation files
- Updated `manual-testing-guide.md` with complete Phase 4 documentation
- Updated `PROJECT_STATUS.md` with PostgreSQL and Guid architecture details
- Comprehensive documentation of dual-identifier pattern

### 🎯 Improvements
- Generic `BaseRepository<TEntity, TKey>` and `BaseService<TEntity, TKey>` pattern
- Domain-driven design organized by business contexts
- Complete service layer with all 28 entities registered in DI container
- Verified all 28 tables created correctly with proper foreign key constraints

---

## [0.7.0] - 2025-10-05

### ✨ Added
- **Redis Persistence (Complete)**: Production-ready persistence for sessions and channels with automatic TTL
  - Multi-instance Redis 7.2 Alpine (one per node)
  - StackExchange.Redis 2.8.16 client
  - `RedisSessionStore` and `RedisChannelStore` implementations
  - Automatic TTL management for sessions (1 hour) and channels (30 minutes)
  - Rate limiting via Redis Sorted Sets (token bucket algorithm)
  - Session and channel data persists across node restarts
  - Graceful fallback to in-memory storage if Redis unavailable
- **Feature Flags for Persistence**:
  - `UseRedisForSessions` - Enable Redis for session storage
  - `UseRedisForChannels` - Enable Redis for channel storage
  - `UsePostgreSqlForNodes` - Enable PostgreSQL for node registry

### 🔧 Fixed
- **Redis Channel Persistence**: Fixed `ChannelMetadata` class to include `IdentifiedNodeId` and `CertificateFingerprint`
- **All IChannelStore methods migrated to async**

### 📚 Documentation
- `docs/testing/redis-testing-guide.md` - Comprehensive Redis testing guide
- `docs/testing/docker-compose-quick-start.md` - Docker Compose quick start
- `docs/development/persistence-architecture.md` - Redis and PostgreSQL architecture
- `docs/development/persistence-implementation-roadmap.md` - Implementation plan

### 🧪 Tests
- 72/75 tests passing (96% pass rate)
- No regressions from Redis migration

---

## [0.6.0] - 2025-10-03

### ✨ Added
- **Phase 4: Session Management (Complete)**: Bearer token authentication with capability-based authorization
  - Session lifecycle management: `whoami`, `renew`, `revoke`, `metrics`
  - Capability-based authorization: ReadOnly, ReadWrite, Admin
  - Per-session rate limiting: 60 requests/minute with token bucket algorithm
  - Session token TTL: 1 hour (configurable)
  - All endpoints encrypted via AES-256-GCM channel
  - Session token sent inside encrypted payload (NOT in HTTP headers)
- **Session Management Endpoints**:
  - `POST /api/session/whoami` - Get current session info
  - `POST /api/session/renew` - Extend session TTL
  - `POST /api/session/revoke` - Logout/invalidate session
  - `POST /api/session/metrics` - Get session metrics (requires Admin capability)
- **PrismAuthenticatedSessionAttribute**: Resource filter for session validation
  - Extracts session token from decrypted payload (via reflection)
  - Validates session exists and hasn't expired
  - Enforces rate limiting (60 req/min) with token bucket algorithm
  - Supports authorization by capability level

### 📚 Documentation
- `docs/architecture/phase4-session-management.md` - Phase 4 session architecture

### 🧪 Tests
- **8 new Phase 4 integration tests** - All passing
- End-to-end test script: `test-phase4.sh` (complete Phases 1→2→3→4)
- Session-related test scenarios: WhoAmI, renewal, revocation, metrics, rate limiting

---

## [0.5.0] - 2025-10-03

### ✨ Added
- **Phase 3: Mutual Challenge/Response Authentication (Complete)**
  - Challenge-response protocol with 32-byte random challenges
  - Challenge TTL: 5 minutes (300 seconds)
  - Session token generation: 1 hour TTL
  - RSA-2048 signature verification
  - Proof of private key possession
  - One-time use challenges (invalidated after use)
  - In-memory challenge storage with key: `{ChannelId}:{NodeId}`
- **Authentication Endpoints**:
  - `POST /api/node/challenge` - Request challenge
  - `POST /api/node/authenticate` - Submit challenge response with signature
- **Testing Helper Endpoints** (Dev/NodeA/NodeB only):
  - `POST /api/testing/request-challenge` - Client wrapper for requesting challenge
  - `POST /api/testing/sign-challenge` - Helper to sign challenge data in correct format
  - `POST /api/testing/authenticate` - Client wrapper for authentication

### 🔒 Security
- Signature format: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
- Timestamp validation (±5 minutes tolerance)
- Replay attack protection via nonce validation

### 🧪 Tests
- 5 new Phase 3 integration tests - All passing
- Manual testing script: `test-phase3.sh` (complete Phases 1→2→3)

### 📚 Documentation
- Updated `docs/architecture/handshake-protocol.md` with Phase 3 specification

---

## [0.4.0] - 2025-10-02

### ✨ Added
- **Phase 2: Node Identification and Authorization (Complete)**
  - X.509 certificate-based identification
  - RSA-2048 digital signatures
  - Node registry with approval workflow (Unknown → Pending → Authorized/Revoked)
  - Certificate fingerprint (SHA-256) as natural key
  - Encrypted payload handling via `PrismEncryptedChannelConnectionAttribute<T>`
  - Dual-identifier architecture: NodeId (string) + RegistrationId (Guid)
  - ChannelContext stores `IdentifiedNodeId` (Guid) for subsequent phases
- **Node Identification Endpoints**:
  - `POST /api/channel/identify` - Identify node after channel established
  - `POST /api/node/register` - Register unknown node
  - `GET /api/node/nodes` - List registered nodes (admin)
  - `PUT /api/node/{id:guid}/status` - Update node status (admin, uses RegistrationId Guid)
- **Authorization States**: Unknown, Pending, Authorized, Revoked
- **Testing Endpoints** (Dev/NodeA/NodeB only):
  - `POST /api/testing/generate-certificate` - Generate self-signed certificate
  - `POST /api/testing/sign-data` - Sign data with certificate
  - `POST /api/testing/verify-signature` - Verify signature
  - `POST /api/testing/generate-node-identity` - Generate complete identity
  - `POST /api/testing/encrypt-payload` - Encrypt payload with channel key
  - `POST /api/testing/decrypt-payload` - Decrypt payload with channel key

### 🔒 Security
- RSA-SHA256 signature verification
- Certificate expiration validation
- Input sanitization and validation
- Replay attack protection via nonce and timestamp validation
- Certificate fingerprint uniqueness constraint

### 🧪 Tests
- 6 new Phase 2 integration tests - All passing
- Manual testing script: `test-phase2-full.ps1`

### 📚 Documentation
- `docs/architecture/handshake-protocol.md` - Complete protocol specification (Phases 1-2)
- `docs/testing/phase2-test-plan.md` - Phase 2 testing plan

---

## [0.3.1] - 2025-10-02

### 🔧 Fixed
- **JSON Deserialization of Encrypted Payloads**: Fixed camelCase/PascalCase incompatibility
  - Added `PropertyNameCaseInsensitive = true` in `Program.cs`
  - Added `[JsonPropertyName]` attributes in `EncryptedPayload`
  - Aligned `JsonSerializerOptions` between ASP.NET Core and `ChannelEncryptionService`
- **Root Cause**: Client sent JSON in camelCase, server expected PascalCase
- **Impact**: Now accepts both formats (camelCase and PascalCase)

### 📚 Documentation
- Created complete manual testing guide: `docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md`
  - 9 detailed steps with Swagger UI
  - Troubleshooting common errors
  - Validation of all Phase 2 scenarios
- Created automated PowerShell script: `test-fase2-manual.ps1`
  - Executes complete test flow automatically
  - Colorized output with step-by-step progress
  - Validation of all encrypted payloads
- Updated `README.md` with 3 testing options:
  - Automated testing (PowerShell)
  - Manual testing via Swagger
  - Integration testing (xUnit)

### 🎯 Improvements
- **Global JSON Configuration** (`Program.cs`):
  ```csharp
  builder.Services.AddControllers()
      .AddJsonOptions(options => {
          options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
          options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
          options.JsonSerializerOptions.AllowTrailingCommas = true;
      });
  ```
- **Explicit Attributes** (`EncryptedPayload`):
  ```csharp
  [JsonPropertyName("encryptedData")]
  public string EncryptedData { get; set; }

  [JsonPropertyName("iv")]
  public string Iv { get; set; }

  [JsonPropertyName("authTag")]
  public string AuthTag { get; set; }
  ```

---

## [0.3.0] - 2025-10-02

### ✨ Added
- **Channel Encryption for Phase 2+**
  - All payloads after channel establishment are encrypted with AES-256-GCM
  - Mandatory `X-Channel-Id` header for channel validation
  - `EncryptPayload`/`DecryptPayload` methods in `IChannelEncryptionService`
- **Centralized Channel Management**
  - `IChannelStore` interface for storing channel contexts
  - `ChannelStore` implementation with `ConcurrentDictionary`
  - Automatic expiration validation (30 minutes)
- **Encryption Testing Endpoints** (`TestingController`)
  - `POST /api/testing/encrypt-payload` - Encrypts any JSON payload
  - `POST /api/testing/decrypt-payload` - Decrypts received payload
  - `GET /api/testing/channel-info/{channelId}` - Channel information

### 🔒 Security
- **Breaking Change**: Endpoints `/api/node/identify` and `/api/node/register` now **require** encrypted payload
  - Format: `{"encryptedData": "...", "iv": "...", "authTag": "..."}`
  - Mandatory header: `X-Channel-Id`
- **Replay Attack Protection**: Channel ID binds requests to specific channel
- **Perfect Forward Secrecy Maintained**: Symmetric keys derived from ephemeral ECDH keys

### 📚 Documentation
- Created `docs/development/channel-encryption-implementation.md`
- Created `docs/development/testing-endpoints-criptografia.md`
- Updated `docs/testing/manual-testing-guide.md` with encryption instructions
- Created `docs/api-examples/testing-encryption.http` with request examples

### 🔧 Technical Changes
- **Controllers**:
  - Created `NodeConnectionController.cs` (Phase 2 encrypted endpoints)
  - Maintained `ChannelController.cs` (Phase 1 only)
  - Added `TestingController.cs` (testing helpers)
- **Services**:
  - Extended `ChannelEncryptionService` with payload methods
  - Created `ChannelStore` for context management
  - Updated `NodeChannelClient` to use encryption
- **Program.cs**:
  - Registered `IChannelStore` as Singleton
  - Configured NodeA/NodeB/Development environments for Swagger

---

## [0.2.0] - 2025-10-01

### ✨ Added
- **Phase 2: Node Identification and Registration** (without encryption - initial version)
  - Endpoint `POST /api/channel/identify` - Identify node with certificate
  - Endpoint `POST /api/node/register` - Register new node
  - Endpoint `PUT /api/node/{nodeId}/status` - Update authorization status
  - Endpoint `GET /api/node/nodes` - List all registered nodes
- **Node Registry Service**
  - In-memory storage of registered nodes
  - Authorization states: Unknown, Pending, Authorized, Revoked
  - RSA signature verification
  - Certificate fingerprint calculation (SHA-256)
- **Certificate Helper**
  - X.509 self-signed certificate generation
  - RSA-2048 support
  - Configurable validity

### 📚 Documentation
- Created `docs/architecture/handshake-protocol.md`
- Created `docs/testing/phase2-test-plan.md`
- Created `docs/PROJECT_STATUS.md`
- Updated `README.md` with Phase 2 status

### 🧪 Tests
- Created `test-phase2-full.ps1` - PowerShell script for complete testing
- Test scenarios:
  - Unknown node → Registration → Pending → Approval → Authorized
  - Digital signature verification
  - Next phase flow (`phase3_authenticate`)

---

## [0.1.0] - 2025-09-30

### ✨ Added
- **Phase 1: Encrypted Communication Channel Establishment**
  - Endpoint `POST /api/channel/open` - Server accepts handshake
  - Endpoint `POST /api/channel/initiate` - Client initiates handshake
  - Endpoint `GET /api/channel/health` - Health check
  - Endpoint `GET /api/channel/{channelId}` - Channel information
- **Cryptography Services**
  - `EphemeralKeyService` - ECDH P-384 ephemeral key generation
  - `ChannelEncryptionService` - HKDF + AES-256-GCM
  - `NodeChannelClient` - HTTP client for handshake
- **Clean Architecture**
  - `Bioteca.Prism.Domain` - Entities, DTOs, Requests, Responses
  - `Bioteca.Prism.Service` - Business logic
  - `Bioteca.Prism.InteroperableResearchNode` - API layer
- **Docker Deployment**
  - `docker-compose.yml` - Orchestration for Node A and Node B
  - Separate configurations: `appsettings.NodeA.json`, `appsettings.NodeB.json`
  - Health checks and networking configured

### 🔒 Security
- **Perfect Forward Secrecy (PFS)**: ECDH P-384 ephemeral keys
- **HKDF-SHA256**: Symmetric key derivation
- **AES-256-GCM**: Authenticated encryption (prepared for Phase 2+)

### 📚 Documentation
- Created initial `README.md`
- Created `docs/README.md` - Documentation index
- Created `docs/testing/manual-testing-guide.md`

### 🧪 Tests
- Created `test-docker.ps1` - PowerShell script for Phase 1 testing
- Basic test scenarios:
  - Health check
  - ECDH handshake
  - Symmetric key derivation

---

## Version Format

- **MAJOR** (X.0.0): Incompatible API changes
- **MINOR** (0.X.0): Compatible new features
- **PATCH** (0.0.X): Compatible bug fixes

## Change Categories

- **Added** - New features
- **Changed** - Changes to existing functionality
- **Deprecated** - Features to be removed
- **Removed** - Removed features
- **Fixed** - Bug fixes
- **Security** - Security vulnerability fixes
