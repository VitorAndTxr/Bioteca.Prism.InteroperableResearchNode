# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## PRISM Project - Master Architecture Overview

**PRISM** (Project Research Interoperability and Standardization Model) is a comprehensive federated framework for biomedical research data management, designed to break down data silos and enable secure, standardized collaboration across research institutions.

### Project Vision

The PRISM ecosystem addresses the fundamental problem of data fragmentation in biomedical research by creating a federated network where each research institution maintains sovereignty over its data while enabling secure cross-institutional queries and collaboration.

### Ecosystem Components

The PRISM framework consists of four interconnected components:

#### 1. **InteroperableResearchNode** (Backend - This Project)
**Purpose**: Core backend server implementing the federated research data exchange protocol
**Technology**: ASP.NET Core 8.0 (C#), PostgreSQL 18, Redis 7.2
**Key Features**:
- 4-phase cryptographic handshake protocol (ECDH + RSA-2048)
- Node-to-node secure communication with Perfect Forward Secrecy
- Clinical data model (28 tables) with HL7 FHIR alignment
- SNOMED CT integration for medical terminologies
- Multi-instance architecture (independent PostgreSQL + Redis per node)
- Generic repository and service pattern with Clean Architecture

**Role in Ecosystem**: Acts as the trusted gateway for each research institution, managing:
- Authentication and authorization of other nodes
- Secure storage of research data (volunteers, researchers, devices, sessions, biosignals)
- Federated query execution across the network
- Data validation and quality control

#### 2. **InteroperableResearchsEMGDevice** (Embedded Firmware)
**Purpose**: ESP32-based hardware device for electrostimulation and biosignal acquisition
**Technology**: C++, FreeRTOS, ESP32 dual-core, PlatformIO
**Key Features**:
- Surface electromyography (sEMG) signal processing with Butterworth filtering
- Functional Electrical Stimulation (FES) with programmable parameters
- Real-time biosignal streaming (10-200 Hz) via Bluetooth
- Dual-core architecture (Core 0: signal processing, Core 1: communication)
- JSON-based Bluetooth protocol for mobile app communication

**Role in Ecosystem**: Represents the **Device** abstraction in PRISM model - specialized hardware for biosignal capture and therapeutic stimulation, communicating with mobile applications for data collection.

#### 3. **InteroperableResearchInterfaceSystem** (Interface Layer)
**Purpose**: TypeScript/Node.js middleware for communication orchestration
**Technology**: TypeScript, Node.js, MCP Tunnel
**Key Features**:
- Protocol translation between components
- WebSocket/HTTP communication management
- API gateway and routing

**Role in Ecosystem**: Bridges the **Application** layer (mobile app) with the **Device** layer (ESP32 firmware) and potentially the **Node** backend for data submission.

#### 4. **neurax_react_native_app** (Mobile Application)
**Purpose**: React Native mobile application for research data collection
**Technology**: React Native, Expo, TypeScript
**Key Features**:
- Bluetooth communication with sEMG device
- Real-time biosignal visualization
- Session management and FES parameter configuration
- User interface for researchers and volunteers

**Role in Ecosystem**: Represents the **Application** abstraction in PRISM model - general-purpose software that adds context (volunteer info, session metadata), controls hardware, and potentially submits data to research nodes.

### Architectural Philosophy

**PRISM Model Abstraction**:
```
┌─────────────────────────────────────────────────────────────┐
│                    Research Institution                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐         ┌──────────────┐                  │
│  │ Application  │────────▶│   Device     │                  │
│  │ (Mobile App) │  BT     │  (sEMG/FES)  │                  │
│  └──────┬───────┘         └──────────────┘                  │
│         │ HTTPS                                              │
│         │                                                     │
│  ┌──────▼────────────────────────────────────────┐          │
│  │    Interoperable Research Node (IRN)          │          │
│  │    - Data validation and storage              │          │
│  │    - Authentication and authorization         │          │
│  │    - Federated query engine                   │          │
│  └───────────────────────────┬───────────────────┘          │
│                              │ Encrypted Channel              │
└──────────────────────────────┼───────────────────────────────┘
                               │
                ┌──────────────▼──────────────┐
                │   Federated PRISM Network   │
                │  (Other Research Nodes)     │
                └─────────────────────────────┘
```

**Key Design Principles**:
1. **Data Sovereignty**: Each node maintains full control over its data
2. **Cryptographic Trust**: 4-phase handshake ensures mutual authentication before data exchange
3. **Standardization**: HL7 FHIR + SNOMED CT for interoperability
4. **Separation of Concerns**: Device (capture) ≠ Application (context) ≠ Node (storage/federation)
5. **Privacy by Design**: LGPD/GDPR compliance, encryption at rest and in transit

### Data Flow Example (Complete Research Session)

```
1. Researcher prepares session via Mobile App
   ├─> Configures FES parameters (amplitude, frequency, pulse width)
   ├─> Selects volunteer and research project
   └─> Connects to sEMG Device via Bluetooth

2. sEMG Device acquires biosignals
   ├─> Samples at 860 Hz with AD8232 sensor
   ├─> Applies Butterworth filter (10-40 Hz)
   ├─> Detects muscle activation threshold
   └─> Triggers FES stimulation when threshold exceeded

3. Mobile App collects session data
   ├─> Receives real-time sEMG stream (13 samples/packet)
   ├─> Logs FES events and trigger timestamps
   ├─> Records volunteer vital signs and notes
   └─> Packages data with SNOMED CT annotations

4. Data submission to Research Node (future)
   ├─> Authenticates with node via handshake protocol
   ├─> Encrypts payload with AES-256-GCM
   ├─> Submits biosignal files + metadata
   └─> Node validates and stores in PostgreSQL

5. Federated query across network (future)
   ├─> Node A queries "sEMG sessions with spasticity events"
   ├─> Request authenticated and encrypted to Node B, C, D
   ├─> Each node executes query on local data
   ├─> Aggregated results returned to Node A
   └─> Privacy-preserving: raw data never leaves source nodes
```

### Technology Stack Overview

| Component | Language | Runtime | Database | Communication |
|-----------|----------|---------|----------|---------------|
| **InteroperableResearchNode** | C# 12 | .NET 8.0 | PostgreSQL 18 + Redis 7.2 | HTTPS (TLS 1.3) |
| **sEMG Device Firmware** | C++ | ESP32 FreeRTOS | N/A | Bluetooth SPP (JSON) |
| **Interface System** | TypeScript | Node.js | N/A | WebSocket/HTTP |
| **Mobile App** | TypeScript/JSX | React Native (Expo) | SQLite (local) | Bluetooth + HTTPS |

### Current Development Status (October 2025)

**✅ Completed (Production-Ready)**:
- InteroperableResearchNode: Phases 1-4 + Clinical Data Model (28 tables)
- sEMG Device: FES control + Real-time streaming + Bluetooth protocol
- Mobile App: Basic UI + Bluetooth connection (partial)

**🚧 In Progress**:
- Phase 5 (Federated Queries): Cross-node data querying
- Mobile App: Data submission to research nodes
- Interface System: Protocol translation layer

**📋 Planned**:
- Production certificate management (Let's Encrypt)
- Observability stack (Prometheus + OpenTelemetry)
- Clinical trial management features
- Multi-center research coordination tools

### Navigation for AI Assistants

When working on this codebase:

1. **Backend/Node Development** → You are here (`InteroperableResearchNode/CLAUDE.md`)
2. **Device Firmware** → See `InteroperableResearchsEMGDevice/CLAUDE.md`
3. **Interface System** → See `InteroperableResearchInterfaceSystem/CLAUDE.md`
4. **Mobile App** → See `neurax_react_native_app/` (limited AI guidance)
5. **Master Overview** → See root `CLAUDE.md` for cross-component context

---

## Documentation Standards for All LLM Providers

**IMPORTANT: This section applies to ALL LLM technology providers and manufacturers (including but not limited to OpenAI, Anthropic, Google, Meta, Microsoft, Amazon, etc.)**

### Primary Language Requirement

**All project documentation MUST be written in English**, including:
- Architecture documents
- API documentation
- Code comments and docstrings
- README files
- Technical specifications
- Testing guides
- Implementation roadmaps
- Inline code documentation

### Language Policy Exceptions

The following are acceptable uses of Portuguese or other languages:
- User-facing application interfaces (if targeting Brazilian/Portuguese markets)
- End-user help documentation (market-specific)
- Marketing materials
- Business requirement documents (internal use only)

### Translation Guidelines

When working with this codebase:
1. **Always write new documentation in English**
2. **Translate existing Portuguese documentation to English when encountered**
3. **Maintain technical terminology in English** (e.g., "handshake", "encryption", "authentication")
4. **Preserve code examples and technical accuracy during translation**
5. **Update cross-references and links after translation**

### Rationale

English documentation ensures:
- Global accessibility for international collaborators
- Better integration with international biomedical research networks
- Compatibility with English-based technical standards (HL7 FHIR, X.509, OAuth, etc.)
- Improved discoverability in academic and open-source communities

## Project Overview

**Interoperable Research Node (IRN)** - Core component of the PRISM framework for federated biomedical research data. Enables secure, standardized communication between research nodes using cryptographic handshakes, node authentication, and federated queries.

**Current Status**: Phase 4 Complete + Redis Persistence + PostgreSQL Persistence + Generic Repository Pattern + Complete Service Layer Architecture (Encrypted Channel + Node Identification + Mutual Authentication + Session Management + Clinical Data Model)

## Architecture

### Project Structure (Clean Architecture + Generic Base Pattern)

```
Bioteca.Prism.Domain/          # Domain layer (entities, DTOs, enums)
├── Entities/                  # Domain entities organized by context
│   ├── Node/                  # Node management entities
│   ├── Research/              # Research project entities
│   ├── Volunteer/             # Volunteer/participant entities
│   ├── Researcher/            # Researcher entities
│   ├── Application/           # Data collection applications
│   ├── Device/                # Hardware devices
│   ├── Sensor/                # Device sensors
│   ├── Record/                # Data recording sessions
│   ├── Clinical/              # Clinical catalogs (conditions, events, medications, allergies)
│   ├── Snomed/                # SNOMED CT terminologies
│   └── Session/               # Session management
├── Requests/                  # Request DTOs
├── Responses/                 # Response DTOs
├── Enumerators/               # Domain enums
├── Errors/                    # Error models
└── Payloads/                  # Encrypted payload models

Bioteca.Prism.Core/            # Core layer (interfaces, base implementations)
├── Interfaces/                # Core interfaces
│   ├── IBaseRepository<TEntity, TKey>    # Generic repository interface
│   ├── IServiceBase<TEntity, TKey>       # Generic service interface
│   ├── IChannelEncryptionService
│   └── IEphemeralKeyService
├── Database/                  # Database base implementations
│   ├── Context/               # Base DbContext configurations
│   └── Repositories/          # Generic BaseRepository<TEntity, TKey>
├── Service/                   # Generic BaseService<TEntity, TKey>
├── Middleware/                # Request processing middleware
│   ├── Channel/               # Channel validation and encryption
│   ├── Node/                  # Node identification and authentication
│   └── Session/               # Session management
├── Security/                  # Security utilities
│   ├── Certificate/           # X.509 certificate helpers
│   └── Cryptography/          # ECDH, AES-GCM, HKDF implementations
└── Cache/                     # Cache interfaces
    └── Session/               # Session store interfaces

Bioteca.Prism.Data/            # Data layer (PostgreSQL + Redis persistence)
├── Persistence/
│   ├── Contexts/
│   │   ├── PrismDbContext.cs            # EF Core DbContext
│   │   └── PrismDbContextFactory.cs     # Design-time factory for migrations
│   └── Configurations/                  # EF Core entity configurations (28 entities)
│       ├── ResearchNodeConfiguration.cs
│       ├── ResearchConfiguration.cs
│       ├── VolunteerConfiguration.cs
│       ├── ResearcherConfiguration.cs
│       ├── ApplicationConfiguration.cs
│       ├── DeviceConfiguration.cs
│       ├── SensorConfiguration.cs
│       ├── RecordSessionConfiguration.cs
│       ├── RecordConfiguration.cs
│       ├── RecordChannelConfiguration.cs
│       ├── TargetAreaConfiguration.cs
│       ├── SnomedLateralityConfiguration.cs
│       ├── SnomedTopographicalModifierConfiguration.cs
│       ├── SnomedBodyRegionConfiguration.cs
│       ├── SnomedBodyStructureConfiguration.cs
│       ├── SnomedSeverityCodeConfiguration.cs
│       ├── ClinicalConditionConfiguration.cs
│       ├── ClinicalEventConfiguration.cs
│       ├── MedicationConfiguration.cs
│       ├── AllergyIntoleranceConfiguration.cs
│       ├── VitalSignsConfiguration.cs
│       ├── VolunteerAllergyIntoleranceConfiguration.cs
│       ├── VolunteerMedicationConfiguration.cs
│       ├── VolunteerClinicalConditionConfiguration.cs
│       ├── VolunteerClinicalEventConfiguration.cs
│       ├── ResearchApplicationConfiguration.cs
│       ├── ResearchDeviceConfiguration.cs
│       ├── ResearchVolunteerConfiguration.cs
│       └── ResearchResearcherConfiguration.cs
├── Interfaces/                          # Repository interfaces by domain
│   ├── Node/INodeRepository.cs
│   ├── Research/IResearchRepository.cs
│   ├── Volunteer/IVolunteerRepository.cs
│   ├── Researcher/IResearcherRepository.cs
│   ├── Application/IApplicationRepository.cs
│   ├── Device/IDeviceRepository.cs
│   ├── Sensor/ISensorRepository.cs
│   ├── Record/                          # Record-related repositories
│   │   ├── IRecordSessionRepository.cs
│   │   ├── IRecordRepository.cs
│   │   ├── IRecordChannelRepository.cs
│   │   └── ITargetAreaRepository.cs
│   └── Snomed/ISnomedRepository.cs
├── Repositories/                        # Repository implementations by domain
│   ├── Node/NodeRepository.cs           # Extends BaseRepository<ResearchNode, Guid>
│   ├── Research/ResearchRepository.cs   # Extends BaseRepository<Research, Guid>
│   ├── Volunteer/VolunteerRepository.cs # Extends BaseRepository<Volunteer, Guid>
│   ├── Researcher/ResearcherRepository.cs
│   ├── Application/ApplicationRepository.cs
│   ├── Device/DeviceRepository.cs
│   ├── Sensor/SensorRepository.cs
│   ├── Record/                          # Record repositories
│   │   ├── RecordSessionRepository.cs
│   │   ├── RecordRepository.cs
│   │   ├── RecordChannelRepository.cs
│   │   └── TargetAreaRepository.cs
│   └── Snomed/SnomedRepository.cs       # All SNOMED implementations
├── Migrations/                          # EF Core migrations
│   └── 20251008152728_CompleteSchema.cs # Current migration (28 tables)
└── Cache/
    └── Channel/
        └── ChannelStore.cs              # In-memory channel storage (fallback)

Bioteca.Prism.Service/         # Service layer (business logic)
├── Interfaces/                # Service interfaces by domain
│   ├── Application/IApplicationService.cs
│   ├── Clinical/              # Clinical service interfaces
│   │   ├── IClinicalConditionService.cs
│   │   ├── IClinicalEventService.cs
│   │   ├── IMedicationService.cs
│   │   ├── IAllergyIntoleranceService.cs
│   │   ├── IVitalSignsService.cs
│   │   └── IVolunteerClinicalService.cs
│   ├── Device/IDeviceService.cs
│   ├── Node/                  # Node-specific service interfaces
│   ├── Record/                # Record service interfaces
│   ├── Research/IResearchService.cs
│   ├── Researcher/IResearcherService.cs
│   ├── Sensor/ISensorService.cs
│   ├── Snomed/ISnomedService.cs
│   └── Volunteer/IVolunteerService.cs
├── Services/                  # Service implementations by domain
│   ├── Application/ApplicationService.cs     # Extends BaseService<Application, Guid>
│   ├── Clinical/              # Clinical services
│   │   ├── ClinicalConditionService.cs       # Extends BaseService<ClinicalCondition, string>
│   │   ├── ClinicalEventService.cs           # Extends BaseService<ClinicalEvent, string>
│   │   ├── MedicationService.cs              # Extends BaseService<Medication, string>
│   │   ├── AllergyIntoleranceService.cs      # Extends BaseService<AllergyIntolerance, string>
│   │   ├── VitalSignsService.cs              # Extends BaseService<VitalSigns, Guid>
│   │   └── VolunteerClinicalService.cs       # Aggregate service (no base class)
│   ├── Device/DeviceService.cs               # Extends BaseService<Device, Guid>
│   ├── Node/                  # Node-specific services
│   │   ├── NodeChannelClient.cs              # HTTP client for handshake
│   │   ├── NodeRegistryService.cs            # Node registry (in-memory fallback)
│   │   ├── PostgreSqlNodeRegistryService.cs  # PostgreSQL-backed node registry
│   │   └── ChallengeService.cs               # Challenge-response authentication
│   ├── Record/                # Record services
│   │   ├── RecordSessionService.cs
│   │   ├── RecordService.cs
│   │   ├── RecordChannelService.cs
│   │   └── TargetAreaService.cs
│   ├── Research/ResearchService.cs           # Extends BaseService<Research, Guid>
│   ├── Researcher/ResearcherService.cs       # Extends BaseService<Researcher, Guid>
│   ├── Sensor/SensorService.cs               # Extends BaseService<Sensor, Guid>
│   ├── Snomed/SnomedService.cs               # SNOMED terminology service
│   ├── Volunteer/VolunteerService.cs         # Extends BaseService<Volunteer, Guid>
│   ├── Session/SessionService.cs             # Session lifecycle (Phase 4)
│   └── Cache/                 # Cache persistence services
│       ├── RedisConnectionService.cs         # Redis connection management
│       ├── RedisSessionStore.cs              # Redis session persistence
│       ├── InMemorySessionStore.cs           # In-memory session fallback
│       └── RedisChannelStore.cs              # Redis channel persistence

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

### Generic Base Pattern Architecture

**Design Philosophy**: DRY principle with type-safe generic base classes for repositories and services.

**Core Interfaces** (`Bioteca.Prism.Core/Interfaces/`):

```csharp
// Generic repository interface
public interface IBaseRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}

// Generic service interface
public interface IServiceBase<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}
```

**Base Implementations**:

```csharp
// Bioteca.Prism.Core/Database/Repositories/BaseRepository.cs
public class BaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    // ... implementation with EF Core
}

