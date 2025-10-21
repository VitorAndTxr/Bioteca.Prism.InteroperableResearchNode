# IRN Documentation - Interoperable Research Node

This folder contains all technical and development documentation for the IRN project.

> **📌 Translation Status:** This documentation is being standardized to English. See [DOCUMENTATION_TRANSLATION_STATUS.md](DOCUMENTATION_TRANSLATION_STATUS.md) for translation progress.

## 📊 Project Status

**Last Updated:** 2025-10-07
**Version:** 0.8.0

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ **Complete + Redis** | Encrypted channel with ephemeral ECDH + Redis persistence |
| **Phase 2** | ✅ **Complete + PostgreSQL** | Node identification, X.509 certificates + PostgreSQL node registry |
| **Phase 3** | ✅ **Complete** | Mutual authentication with challenge/response |
| **Phase 4** | ✅ **Complete + Redis** | Session management with capabilities + Redis persistence |
| **PostgreSQL Persistence** | ✅ **Complete** | Multi-instance PostgreSQL 18 + EF Core 8.0 + Guid architecture |
| **Redis Persistence** | ✅ **Complete** | Multi-instance Redis for sessions and channels |
| **Phase 5** | 📋 **Next** | Federated queries with session-based authorization |

## Documentation Structure

### 1. Architecture and Design
- [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md) - **⭐ MAIN** - Complete handshake protocol (Phases 1-4)
- [`architecture/phase4-session-management.md`](architecture/phase4-session-management.md) - **⭐ NEW** - Phase 4 session architecture
- [`architecture/node-communication.md`](architecture/node-communication.md) - Inter-node communication architecture
- [`architecture/session-management.md`](architecture/session-management.md) - Session management between nodes

### 2. Testing
- [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md) - **⭐ MAIN** - Complete manual testing guide (Phases 1-4)
- [`testing/redis-testing-guide.md`](testing/redis-testing-guide.md) - **⭐** - Comprehensive Redis testing guide
- [`testing/docker-compose-quick-start.md`](testing/docker-compose-quick-start.md) - **⭐** - Docker Compose quick start
- ~~[`testing/phase3-testing-endpoints.md`](testing/phase3-testing-endpoints.md)~~ - Deprecated (covered in manual-testing-guide.md)
- ~~[`testing/TEST-SUITE-STATUS-2025-10-02.md`](testing/TEST-SUITE-STATUS-2025-10-02.md)~~ - Outdated (see PROJECT_STATUS.md)

### 3. Development
- [`development/DOCKER-SETUP.md`](development/DOCKER-SETUP.md) - **⭐ NEW** - Comprehensive Docker setup guide (PostgreSQL + Redis)
- [`development/persistence-architecture.md`](development/persistence-architecture.md) - **⭐** - Redis and PostgreSQL architecture
- [`development/persistence-implementation-roadmap.md`](development/persistence-implementation-roadmap.md) - 15-day implementation plan
- [`development/ai-assisted-development.md`](development/ai-assisted-development.md) - AI-assisted development patterns
- [`development/implementation-roadmap.md`](development/implementation-roadmap.md) - Implementation roadmap
- ~~[`development/debugging-docker.md`](development/debugging-docker.md)~~ - Deprecated (see DOCKER-SETUP.md)
- ~~[`development/phase3-authentication-plan.md`](development/phase3-authentication-plan.md)~~ - Historical (Phase 3 complete)

### 4. API and Protocols ⚠️
- ~~[`api/node-endpoints.md`](api/node-endpoints.md) - Node communication endpoints~~ (Outdated - see Swagger)
- ~~[`api/message-formats.md`](api/message-formats.md) - Message and payload formats~~ (Outdated - see Models)

## 🚀 Quick Start

### To Test the System

1. **Start Docker environment:**
   ```bash
   # RECOMMENDED: Separated architecture
   # 1. Start persistence layer (PostgreSQL + Redis + pgAdmin + Azurite)
   docker-compose -f docker-compose.persistence.yml up -d

   # 2. Start application layer (Node A + Node B)
   docker-compose -f docker-compose.application.yml up -d

   # OR: Legacy single-file approach
   docker-compose up -d
   ```

