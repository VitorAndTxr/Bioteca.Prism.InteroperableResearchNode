# InteroperableResearchNode - Core Backend

**Component Type**: Backend Server
**Technology**: ASP.NET Core 8.0, C#, PostgreSQL 18, Redis 7.2
**Purpose**: Core backend node for secure, federated research data management

---

## Overview

The **InteroperableResearchNode (IRN)** is the cornerstone of the PRISM ecosystem. It implements a secure, cryptographic handshake protocol for federated research data exchange.

**Core Purpose**: Acts as the trusted gateway for each research institution, managing authentication, authorization, and secure storage of research data with federated query capabilities.

---

## Key Architectural Features

### Clean Architecture Pattern
- **Domain Layer** → **Core Layer** → **Data Layer** → **Service Layer** → **API Layer**
- Generic base repository and service pattern (`IBaseRepository<TEntity, TKey>`, `IServiceBase<TEntity, TKey>`)
- Domain-driven design with context-specific services

### 4-Phase Handshake Protocol (✅ Complete)

**Phase 1: Encrypted Channel Establishment**
- ECDH P-384 + AES-256-GCM + Perfect Forward Secrecy
- Endpoints: `/api/channel/open`, `/api/channel/initiate`

**Phase 2: Node Identification and Registration**
- X.509 certificates + RSA-2048 signatures
- Certificate fingerprint (SHA-256) as natural key
- Node registry with approval workflow (Unknown → Pending → Authorized/Revoked)
- Endpoints: `/api/channel/identify`, `/api/node/register`, `/api/node/{id:guid}/status`

**Phase 3: Challenge-Response Mutual Authentication**
- Cryptographic proof of private key possession
- 32-byte random challenges with 5-minute TTL
- One-time use challenges (replay protection)
- Endpoints: `/api/node/challenge`, `/api/node/authenticate`

**Phase 4: Session Management**
- Capability-based authorization (ReadOnly, ReadWrite, Admin)
- Rate limiting (60 requests/minute) using token bucket algorithm
- 1-hour session tokens with automatic TTL
- Endpoints: `/api/session/whoami`, `/api/session/renew`, `/api/session/revoke`, `/api/session/metrics`

### Dual-Identifier Architecture

The system uses THREE types of identifiers for nodes:

1. **NodeId (string)** - Protocol-level identifier
   - Human-readable (e.g., "hospital-research-node-001", "node-a")
   - Used in all request/response DTOs for external communication
   - Sent in Phase 2 identification and Phase 3 authentication

2. **RegistrationId / Id (Guid)** - Database primary key
   - Internal unique identifier (e.g., `f6cdb452-17a1-4d8f-9241-0974f80c56ef`)
   - Primary key in `research_nodes` table
   - Used for all database operations and administrative endpoints

3. **Certificate Fingerprint (SHA-256)** - Natural key
   - SHA-256 hash of the X.509 certificate bytes
   - Used for node lookups during identification and registration
   - Enforces uniqueness constraint (unique index in database)

**Usage Pattern**:
```
Phase 2 Identification → Send NodeId (string) → Receive RegistrationId (Guid)
Administrative Operations → Use RegistrationId (Guid)
Phase 3 Authentication → Use NodeId (string)
```

### Persistence Layer

**PostgreSQL 18**: Multi-instance architecture (one database per node)
- 28 tables with complete clinical data model
- EF Core 8.0.10 with migrations
- Connection resiliency (3 retries, 5-second delay)
- Generic repository pattern

**Redis 7.2**: Multi-instance for sessions and channels
- Automatic TTL management
- Distributed rate limiting
- Persistence across node restarts
- Graceful fallback to in-memory storage

### Clinical Data Model (HL7 FHIR-aligned)

**28 Main Tables**:

**Core Entities** (10 tables):
- Research projects, volunteers, researchers
- Devices, sensors, applications
- Recording sessions, records, record channels, target areas

**SNOMED CT Terminologies** (4 tables):
- Body structures, body regions
- Lateralities, topographical modifiers
- Severity codes

**Clinical Data** (14 tables):
- Clinical conditions catalog
- Clinical events catalog
- Medications (ANVISA integration)
- Allergy/intolerance catalog
- Vital signs measurements
- Volunteer-specific clinical data (conditions, events, medications, allergies)
- Many-to-many relationship tables