// Bioteca.Prism.Core/Service/BaseService.cs
public class BaseService<TEntity, TKey> : IServiceBase<TEntity, TKey> where TEntity : class
{
    protected readonly IBaseRepository<TEntity, TKey> _repository;
    // ... implementation delegating to repository
}
```

**Domain-Specific Extensions**:

```csharp
// Example: Bioteca.Prism.Service/Interfaces/Volunteer/IVolunteerService.cs
public interface IVolunteerService : IServiceBase<Volunteer, Guid>
{
    // Inherits all base CRUD methods

    // Add domain-specific methods:
    Task<List<Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default);
}

// Bioteca.Prism.Service/Services/Volunteer/VolunteerService.cs
public class VolunteerService : BaseService<Volunteer, Guid>, IVolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;

    public VolunteerService(IVolunteerRepository repository) : base(repository)
    {
        _volunteerRepository = repository;
    }

    // Base methods inherited: GetByIdAsync, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsAsync

    // Domain-specific implementations:
    public async Task<List<Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _volunteerRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }
}
```

**Benefits**:
- ✅ **Code Reusability**: Base CRUD operations implemented once
- ✅ **Type Safety**: Generic constraints ensure proper usage
- ✅ **Consistency**: All repositories/services follow same pattern
- ✅ **Extensibility**: Easy to add domain-specific methods
- ✅ **Maintainability**: Changes to base pattern affect all implementations
- ✅ **Testability**: Mock base interfaces for unit testing

### Node Identifier Architecture (Dual-Identifier System)

**IMPORTANT**: The system uses TWO types of identifiers for nodes:

1. **NodeId (string)** - Protocol-level identifier
   - Used in all request/response DTOs for external communication
   - Human-readable (e.g., "hospital-research-node-001", "node-a")
   - Sent in Phase 2 identification requests
   - Used in Phase 3 authentication requests
   - **Purpose**: External protocol communication

2. **RegistrationId / Id (Guid)** - Database primary key
   - Internal unique identifier (e.g., `f6cdb452-17a1-4d8f-9241-0974f80c56ef`)
   - Primary key in `research_nodes` table
   - Used for all database operations and administrative endpoints
   - Returned in `NodeStatusResponse.RegistrationId` after Phase 2 identification
   - **Purpose**: Internal database operations, administrative endpoints

3. **Certificate Fingerprint (SHA-256)** - Natural key
   - SHA-256 hash of the X.509 certificate bytes
   - Used for node lookups during identification and registration
   - Enforces uniqueness constraint (unique index in database)
   - **Purpose**: True unique identifier for authentication

**Usage Pattern**:
```
Phase 2 Identification → Send NodeId (string) → Receive RegistrationId (Guid)
Administrative Operations → Use RegistrationId (Guid)
Phase 3 Authentication → Use NodeId (string)
```

**Key Endpoints**:
- **Identification**: `POST /api/channel/identify` - Uses string NodeId in request
- **Status Update**: `PUT /api/node/{id:guid}/status` - Uses Guid RegistrationId as route parameter
- **Authentication**: `POST /api/node/authenticate` - Uses string NodeId in request

**Database Schema** (`research_nodes` table):
- **Primary Key**: `id` (uuid) - The RegistrationId
- **Unique Index**: `certificate_fingerprint` (text) - Natural key for lookups
- **No `node_id` column**: String NodeId is NOT stored in database (protocol-level only)

### Handshake Protocol (4 Phases)

**Phase 1: Encrypted Channel (✅ Complete)**
- ECDH P-384 ephemeral key exchange
- HKDF-SHA256 key derivation
- AES-256-GCM symmetric encryption
- Perfect Forward Secrecy
- **Redis or In-Memory storage** (configurable via feature flags)
- Automatic TTL management (30 minutes default)
- Endpoints: `/api/channel/open`, `/api/channel/initiate`

**Phase 2: Node Identification (✅ Complete)**
- X.509 certificate-based identification (SHA-256 fingerprint as natural key)
- RSA-2048 digital signatures
- Node registry with approval workflow (Unknown → Pending → Authorized/Revoked)
- **PostgreSQL persistence** with EF Core 8.0.10 (configurable via feature flags)
- **Certificate fingerprint uniqueness**: Re-registration updates existing node if certificate matches
- **Encrypted payload handling via `PrismEncryptedChannelConnectionAttribute<T>`**
- **Returns both NodeId (string) and RegistrationId (Guid)** in `NodeStatusResponse`
- **Stores IdentifiedNodeId (Guid) in ChannelContext** for subsequent phases
- Endpoints: `/api/channel/identify`, `/api/node/register`, `/api/node/{id:guid}/status` (admin)

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
- **Redis or In-Memory storage** (configurable via feature flags)
- Rate limiting (60 requests/minute) using Redis Sorted Sets
- Session metrics and monitoring
- Automatic TTL management (1 hour default)
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

## Docker Architecture

The project uses a **two-layer Docker Compose architecture** for better data persistence and lifecycle management:

### Layer 1: Persistence (`docker-compose.persistence.yml`)
Stateful services with persistent data:
- PostgreSQL Node A (port 5432) + Node B (port 5433)
- Redis Node A (port 6379) + Node B (port 6380)
- Azurite (ports 10000-10002) - Azure Storage Emulator
- pgAdmin (port 5050) - Database management UI

**Features**:
- Named volumes with explicit names (e.g., `irn-postgres-data-node-a`)
- `restart: unless-stopped` policy
- Shared network `irn-network`
- Data persists even when containers are stopped

### Layer 2: Application (`docker-compose.application.yml`)
Stateless application services:
- Node A (port 5000) - Research Node instance A
- Node B (port 5001) - Research Node instance B

**Features**:
- Connects to external `irn-network` (created by persistence layer)
- Environment variables override appsettings.json for Docker networking
- Connection strings use Docker service names (e.g., `irn-postgres-node-a`, `irn-redis-node-a`)
- Safe to restart without data loss
- Fast rebuild/restart cycles

**Important Environment Variables** (in docker-compose.application.yml):
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=NodeA
  - ConnectionStrings__PrismDatabase=Host=irn-postgres-node-a;Port=5432;Database=prism_node_a_registry;Username=prism_user_a;Password=prism_secure_password_2025_a
  - Redis__ConnectionString=irn-redis-node-a:6379,password=prism-redis-password-node-a,abortConnect=false
```

