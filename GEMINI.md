# PRISM - Interoperable Research Node (IRN)

## Project Overview

The Interoperable Research Node (IRN) is the core component of the PRISM (Project Research Interoperability and Standardization Model) framework. It is a .NET 8 project designed to create a federated network of standardized, discoverable, and accessible research data, breaking down data silos in biomedical research. The project uses Docker for containerization, with PostgreSQL for data persistence and Redis for caching sessions and channels. The architecture is based on a microservices approach, with two nodes (`node-a` and `node-b`) that can communicate with each other.

## Building and Running

The project can be built and run using Docker.

**Key commands:**

*   **Start persistence layer (PostgreSQL + Redis):**
    ```bash
    docker-compose -f docker-compose.persistence.yml up -d
    ```
*   **Start application layer (Node A + Node B):**
    ```bash
    docker-compose -f docker-compose.application.yml up -d
    ```
*   **Run all automated tests:**
    ```bash
    dotnet test
    ```

## Development Conventions

*   **Code Style:** The project follows the standard C# and .NET coding conventions.
*   **Testing:** The project uses xUnit for testing. The tests are located in the `Bioteca.Prism.InteroperableResearchNode.Test` project.
*   **Database:** The project uses Entity Framework Core for database migrations. The migrations are located in the `Bioteca.Prism.Data/Migrations` directory.
*   **API Documentation:** The project uses Swagger/OpenAPI for API documentation. The Swagger UI can be accessed at `http://localhost:5000/swagger` for Node A and `http://localhost:5001/swagger` for Node B.

---

# CLAUDE.md - InteroperableResearchNode

This file provides guidance to Claude Code when working with the **InteroperableResearchNode** backend.

---

## Project Overview

**Interoperable Research Node (IRN)** - Core backend component of the PRISM federated framework for biomedical research data. Implements a secure 4-phase handshake protocol for node-to-node communication with cryptographic authentication.

**Current Status**: Phase 4 Complete + Clinical Data Model (28 tables) + Generic Repository Pattern
**Test Status**: 73/75 tests passing (97.3%)

---

## Quick Start

### Local Development

```bash
# Build solution
dotnet build Bioteca.Prism.InteroperableResearchNode.sln

# Run Node A (port 5000)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeA

# Run Node B (port 5001)
dotnet run --project Bioteca.Prism.InteroperableResearchNode --launch-profile NodeB

# Run all tests
dotnet test Bioteca.Prism.InteroperableResearchNode.Test/*.csproj

# Run end-to-end test (Phases 1→2→3→4)
bash test-phase4.sh
```

### Docker Multi-Node (Recommended)

```bash
# 1. Start persistence layer (PostgreSQL, Redis, pgAdmin, Azurite)
docker-compose -f docker-compose.persistence.yml up -d

# 2. Start application layer (Node A, Node B)
docker-compose -f docker-compose.application.yml up -d

# 3. View logs
docker logs -f irn-node-a

# 4. Rebuild after code changes
docker-compose -f docker-compose.application.yml up -d --build

# 5. Stop applications (data persists)
docker-compose -f docker-compose.application.yml down
```

**Access Points**:
- Node A Swagger: http://localhost:5000/swagger
- Node B Swagger: http://localhost:5001/swagger
- pgAdmin: http://localhost:5050 (admin@prism.local / prism-admin-password-2025)

---

## Architecture Summary

### Clean Architecture Pattern

```
Domain Layer (Entities, DTOs, Enums)
    ↓
Core Layer (Interfaces, Base Implementations)
    ↓
Data Layer (PostgreSQL + Redis, Repositories)
    ↓
Service Layer (Business Logic, Domain Services)
    ↓
API Layer (Controllers, Middleware, Filters)
```

### Two Authentication Systems

PRISM implements **two separate authentication systems** for different purposes:

#### 1. User Authentication (Human Researchers)

**Purpose**: Authenticate researchers accessing the system via web/mobile interfaces.

**Technology**:
- JWT Bearer tokens (RS256 signature)
- SHA512 password hashing
- Role-based access control

**Endpoints**:
- `POST /api/userauth/login` - User login
- `POST /api/userauth/refreshtoken` - Token refresh
- `POST /api/userauth/encrypt` - Password hashing utility

**Usage Pattern**:
```http
Authorization: Bearer {jwt_token}
```

**Details**: `docs/architecture/USER_SESSION_ARCHITECTURE.md`

#### 2. Node Session Authentication (Federated Nodes)

**Purpose**: Authenticate remote research nodes for federated data exchange.

**Technology**:
- 4-phase handshake protocol
- AES-256-GCM encryption
- Capability-based authorization