**Key Features**:
- SNOMED CT integration for medical terminologies
- HL7 FHIR alignment for interoperability
- JSONB columns for flexible metadata
- Hierarchical relationships (body structures, regions)
- Audit timestamps (created_at, updated_at)

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Runtime** | .NET 8.0, C# 12 | Modern, high-performance runtime |
| **Web Framework** | ASP.NET Core 8.0 | RESTful API endpoints |
| **ORM** | Entity Framework Core 8.0.10 | Database access and migrations |
| **Database** | PostgreSQL 18 | Relational data storage (multi-instance) |
| **Cache** | Redis 7.2 | Session and channel persistence (multi-instance) |
| **Cryptography** | ECDH P-384, AES-256-GCM, RSA-2048 | Secure communication |
| **Certificates** | X.509, SHA-256 | Node authentication |
| **Containerization** | Docker, Docker Compose | Multi-node orchestration |

---

## Generic Base Pattern

**Design Philosophy**: DRY principle with type-safe generic base classes.

### Core Interfaces

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

### Domain-Specific Extensions

```csharp
// Example: Volunteer Service
public interface IVolunteerService : IServiceBase<Volunteer, Guid>
{
    // Inherits all base CRUD methods
    // Add domain-specific methods:
    Task<List<Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default);
}
```

**Benefits**:
- ✅ Code reusability: Base CRUD operations implemented once
- ✅ Type safety: Generic constraints ensure proper usage
- ✅ Consistency: All repositories/services follow same pattern
- ✅ Extensibility: Easy to add domain-specific methods
- ✅ Maintainability: Changes to base pattern affect all implementations
- ✅ Testability: Mock base interfaces for unit testing

---

## Docker Multi-Instance Architecture

### Two-Layer Architecture

**Layer 1: Persistence** (`docker-compose.persistence.yml`)
- PostgreSQL Node A (port 5432) + Node B (port 5433)
- Redis Node A (port 6379) + Node B (port 6380)
- Azurite (ports 10000-10002) - Azure Storage Emulator
- pgAdmin (port 5050) - Database management UI

**Layer 2: Application** (`docker-compose.application.yml`)
- Node A (port 5000) - Research Node instance A
- Node B (port 5001) - Research Node instance B

**Key Features**:
- Named volumes with explicit names
- `restart: unless-stopped` policy
- Shared network `irn-network`
- Data persists even when containers are stopped
- Environment variables override appsettings.json for Docker networking

---

## Security & Cryptography

### Phase 1 - Encrypted Channel
- ECDH P-384 ephemeral key exchange
- HKDF-SHA256 key derivation
- AES-256-GCM symmetric encryption
- Perfect Forward Secrecy (ephemeral keys discarded after session)

### Phase 2 - Node Identification
- X.509 certificate-based identification
- SHA-256 certificate fingerprint as natural key
- RSA-2048 digital signatures
- PostgreSQL node registry with approval workflow

### Phase 3 - Mutual Authentication
- 32-byte random challenges (5-minute TTL)
- RSA-2048 signature verification
- Proof of private key possession
- One-time use challenges (replay protection)

### Phase 4 - Session Management
- 1-hour session tokens with automatic TTL
- Capability-based authorization (ReadOnly, ReadWrite, Admin)
- Rate limiting (60 requests/minute) via Redis Sorted Sets
- Session persistence across node restarts

---

## Biomedical Data Standards

### HL7 FHIR Alignment
- Research projects → ResearchStudy resource
- Volunteers → Patient resource
- Researchers → Practitioner resource
- Recording sessions → Observation resource
- Clinical conditions → Condition resource
- Medications → MedicationStatement resource
- Allergies → AllergyIntolerance resource
- Vital signs → Observation (vital-signs profile)

### SNOMED CT Integration
- Body structures (123037004 - Body structure)
- Lateralities (7771000 - Left, 24028007 - Right)
- Topographical modifiers (255561001 - Medial, 49370004 - Lateral)
- Severity codes (255604002 - Mild, 6736007 - Moderate, 24484000 - Severe)
- Clinical conditions (ICD-10/SNOMED CT codes)
- Clinical events (observable entities)