**Network Configuration**:
- All containers MUST be on the same `irn-network` for service discovery
- Use Docker service names for container-to-container communication
- `localhost` only works for host-to-container communication on published ports

### Legacy File (`docker-compose.yml`)
All services in one file for backward compatibility and quick local development.

**See**: `docs/development/DOCKER-SETUP.md` for comprehensive Docker documentation.

## Development Commands

### Build & Run

```bash
# Build solution
dotnet build Bioteca.Prism.InteroperableResearchNode/Bioteca.Prism.InteroperableResearchNode.sln

# Run locally (Node A on port 5000)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeA

# Run locally (Node B on port 5001)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeB

# Docker multi-node setup (separated architecture - recommended)
# 1. Start persistence layer (PostgreSQL, Redis, Azurite) - one-time setup
docker-compose -f docker-compose.persistence.yml up -d

# 2. Start application layer (Node A, Node B)
docker-compose -f docker-compose.application.yml up -d

# 3. Stop applications (data persists)
docker-compose -f docker-compose.application.yml down

# 4. Rebuild and restart applications
docker-compose -f docker-compose.application.yml up -d --build

# Legacy: All services together (for quick local development)
docker-compose up -d                    # Start all services
docker-compose down                     # Stop all containers (volumes persist)
docker-compose down -v                  # Stop and remove volumes (⚠️ deletes data)
docker-compose build --no-cache         # Rebuild after code changes

# View logs
docker logs -f irn-node-a               # View Node A logs
docker logs -f irn-node-b               # View Node B logs
docker logs -f irn-postgres-node-a      # View PostgreSQL Node A logs
docker logs -f irn-redis-node-a         # View Redis Node A logs

# Redis CLI access
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b

# PostgreSQL access
docker exec -it irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry
docker exec -it irn-postgres-node-b psql -U prism_user_b -d prism_node_b_registry

# EF Core migrations (from project root)
dotnet ef migrations add MigrationName --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode
dotnet ef database update --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode
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

**Recent Updates (2025-10-05):**
- ✅ **Redis Persistence Implementation**: Session and channel storage with automatic TTL
- ✅ Multi-instance Redis architecture (one per node)
- ✅ `RedisSessionStore` and `RedisChannelStore` implementations
- ✅ Feature flags for conditional Redis usage (`UseRedisForSessions`, `UseRedisForChannels`)
- ✅ In-memory fallback implementations (`InMemorySessionStore`, async `ChannelStore`)
- ✅ Comprehensive Redis testing documentation
- ✅ All IChannelStore methods migrated to async
- ✅ 72/75 tests passing (96% - no regressions from Redis migration)

**Previous Updates (2025-10-03):**
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

Service registration based on scope and feature flags:

**Singleton services** (shared state across requests):
- `IEphemeralKeyService` - ECDH key generation
- `IChannelEncryptionService` - Crypto operations
- `INodeChannelClient` - HTTP client for initiating handshakes (5-minute timeout)
- `INodeRegistryService` - Node registry (in-memory fallback or PostgreSQL-backed)
- `IChallengeService` - Challenge-response authentication (Phase 3)
- `ISessionService` - Session lifecycle management (Phase 4)
- `IRedisConnectionService` - Redis connection management (conditional)
- `ISessionStore` - Session persistence (Redis or In-Memory based on feature flags)
- `IChannelStore` - Channel persistence (Redis or In-Memory based on feature flags)

**Scoped services** (new instance per request, for database operations):
- `PrismDbContext` - EF Core DbContext (PostgreSQL, conditional)
- `INodeRepository` - Node repository (Guid-based CRUD operations, conditional)
- `IResearchRepository` - Research project repository
- `IVolunteerRepository` - Volunteer/participant repository
- `IResearcherRepository` - Researcher repository
- `IApplicationRepository` - Application repository
- `IDeviceRepository` - Device repository
- `ISensorRepository` - Sensor repository
- `IRecordSessionRepository` - Record session repository
- `IRecordRepository` - Record repository
- `IRecordChannelRepository` - Record channel repository
- `ITargetAreaRepository` - Target area repository
- `ISnomedLateralityRepository` - SNOMED laterality codes repository
- `ISnomedTopographicalModifierRepository` - SNOMED topographical modifiers repository
- `ISnomedBodyRegionRepository` - SNOMED body regions repository
- `ISnomedBodyStructureRepository` - SNOMED body structures repository

**Feature Flags** (in appsettings.json):
```json
{
  "FeatureFlags": {
    "UseRedisForSessions": true,    // Enable Redis for session storage
    "UseRedisForChannels": true,    // Enable Redis for channel storage
    "UsePostgreSqlForNodes": true   // Enable PostgreSQL for node registry
  },
  "HttpClient": {
    "TimeoutSeconds": 300  // HTTP client timeout (5 minutes)
  }
}
```

### Redis Persistence (✅ Implemented)

**Multi-Instance Architecture**: Each node has its own isolated Redis instance.

```yaml
# docker-compose.yml
redis-node-a:
  port: 6379
  password: prism-redis-password-node-a
  volume: redis-data-node-a