**Endpoints**: See "4-Phase Handshake Protocol" below

**Usage Pattern**:
```http
X-Channel-Id: {channel_id}
X-Session-Id: {session_token}
```

**Key Difference**: User auth uses HTTPS for transport security, while node auth uses end-to-end encryption (AES-256-GCM) over HTTPS for federated trust.

**Detailed Comparison**: `docs/architecture/USER_SESSION_ARCHITECTURE.md`

### 4-Phase Handshake Protocol

**Phase 1 - Encrypted Channel**: ECDH P-384 + AES-256-GCM + Perfect Forward Secrecy
- Endpoints: `/api/channel/open`, `/api/channel/initiate`
- **Details**: `docs/workflows/CHANNEL_FLOW.md`

**Phase 2 - Node Identification**: X.509 certificates + RSA-2048 signatures
- Node registry with approval workflow (Unknown → Pending → Authorized/Revoked)
- Endpoints: `/api/channel/identify`, `/api/node/register`, `/api/node/{id:guid}/status`
- **Details**: `docs/workflows/PHASE2_IDENTIFICATION_FLOW.md`

**Phase 3 - Mutual Authentication**: Challenge-response protocol
- 32-byte random challenges with 5-minute TTL
- Endpoints: `/api/node/challenge`, `/api/node/authenticate`
- **Details**: `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`

**Phase 4 - Session Management**: Bearer tokens + capability-based authorization
- Rate limiting (60 requests/minute), 1-hour TTL with renewal
- Endpoints: `/api/session/whoami`, `/api/session/renew`, `/api/session/revoke`, `/api/session/metrics`
- **Details**: `docs/workflows/PHASE4_SESSION_FLOW.md`

**Complete Protocol**: `docs/architecture/handshake-protocol.md`

### Dual-Identifier Architecture

**Three types of identifiers** for nodes:
1. **NodeId (string)** - Protocol-level identifier (e.g., "node-a")
2. **RegistrationId (Guid)** - Database primary key
3. **Certificate Fingerprint (SHA-256)** - Natural key for authentication

**Usage**:
- Phase 2 Identification: Send NodeId → Receive RegistrationId
- Administrative Operations: Use RegistrationId (Guid)
- Phase 3 Authentication: Use NodeId (string)

**Details**: `docs/architecture/NODE_IDENTIFIER_ARCHITECTURE.md`

### Generic Base Pattern

**Interfaces**:
- `IBaseRepository<TEntity, TKey>` - Generic CRUD operations
- `IServiceBase<TEntity, TKey>` - Generic business logic

**Benefits**: Code reusability, type safety, consistency across all entities

**Details**: `docs/architecture/GENERIC_BASE_PATTERN.md`

### Persistence Layer

**PostgreSQL 18**: Multi-instance (one per node)
- 28 tables with clinical data model
- EF Core 8.0.10 with migrations
- Connection resiliency (3 retries, 5-second delay)

**Redis 7.2**: Multi-instance for sessions and channels
- Automatic TTL management
- Distributed rate limiting
- Persistence across restarts

**Details**: `docs/development/PERSISTENCE_LAYER.md` (TODO)

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Runtime** | .NET 8.0, C# 12 | Modern, high-performance runtime |
| **Web Framework** | ASP.NET Core 8.0 | RESTful API endpoints |
| **ORM** | EF Core 8.0.10 + Npgsql | Database access and migrations |
| **Database** | PostgreSQL 18 | Relational data storage (28 tables) |
| **Cache** | Redis 7.2 | Session and channel persistence |
| **Cryptography** | ECDH P-384, AES-256-GCM, RSA-2048 | Secure communication |
| **Containerization** | Docker, Docker Compose | Multi-node orchestration |

---

## Project Structure (Simplified)

```
Bioteca.Prism.Domain/          # Entities, DTOs, Enums
Bioteca.Prism.Core/            # Interfaces, Base Classes
Bioteca.Prism.Data/            # Repositories, EF Core, Migrations
Bioteca.Prism.Service/         # Business Logic Services
Bioteca.Prism.InteroperableResearchNode/  # API Controllers
Bioteca.Prism.InteroperableResearchNode.Test/  # Integration Tests
docs/                          # Comprehensive Documentation
docker-compose.persistence.yml # Database/cache infrastructure
docker-compose.application.yml # Application containers
```

**Detailed Structure**: `docs/architecture/PROJECT_STRUCTURE.md` (TODO)

---

## Clinical Data Model (28 Tables)

**Core Entities** (10 tables):
- Research projects, volunteers, researchers
- Devices, sensors, applications
- Recording sessions, records, record channels, target areas