### Data Validation
- Rigorous validation for biosignals
- Capture and processing metadata
- Detailed access and modification logs
- LGPD/GDPR compliance

---

## Development Commands

```bash
# Build solution
dotnet build Bioteca.Prism.InteroperableResearchNode.sln

# Run locally (Node A on port 5000)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeA

# Run locally (Node B on port 5001)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeB

# Docker multi-node setup (separated architecture - recommended)
# 1. Start persistence layer (PostgreSQL, Redis, pgAdmin, Azurite)
docker-compose -f docker-compose.persistence.yml up -d

# 2. Start application layer (Node A, Node B)
docker-compose -f docker-compose.application.yml up -d

# 3. Check status
docker-compose -f docker-compose.application.yml ps

# 4. View logs
docker logs -f irn-node-a
docker logs -f irn-postgres-node-a
docker logs -f irn-redis-node-a

# 5. Rebuild after code changes
docker-compose -f docker-compose.application.yml up -d --build

# 6. Stop applications (data persists in persistence layer)
docker-compose -f docker-compose.application.yml down

# Access services:
# - Node A Swagger: http://localhost:5000/swagger
# - Node B Swagger: http://localhost:5001/swagger
# - pgAdmin: http://localhost:5050 (admin@prism.local / prism-admin-password-2025)
```

---

## Testing

```bash
# Run all automated tests (73/75 passing - 97.3%)
dotnet test Bioteca.Prism.InteroperableResearchNode.Test/Bioteca.Prism.InteroperableResearchNode.Test.csproj

# Run specific test suite
dotnet test --filter "FullyQualifiedName~Phase4SessionManagementTests"

# Run with detailed output
dotnet test --verbosity detailed

# End-to-end test script (Phases 1→2→3→4)
bash test-phase4.sh
```

### Test Status

**Overall: 73/75 tests passing (97.3% pass rate)** ✅

| Category | Passing | Total | Pass Rate |
|----------|---------|-------|-----------|
| Phase 1 (Channel Establishment) | 6/6 | 100% | ✅ |
| Certificate & Signature | 13/15 | 86.7% | ⚠️ |
| Phase 2 (Node Identification) | 6/6 | 100% | ✅ |
| Phase 3 (Mutual Authentication) | 5/5 | 100% | ✅ |
| Phase 4 (Session Management) | 8/8 | 100% | ✅ |
| Encrypted Channel Integration | 3/3 | 100% | ✅ |
| NodeChannelClient | 7/7 | 100% | ✅ |
| Security & Edge Cases | 23/23 | 100% | ✅ |

---

## Database Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode

# Apply migrations
dotnet ef database update --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode

# Access PostgreSQL CLI
docker exec -it irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry

# Access Redis CLI
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a
```

---

## Integration with PRISM Ecosystem

The InteroperableResearchNode integrates with the PRISM framework:

1. **Node-to-Node Communication**: Secure handshake protocol for federated queries
2. **Mobile App Integration**: HTTPS endpoints for data submission
3. **Device Registry**: Registers sEMG devices and other hardware
4. **Session Recording**: Stores therapeutic session metadata
5. **Data Federation**: Enables cross-institutional research queries (Phase 5 - planned)
6. **Standards Compliance**: HL7 FHIR and SNOMED CT for interoperability

---

## Documentation References

For detailed information, see:

- **Main Documentation**: `InteroperableResearchNode/CLAUDE.md`
- **Project Structure**: `docs/architecture/PROJECT_STRUCTURE.md`
- **Generic Base Pattern**: `docs/architecture/GENERIC_BASE_PATTERN.md`
- **Node Identifiers**: `docs/architecture/NODE_IDENTIFIER_ARCHITECTURE.md`
- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Phase 4 Sessions**: `docs/architecture/phase4-session-management.md`
- **Persistence**: `docs/development/PERSISTENCE_LAYER.md`
- **Docker Setup**: `docs/development/DOCKER-SETUP.md`
- **Testing Guide**: `docs/testing/manual-testing-guide.md`
- **Project Status**: `docs/PROJECT_STATUS.md`
- **Workflows**: `docs/workflows/` (Phase 1-4 flows)