2. **Run automated tests:**
   ```bash
   # All phases (Phases 1-4)
   dotnet test

   # Specific test suite
   dotnet test --filter "FullyQualifiedName~Phase4SessionManagementTests"
   ```

3. **End-to-end test:**
   ```bash
   # Complete handshake flow (Phases 1→2→3→4)
   bash test-phase4.sh
   ```

4. **Manual testing and debugging:**
   - Read [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md)
   - Read [`testing/redis-testing-guide.md`](testing/redis-testing-guide.md)

### For Development

1. **Understand the architecture:**
   - Read [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md)
   - Read [`architecture/phase4-session-management.md`](architecture/phase4-session-management.md)

2. **Debug in Visual Studio:**
   - Select profile "Node A (Debug)" or "Node B (Debug)"
   - Suggested breakpoints in [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md)

3. **Available endpoints:**
   - Swagger: http://localhost:5000/swagger (Node A)
   - Swagger: http://localhost:5001/swagger (Node B)

## 📚 Implemented Endpoints

### Phase 1: Encrypted Channel
- `POST /api/channel/open` - Accept channel request (server)
- `POST /api/channel/initiate` - Initiate channel with remote node (client)
- `GET /api/channel/{channelId}` - Channel information
- `GET /api/channel/health` - Health check

### Phase 2: Identification and Authorization
- `POST /api/channel/identify` - Identify node with certificate (encrypted, returns RegistrationId Guid)
- `POST /api/node/register` - Register new unknown node (encrypted, certificate fingerprint as natural key)
- `GET /api/node/nodes` - List registered nodes (admin)
- `PUT /api/node/{id:guid}/status` - **UPDATED**: Update node status (admin, uses RegistrationId Guid)

### Phase 3: Mutual Authentication
- `POST /api/node/challenge` - Request authentication challenge (encrypted)
- `POST /api/node/authenticate` - Authenticate with signed challenge (encrypted)

### Phase 4: Session Management
- `POST /api/session/whoami` - Get current session info (encrypted)
- `POST /api/session/renew` - Renew session TTL (encrypted)
- `POST /api/session/revoke` - Revoke/logout session (encrypted)
- `POST /api/session/metrics` - Get session metrics (encrypted, requires admin capability)

### Testing (Dev/NodeA/NodeB environments only)
- `POST /api/testing/generate-certificate` - Generate self-signed certificate
- `POST /api/testing/sign-data` - Sign data with certificate
- `POST /api/testing/verify-signature` - Verify signature
- `POST /api/testing/generate-node-identity` - Generate complete identity
- `POST /api/testing/encrypt-payload` - Encrypt payload with channel
- `POST /api/testing/decrypt-payload` - Decrypt payload with channel

## 🔧 Technologies Used

### Backend
- **ASP.NET Core 8.0** - Web framework
- **C# 12** - Programming language

### Persistence
- **PostgreSQL 18 Alpine** - Node registry database with Guid primary keys
- **Entity Framework Core 8.0.10** - ORM with Npgsql provider
- **Redis 7.2 Alpine** - Sessions and channels cache with automatic TTL
- **StackExchange.Redis 2.8.16** - .NET Redis client

### Cryptography
- **ECDH P-384** - Ephemeral key exchange
- **HKDF-SHA256** - Key derivation
- **AES-256-GCM** - Symmetric encryption
- **RSA-2048** - Certificates and signatures
- **X.509** - Certificate standard

### Infrastructure
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration
- **Swagger/OpenAPI** - API documentation

### Testing
- **xUnit** - Integration tests
- **Bash** - End-to-end test scripts
- **PowerShell** - Legacy test scripts

## 📖 Conventions

- ✅ Implemented and validated
- 🚧 In development
- 📋 Planned
- ⚠️ Outdated/Blocked
- ⭐ Main/important document
- **NEW** - Recently added (within last week)

## 🌍 Documentation Language

**Standard Language:** English

All documentation is now written in English. Translations to English have been completed. See [DOCUMENTATION_TRANSLATION_STATUS.md](DOCUMENTATION_TRANSLATION_STATUS.md) for translation completion status.