redis-node-b:
  port: 6380
  password: prism-redis-password-node-b
  volume: redis-data-node-b
```

**Feature Flags** (in `appsettings.NodeA.json` and `appsettings.NodeB.json`):
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=prism-redis-password-node-a,abortConnect=false",
    "EnableRedis": false  // Set to true to enable Redis
  },
  "FeatureFlags": {
    "UseRedisForSessions": false,  // Enable Redis for session storage
    "UseRedisForChannels": false   // Enable Redis for channel storage
  }
}
```

**Storage Implementations**:

1. **Sessions** (`ISessionStore`):
   - **Redis**: `RedisSessionStore` - Automatic TTL, rate limiting via Sorted Sets
   - **In-Memory**: `InMemorySessionStore` - Fallback with manual cleanup
   - **Key Pattern**: `session:{token}`, `session:node:{nodeId}:sessions`, `session:ratelimit:{token}`

2. **Channels** (`IChannelStore`):
   - **Redis**: `RedisChannelStore` - Separates metadata (JSON) and binary keys
   - **In-Memory**: `ChannelStore` - ConcurrentDictionary storage
   - **Key Pattern**: `channel:{id}` (metadata), `channel:key:{id}` (binary symmetric key)

**Benefits**:
- Automatic expiration management (TTL)
- Persistence across node restarts
- Distributed rate limiting
- Production-ready scalability
- Graceful fallback to in-memory if Redis unavailable