**SNOMED CT Terminologies** (4 tables):
- Body structures, body regions, lateralities, topographical modifiers, severity codes

**Clinical Data** (14 tables):
- Clinical conditions, events, medications, allergies
- Vital signs measurements
- Volunteer-specific clinical records
- Many-to-many relationship tables

**Standards**: HL7 FHIR-aligned with SNOMED CT integration

**Details**: `docs/components/INTEROPERABLE_RESEARCH_NODE.md`

---

## Database Commands

```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project Bioteca.Prism.Data \
  --startup-project Bioteca.Prism.InteroperableResearchNode

# Apply migrations
dotnet ef database update \
  --project Bioteca.Prism.Data \
  --startup-project Bioteca.Prism.InteroperableResearchNode

# Access PostgreSQL CLI
docker exec -it irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry

# Access Redis CLI
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a
```

---

## Testing

```bash
# Run all tests (73/75 passing - 97.3%)
dotnet test Bioteca.Prism.InteroperableResearchNode.Test/*.csproj

# Run specific test suite
dotnet test --filter "FullyQualifiedName~Phase4SessionManagementTests"

# Run with detailed output
dotnet test --verbosity detailed

# End-to-end test script (Phases 1→2→3→4)
bash test-phase4.sh
```

**Test Status**: `docs/PROJECT_STATUS.md`
**Testing Guide**: `docs/testing/manual-testing-guide.md`
**Redis Testing**: `docs/testing/redis-testing-guide.md`

---

## Configuration

### Environment Profiles

**NodeA** (`appsettings.NodeA.json`):
- Port: 5000
- PostgreSQL: `irn-postgres-node-a:5432`
- Redis: `irn-redis-node-a:6379`

**NodeB** (`appsettings.NodeB.json`):
- Port: 5001
- PostgreSQL: `irn-postgres-node-b:5433`
- Redis: `irn-redis-node-b:6380`

### Feature Flags

```json
{
  "FeatureFlags": {
    "UseRedisForSessions": true,    // Enable Redis for session storage
    "UseRedisForChannels": true,    // Enable Redis for channel storage
    "UsePostgreSqlForNodes": true   // Enable PostgreSQL for node registry
  },
  "HttpClient": {
    "TimeoutSeconds": 300  // 5 minutes
  }
}
```

---

## Service Registration (DI Container)

**Singleton Services** (shared state):
- `IEphemeralKeyService` - ECDH key generation
- `IChannelEncryptionService` - Crypto operations
- `INodeChannelClient` - HTTP client for handshakes
- `INodeRegistryService` - Node registry
- `IChallengeService` - Challenge-response auth
- `ISessionService` - Session lifecycle
- `IRedisConnectionService` - Redis connection management (conditional)
- `ISessionStore` - Session persistence (Redis or in-memory)
- `IChannelStore` - Channel persistence (Redis or in-memory)

**Scoped Services** (new instance per request):
- `PrismDbContext` - EF Core DbContext
- All repositories: `INodeRepository`, `IResearchRepository`, `IVolunteerRepository`, etc.
- All domain services extending `BaseService<TEntity, TKey>`

**Details**: `docs/development/SERVICE_REGISTRATION.md` (TODO)

---

## Security & Cryptography

**Phase 1 Security**:
- ECDH P-384: Ephemeral key exchange
- HKDF-SHA256: Key derivation
- AES-256-GCM: Authenticated encryption
- Perfect Forward Secrecy

**Phase 2 Security**:
- X.509 certificates
- SHA-256 certificate fingerprints
- RSA-2048 digital signatures

**Phase 3 Security**:
- 32-byte random challenges (5-minute TTL)
- RSA-2048 signature verification
- One-time use challenges (replay protection)

**Phase 4 Security**:
- GUID session tokens (1-hour TTL)
- Capability-based authorization (ReadOnly, ReadWrite, Admin)
- Rate limiting (60 req/min) via Redis Sorted Sets

**Complete Security Details**: `docs/SECURITY_OVERVIEW.md`

---

## Documentation Standards

**IMPORTANT: All documentation MUST be written in English**, including:
- Architecture documents
- API documentation
- Code comments
- README files
- Testing guides

When encountering Portuguese documentation, translate to English while preserving technical accuracy.

---

## Important Files & Locations

### Configuration
- `appsettings.json` - Base configuration
- `appsettings.NodeA.json` - Node A overrides (port 5000)
- `appsettings.NodeB.json` - Node B overrides (port 5001)
- `docker-compose.persistence.yml` - Database/cache layer
- `docker-compose.application.yml` - Application layer

### Documentation
- `docs/README.md` - Documentation index
- `docs/ARCHITECTURE_PHILOSOPHY.md` - PRISM architecture and design principles
- `docs/SECURITY_OVERVIEW.md` - Complete security architecture
- `docs/components/` - Detailed component descriptions
- `docs/workflows/` - Phase-by-phase implementation flows
- `docs/architecture/` - Technical specifications
- `docs/development/` - Development guides
- `docs/testing/` - Testing documentation
- `docs/PROJECT_STATUS.md` - Implementation status (v0.8.0)

### Testing
- `test-phase4.sh` - End-to-end test script (Phases 1→2→3→4)
- `Bioteca.Prism.InteroperableResearchNode.Test/` - Integration tests (73/75 passing)

---

## Known Issues

**Docker Network Configuration** (Resolved 2025-10-08):
- Ensure all containers on same `irn-network`
- Use separated compose files (persistence + application)

**Test Status** (2025-10-03):
- 73/75 tests passing (97.3%)
- 2 failing tests: RSA signature verification (known issue, non-blocking)

**Compiler Warnings**:
- `NodeRegistryService.cs:44` - Async method without await (intentional)

**Details**: `docs/KNOWN_ISSUES.md` (TODO)

---

## Development Guidelines

### Biomedical Data Standards
- Follow HL7 FHIR standards for health data
- Use SNOMED CT terminologies for clinical concepts
- Implement rigorous validation for biosignals
- Maintain detailed audit logs

### Security Requirements
- LGPD/GDPR compliance for privacy
- End-to-end encryption for all sensitive communications
- Role-based access control with capability-based authorization
- Audit all critical operations

### Code Conventions
- Services: `I{Name}Service` interface, `{Name}Service` implementation
- DTOs: `{Action}Request`, `{Action}Response`
- Repositories: Extend `BaseRepository<TEntity, TKey>`
- Services: Extend `BaseService<TEntity, TKey>`

---

## Next Steps

### Completed Features ✅
- Phase 1-4 handshake protocol
- Redis + PostgreSQL persistence
- Clinical data model (28 tables)
- Generic repository pattern
- Complete service layer

### In Progress 🚧
- Phase 5 (Federated Queries): Cross-node data querying
- Mobile app data submission
- Interface system protocol translation

### Planned 📋
- Production certificate management (Let's Encrypt)
- Observability stack (Prometheus + OpenTelemetry)
- Clinical trial management features

**Detailed Roadmap**: `docs/development/implementation-roadmap.md`

---

## Quick Reference Card

**Most Frequent Commands**:
```bash
# Start everything
docker-compose -f docker-compose.persistence.yml up -d
docker-compose -f docker-compose.application.yml up -d

# Run tests
dotnet test Bioteca.Prism.InteroperableResearchNode.Test/*.csproj

# View logs
docker logs -f irn-node-a

# Database migration
dotnet ef database update --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode

# Stop applications
docker-compose -f docker-compose.application.yml down
```

**Key Endpoints**:

*User Authentication*:
- User Login: `/api/userauth/login`
- Token Refresh: `/api/userauth/refreshtoken`

*Node Authentication (4-Phase Handshake)*:
- Phase 1: `/api/channel/open`, `/api/channel/initiate`
- Phase 2: `/api/channel/identify`, `/api/node/register`
- Phase 3: `/api/node/challenge`, `/api/node/authenticate`
- Phase 4: `/api/session/whoami`, `/api/session/renew`, `/api/session/revoke`

---

## Navigation

**For Detailed Information, See**:

- **Components**: `docs/components/INTEROPERABLE_RESEARCH_NODE.md`
- **Architecture**: `docs/ARCHITECTURE_PHILOSOPHY.md`
- **Security**: `docs/SECURITY_OVERVIEW.md`
- **User & Session Architecture**: `docs/architecture/USER_SESSION_ARCHITECTURE.md`
- **Workflows**: `docs/workflows/` (Phase 1-4 step-by-step guides)
- **Testing**: `docs/testing/manual-testing-guide.md`
- **Project Status**: `docs/PROJECT_STATUS.md`
- **Ecosystem Overview**: See root `../CLAUDE.md`

**For Step-by-Step Implementation**:
- User Authentication: `docs/architecture/USER_SESSION_ARCHITECTURE.md` (Section 3)
- Phase 1: `docs/workflows/CHANNEL_FLOW.md`
- Phase 2: `docs/workflows/PHASE2_IDENTIFICATION_FLOW.md`
- Phase 3: `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`
- Phase 4: `docs/workflows/PHASE4_SESSION_FLOW.md`