**Testing**:
- `docs/testing/redis-testing-guide.md` - Comprehensive Redis testing guide
- `docs/testing/docker-compose-quick-start.md` - Docker Compose quick start

### PostgreSQL Node Registry (✅ Implemented)

**Multi-Instance Architecture**: Each node has its own isolated PostgreSQL database.

```yaml
# docker-compose.yml
postgres-node-a:
  port: 5432
  database: prism_node_a_registry
  user: prism_user_a
  volume: postgres-data-node-a

postgres-node-b:
  port: 5433
  database: prism_node_b_registry
  user: prism_user_b
  volume: postgres-data-node-b
```

**Entity Framework Core**:
- **Version**: 8.0.10 with Npgsql provider
- **Migrations**: Automatic application on startup
- **Connection Resiliency**: 3 retries with 5-second delay
- **Design-Time Factory**: `PrismDbContextFactory` for migrations

**Database Schema** (28 main tables):

1. **`research_nodes`** - Node registry
```sql
CREATE TABLE research_nodes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    node_name text NOT NULL,
    certificate text NOT NULL,
    certificate_fingerprint text NOT NULL UNIQUE,  -- SHA-256 natural key
    node_url text,
    status integer NOT NULL,  -- AuthorizationStatus enum
    node_access_level integer NOT NULL,  -- NodeAccessTypeEnum
    contact_info text,
    institution_details jsonb,  -- JSON metadata
    registered_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    last_authenticated_at timestamptz,
    metadata jsonb
);
```

2. **`research`** - Research projects
```sql
CREATE TABLE research (
    id uuid PRIMARY KEY,
    research_node_id uuid REFERENCES research_nodes(id) ON DELETE CASCADE,
    title text NOT NULL,
    description text NOT NULL,
    start_date timestamptz NOT NULL,
    end_date timestamptz,
    status text NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

3. **`volunteers`** - Study participants
```sql
CREATE TABLE volunteers (
    volunteer_id uuid PRIMARY KEY,
    research_node_id uuid REFERENCES research_nodes(id) ON DELETE CASCADE,
    volunteer_code text NOT NULL UNIQUE,  -- Anonymized identifier
    birth_date timestamptz NOT NULL,
    gender text NOT NULL,
    blood_type text NOT NULL,
    height real,
    weight real,
    medical_history text NOT NULL,
    consent_status text NOT NULL,
    registered_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

4. **`researchers`** - Principal investigators and team members
```sql
CREATE TABLE researchers (
    id uuid PRIMARY KEY,
    research_node_id uuid REFERENCES research_nodes(id) ON DELETE CASCADE,
    name text NOT NULL,
    email text NOT NULL,
    role text NOT NULL,
    institution text NOT NULL,
    credentials text NOT NULL,
    created_at timestamptz NOT NULL
);
```

5. **`applications`** - Data collection software (many-to-many with research via research_application)
```sql
CREATE TABLE applications (
    application_id uuid PRIMARY KEY,
    app_name text NOT NULL,
    url text NOT NULL,
    description text NOT NULL,
    additional_info text NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

6. **`devices`** - Hardware devices (many-to-many with research via research_device)
```sql
CREATE TABLE devices (
    device_id uuid PRIMARY KEY,
    device_name text NOT NULL,
    manufacturer text NOT NULL,
    model text NOT NULL,
    additional_info text NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

7. **`sensors`** - Individual sensors on devices
```sql
CREATE TABLE sensors (
    id uuid PRIMARY KEY,
    device_id uuid REFERENCES devices(id) ON DELETE CASCADE,
    name text NOT NULL,
    sensor_type text NOT NULL,
    unit text NOT NULL,
    min_value real NOT NULL,
    max_value real NOT NULL,
    resolution real NOT NULL,
    created_at timestamptz NOT NULL
);
```

8. **`record_sessions`** - Data collection sessions
```sql
CREATE TABLE record_sessions (
    id uuid PRIMARY KEY,
    research_id uuid REFERENCES research(id) ON DELETE CASCADE,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    application_id uuid REFERENCES applications(id) ON DELETE CASCADE,
    session_date timestamptz NOT NULL,
    duration_seconds integer NOT NULL,
    session_notes text NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

9. **`records`** - Individual data records
```sql
CREATE TABLE records (
    id uuid PRIMARY KEY,
    record_session_id uuid REFERENCES record_sessions(id) ON DELETE CASCADE,
    record_type text NOT NULL,
    start_timestamp timestamptz NOT NULL,
    end_timestamp timestamptz NOT NULL,
    quality_score real NOT NULL,
    metadata jsonb,  -- PostgreSQL JSON
    created_at timestamptz NOT NULL
);
```

10. **`record_channels`** - Signal channels in records
```sql
CREATE TABLE record_channels (
    id uuid PRIMARY KEY,
    record_id uuid REFERENCES records(id) ON DELETE CASCADE,
    sensor_id uuid REFERENCES sensors(id) ON DELETE CASCADE,
    signal_type text NOT NULL,
    file_url text NOT NULL,
    sampling_rate real NOT NULL,
    samples_count integer NOT NULL,
    start_timestamp timestamptz NOT NULL,
    annotations jsonb,  -- PostgreSQL JSON
    created_at timestamptz NOT NULL
);
```

11. **`target_areas`** - Body areas measured (SNOMED CT integration)
```sql
CREATE TABLE target_areas (
    id uuid PRIMARY KEY,
    record_channel_id uuid REFERENCES record_channels(id) ON DELETE CASCADE,
    body_structure_code text REFERENCES snomed_body_structures(snomed_code),
    laterality_code text REFERENCES snomed_lateralities(snomed_code),
    topographical_modifier_code text REFERENCES snomed_topographical_modifiers(snomed_code),
    description text NOT NULL,
    created_at timestamptz NOT NULL
);
```

12-15. **SNOMED CT Tables** - Medical terminology hierarchies
```sql
CREATE TABLE snomed_lateralities (
    snomed_code text PRIMARY KEY,
    display_name text NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL
);

CREATE TABLE snomed_topographical_modifiers (
    snomed_code text PRIMARY KEY,
    display_name text NOT NULL,
    category text NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL
);

CREATE TABLE snomed_body_regions (
    snomed_code text PRIMARY KEY,
    display_name text NOT NULL,
    parent_region_code text REFERENCES snomed_body_regions(snomed_code) ON DELETE RESTRICT,
    description text NOT NULL,
    is_active boolean NOT NULL
);

CREATE TABLE snomed_body_structures (
    snomed_code text PRIMARY KEY,
    body_region_code text REFERENCES snomed_body_regions(snomed_code) ON DELETE CASCADE,
    display_name text NOT NULL,
    structure_type text NOT NULL,
    parent_structure_code text REFERENCES snomed_body_structures(snomed_code) ON DELETE RESTRICT,
    description text NOT NULL,
    is_active boolean NOT NULL
);
```

16. **`snomed_severity_codes`** - SNOMED CT severity classifications
```sql
CREATE TABLE snomed_severity_codes (
    code text PRIMARY KEY,
    display_name text NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

17. **`clinical_conditions`** - Clinical condition catalog
```sql
CREATE TABLE clinical_conditions (
    snomed_code text PRIMARY KEY,
    display_name text NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

18. **`clinical_events`** - Clinical event catalog
```sql
CREATE TABLE clinical_events (
    snomed_code text PRIMARY KEY,
    display_name text NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

19. **`medications`** - Medication catalog with ANVISA codes
```sql
CREATE TABLE medications (
    snomed_code text PRIMARY KEY,
    medication_name text NOT NULL,
    active_ingredient text NOT NULL,
    anvisa_code text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

20. **`allergy_intolerances`** - Allergy/intolerance catalog
```sql
CREATE TABLE allergy_intolerances (
    snomed_code text PRIMARY KEY,
    category text NOT NULL,
    substance_name text NOT NULL,
    type text NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

21. **`vital_signs`** - Volunteer vital signs measurements
```sql
CREATE TABLE vital_signs (
    id uuid PRIMARY KEY,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    record_session_id uuid REFERENCES record_sessions(id) ON DELETE CASCADE,
    systolic_bp real,
    diastolic_bp real,
    heart_rate real,
    respiratory_rate real,
    temperature real,
    oxygen_saturation real,
    weight real,
    height real,
    bmi real,
    measurement_datetime timestamptz NOT NULL,
    measurement_context text NOT NULL,
    recorded_by uuid REFERENCES researchers(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

22. **`volunteer_allergy_intolerances`** - Volunteer-specific allergies
```sql
CREATE TABLE volunteer_allergy_intolerances (
    id uuid PRIMARY KEY,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    allergy_intolerance_snomed_code text REFERENCES allergy_intolerances(snomed_code) ON DELETE RESTRICT,
    criticality text NOT NULL,
    clinical_status text NOT NULL,
    manifestations jsonb NOT NULL,
    onset_date timestamptz,
    last_occurrence timestamptz,
    verification_status text NOT NULL,
    recorded_by uuid REFERENCES researchers(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

23. **`volunteer_medications`** - Volunteer medications
```sql
CREATE TABLE volunteer_medications (
    id uuid PRIMARY KEY,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    medication_snomed_code text REFERENCES medications(snomed_code) ON DELETE RESTRICT,
    condition_id uuid REFERENCES volunteer_clinical_conditions(id) ON DELETE RESTRICT,
    dosage text NOT NULL,
    frequency text NOT NULL,
    route text NOT NULL,
    start_date timestamptz NOT NULL,
    end_date timestamptz,
    status text NOT NULL,
    notes text NOT NULL,
    recorded_by uuid REFERENCES researchers(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

24. **`volunteer_clinical_conditions`** - Volunteer diagnoses
```sql
CREATE TABLE volunteer_clinical_conditions (
    id uuid PRIMARY KEY,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    snomed_code text REFERENCES clinical_conditions(snomed_code) ON DELETE RESTRICT,
    clinical_status text NOT NULL,
    onset_date timestamptz,
    abatement_date timestamptz,
    severity_code text REFERENCES snomed_severity_codes(code) ON DELETE RESTRICT,
    verification_status text NOT NULL,
    clinical_notes text NOT NULL,
    recorded_by uuid REFERENCES researchers(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

25. **`volunteer_clinical_events`** - Volunteer clinical events
```sql
CREATE TABLE volunteer_clinical_events (
    id uuid PRIMARY KEY,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    event_type text NOT NULL,
    snomed_code text REFERENCES clinical_events(snomed_code) ON DELETE RESTRICT,
    event_datetime timestamptz NOT NULL,
    duration_minutes integer,
    severity_code text REFERENCES snomed_severity_codes(code) ON DELETE RESTRICT,
    numeric_value real,
    value_unit text,
    characteristics jsonb NOT NULL,
    target_area_id uuid REFERENCES target_areas(id) ON DELETE RESTRICT,
    record_session_id uuid REFERENCES record_sessions(id) ON DELETE RESTRICT,
    recorded_by uuid REFERENCES researchers(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
```

26-29. **Join Tables** - Many-to-many relationships
```sql
CREATE TABLE research_application (
    research_id uuid REFERENCES research(id) ON DELETE CASCADE,
    application_id uuid REFERENCES applications(application_id) ON DELETE CASCADE,
    role text NOT NULL,
    added_at timestamptz NOT NULL,
    removed_at timestamptz,
    configuration text,
    PRIMARY KEY (research_id, application_id)
);

CREATE TABLE research_device (
    research_id uuid REFERENCES research(id) ON DELETE CASCADE,
    device_id uuid REFERENCES devices(device_id) ON DELETE CASCADE,
    role text NOT NULL,
    added_at timestamptz NOT NULL,
    removed_at timestamptz,
    calibration_status text NOT NULL,
    last_calibration_date timestamptz,
    PRIMARY KEY (research_id, device_id)
);

CREATE TABLE research_volunteers (
    research_id uuid REFERENCES research(id) ON DELETE CASCADE,
    volunteer_id uuid REFERENCES volunteers(volunteer_id) ON DELETE CASCADE,
    enrollment_status text NOT NULL,
    consent_date timestamptz NOT NULL,
    consent_version text NOT NULL,
    exclusion_reason text,
    enrolled_at timestamptz NOT NULL,
    withdrawn_at timestamptz,
    PRIMARY KEY (research_id, volunteer_id)
);

CREATE TABLE research_researchers (
    research_id uuid REFERENCES research(id) ON DELETE CASCADE,
    researcher_id uuid REFERENCES researchers(id) ON DELETE CASCADE,
    role text NOT NULL,
    joined_at timestamptz NOT NULL,
    left_at timestamptz,
    PRIMARY KEY (research_id, researcher_id)
);
```

**Repository Pattern** (`INodeRepository`):
- All methods use **Guid Id** (not string NodeId)
- Certificate fingerprint-based lookups for identification
- Re-registration support (updates existing node if certificate matches)
- Methods: `GetByIdAsync`, `GetByCertificateFingerprintAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

**Service Layer**:
- `PostgreSqlNodeRegistryService` - Production implementation with PostgreSQL
- `NodeRegistryService` - In-memory fallback for testing
- Feature flag: `UsePostgreSqlForNodes` (default: true in NodeA/NodeB profiles)

**Benefits**:
- Persistent node registry across restarts
- ACID transactions for data integrity
- Production-ready relational database
- Automatic migrations on startup
- Graceful fallback to in-memory if database unavailable

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
3. **Identify**: POST `/api/channel/identify` with NodeId (string) + certificate + signature
4. **Server processes**:
   - Calculates certificate fingerprint (SHA-256 hash)
   - Looks up node by certificate fingerprint (natural key)
   - If found: Returns `NodeStatusResponse` with both NodeId (string) and RegistrationId (Guid)
   - Stores `IdentifiedNodeId` (Guid) in `ChannelContext` for subsequent phases
5. **Response**:
   - Unknown node → `isKnown=false`, `registrationId=null`, registration URL provided
   - Known, Pending → `isKnown=true`, `registrationId={guid}`, `status=Pending`, `nextPhase=null`
   - Known, Authorized → `isKnown=true`, `registrationId={guid}`, `status=Authorized`, `nextPhase="phase3_authenticate"`

**Testing Helper**:
- `/api/testing/complete-phase1-phase2` - Combines Phase 1+2 in single call for easier manual testing

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
- `docs/architecture/phase4-session-management.md` - Phase 4 session architecture
- `docs/development/persistence-architecture.md` - Redis and PostgreSQL persistence architecture
- `docs/development/persistence-implementation-roadmap.md` - 15-day implementation plan
- `docs/testing/redis-testing-guide.md` - Comprehensive Redis testing guide
- `docs/testing/docker-compose-quick-start.md` - Docker Compose quick start guide
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

### ⚠️ Docker Network Issues (Resolved 2025-10-08)

**Issue**: PostgreSQL connection fails with "Name or service not known" when running in Docker.

**Root Cause**: PostgreSQL/Redis containers running on different Docker network than application containers.
- Old network: `interoperableresearchnode_irn-network` (legacy docker-compose.yml)
- New network: `irn-network` (persistence layer)

**Solution**: Ensure all containers are on the same network:
```bash
# 1. Check container networks
docker ps --format "table {{.Names}}\t{{.Networks}}"

# 2. If networks don't match, restart persistence layer
docker-compose -f docker-compose.persistence.yml down
docker-compose -f docker-compose.persistence.yml up -d

# 3. Restart application containers
docker-compose -f docker-compose.application.yml restart
```

**Prevention**: Always use the separated compose files (persistence + application) instead of the legacy docker-compose.yml.

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

### ✅ Completed Features (October 2025)

**Phase 1-4 Handshake Protocol:**
- ✅ **Phase 1**: Encrypted channel establishment (ECDH + AES-256-GCM) + Redis/In-Memory storage
- ✅ **Phase 2**: Node identification and registration (X.509 certificates)
- ✅ **Phase 3**: Challenge-response mutual authentication (RSA signatures)
- ✅ **Phase 4**: Session management and access control (Bearer tokens + capabilities) + Redis/In-Memory storage

**Persistence Layer:**
- ✅ **Redis Persistence**: Multi-instance architecture with automatic TTL (sessions + channels)
- ✅ **PostgreSQL Persistence**: 28 tables with complete clinical data model
- ✅ **Generic Repository Pattern**: `BaseRepository<TEntity, TKey>` with domain-specific extensions
- ✅ **Complete Service Layer**: `BaseService<TEntity, TKey>` with domain-specific business logic

**Clinical Data Model (October 8, 2025):**
- ✅ **10 New Clinical Entities**: Conditions, Events, Medications, Allergies, Vital Signs
- ✅ **SNOMED CT Integration**: Severity codes, clinical terminologies
- ✅ **HL7 FHIR Alignment**: Clinical data following healthcare interoperability standards
- ✅ **Complete Migration**: `CompleteSchema` migration with 28 tables (applied successfully)
- ✅ **6 Clinical Services**: Full CRUD operations with domain-specific methods
  - `ClinicalConditionService` - Active conditions, search by name
  - `ClinicalEventService` - Active events, search by name
  - `MedicationService` - ANVISA code lookup, search by name/ingredient
  - `AllergyIntoleranceService` - Filter by category/type
  - `VitalSignsService` - Volunteer/session/date range filtering
  - `VolunteerClinicalService` - Aggregate service with clinical summary

**Architecture Improvements:**
- ✅ **Generic Base Pattern**: Reusable `IBaseRepository<TEntity, TKey>` and `IServiceBase<TEntity, TKey>`
- ✅ **Domain-Driven Design**: Organized by business contexts (Node, Research, Volunteer, Clinical, etc.)
- ✅ **Clean Architecture**: Separation of concerns (Domain → Core → Data → Service → API)
- ✅ **Complete Service Layer**: All 28 entities have services registered in DI container

### 🔧 Immediate Tasks (Current Sprint)

**1. ✅ Fix Database Migration Issue (COMPLETED - October 8, 2025):**
- ✅ ~~Cleaned PostgreSQL databases and reapplied CompleteSchema migration~~
- ✅ ~~Verified all 28 tables created correctly~~
- ✅ ~~Tested data persistence across container restarts~~
- ✅ ~~Migration `20251008152728_CompleteSchema` applied successfully~~
- ✅ ~~Applications running on ports 5000 (Node A) and 5001 (Node B)~~

**2. ✅ Complete Clinical Services (COMPLETED - October 8, 2025):**
- ✅ ~~Create service interfaces for clinical entities~~
  - ✅ `IClinicalConditionService` - Clinical condition catalog operations
  - ✅ `IClinicalEventService` - Clinical event catalog operations
  - ✅ `IMedicationService` - Medication catalog with ANVISA code lookup
  - ✅ `IAllergyIntoleranceService` - Allergy/intolerance catalog by category/type
  - ✅ `IVitalSignsService` - Vital signs with volunteer/session filtering
  - ✅ `IVolunteerClinicalService` - Aggregate service for volunteer clinical data
- ✅ ~~Implement services extending `BaseService<TEntity, TKey>`~~
- ✅ ~~Register services in `Program.cs` DI container (6 services + 9 repositories)~~
- ✅ ~~Build solution successfully (0 errors, 20 pre-existing warnings)~~

**Database Schema Verified:**
- 30 tables total (28 main + 2 system)
- All foreign key constraints working
- Proper indexes on frequently queried columns
- JSONB columns for flexible metadata (manifestations, characteristics)

### 🚀 Phase 5 - Federated Queries (Next Major Feature)

**Query Endpoints:**
- [ ] `POST /api/query/execute` - Execute federated query (requires `query:read`)
- [ ] `POST /api/query/aggregate` - Aggregate query across nodes (requires `query:aggregate`)
- [ ] `GET /api/query/{queryId}/status` - Get query status
- [ ] `GET /api/query/{queryId}/results` - Get query results

**Data Submission Endpoints:**
- [ ] `POST /api/data/submit` - Submit research data (requires `data:write`)
- [ ] `GET /api/data/{dataId}` - Get data by ID (requires `query:read`)
- [ ] `DELETE /api/data/{dataId}` - Delete owned data (requires `data:delete`)

**Query Federation Logic:**
- [ ] Forward queries to connected nodes
- [ ] Aggregate results from multiple nodes
- [ ] Handle node failures and timeouts
- [ ] Cache federated query results

**Reference Architecture:**
- See `docs/architecture/phase4-session-management.md` for similar patterns

### 🏗️ Infrastructure Improvements (Production Readiness)

**Observability:**
- [ ] Structured logging with Serilog
- [ ] Distributed tracing with OpenTelemetry
- [ ] Prometheus metrics for monitoring
- [ ] Health check endpoints for all dependencies

**Scalability:**
- [ ] Redis Sentinel for high availability
- [ ] PostgreSQL read replicas
- [ ] API versioning (`/api/v1/...`)

**Code Quality:**
- [ ] Unit of Work pattern for transactional consistency
- [ ] Result<T> pattern for better error handling
- [ ] Specification pattern for complex queries

### 📚 Documentation Tasks

- [ ] Update `docs/testing/manual-testing-guide.md` with dotnet test commands
- [ ] Create `docs/testing/test-troubleshooting.md` for common test failures
- [ ] Document generic base pattern in `docs/architecture/repository-pattern.md`
- [ ] Create `docs/architecture/service-layer.md`
- [ ] Update ER diagram with clinical entities

## Reference

**Main documentation**: `docs/README.md`

**Protocol specification**: `docs/architecture/handshake-protocol.md`

**Testing guide**: `docs/testing/manual-testing-guide.md`

**Implementation roadmap**: `docs/development/implementation-roadmap.md`
