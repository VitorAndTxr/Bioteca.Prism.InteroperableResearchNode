# IRN Documentation - Interoperable Research Node

This folder contains all technical and development documentation for the IRN project.

> **📌 Translation Status:** This documentation is being standardized to English. See [DOCUMENTATION_TRANSLATION_STATUS.md](DOCUMENTATION_TRANSLATION_STATUS.md) for translation progress.

## 📊 Project Status

**Last Updated:** 2025-10-05

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ **Complete + Redis** | Encrypted channel with ephemeral ECDH + Redis persistence |
| **Phase 2** | ✅ **Complete** | Node identification and authorization with X.509 |
| **Phase 3** | ✅ **Complete** | Mutual authentication with challenge/response |
| **Phase 4** | ✅ **Complete + Redis** | Session management with capabilities + Redis persistence |
| **Redis Persistence** | ✅ **Complete** | Multi-instance Redis for sessions and channels |
| **PostgreSQL** | 📋 **Planned** | Node registry database persistence |

## Documentation Structure

### 1. Architecture and Design
- [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md) - **⭐ MAIN** - Complete handshake protocol (Phases 1-4)
- [`architecture/phase4-session-management.md`](architecture/phase4-session-management.md) - **⭐ NEW** - Phase 4 session architecture
- [`architecture/node-communication.md`](architecture/node-communication.md) - Inter-node communication architecture
- [`architecture/session-management.md`](architecture/session-management.md) - Session management between nodes

### 2. Testing
- [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md) - **⭐ MAIN** - Complete manual testing and debugging guide
- [`testing/redis-testing-guide.md`](testing/redis-testing-guide.md) - **⭐ NEW** - Comprehensive Redis testing guide
- [`testing/docker-compose-quick-start.md`](testing/docker-compose-quick-start.md) - **⭐ NEW** - Docker Compose quick start
- [`testing/phase1-test-plan.md`](testing/phase1-test-plan.md) - Phase 1 test plan (Encrypted Channel)
- [`testing/phase2-test-plan.md`](testing/phase2-test-plan.md) - Phase 2 test plan (Identification)
- [`testing/phase1-docker-test.md`](testing/phase1-docker-test.md) - Docker testing
- [`testing/phase1-two-nodes-test.md`](testing/phase1-two-nodes-test.md) - Two-node testing

### 3. Development
- [`development/persistence-architecture.md`](development/persistence-architecture.md) - **⭐ NEW** - Redis and PostgreSQL architecture
- [`development/persistence-implementation-roadmap.md`](development/persistence-implementation-roadmap.md) - **⭐ NEW** - 15-day implementation plan
- [`development/ai-assisted-development.md`](development/ai-assisted-development.md) - AI-assisted development patterns
- [`development/implementation-roadmap.md`](development/implementation-roadmap.md) - Implementation roadmap
- [`development/debugging-docker.md`](development/debugging-docker.md) - Docker container debugging

### 4. API and Protocols ⚠️
- ~~[`api/node-endpoints.md`](api/node-endpoints.md) - Node communication endpoints~~ (Outdated - see Swagger)
- ~~[`api/message-formats.md`](api/message-formats.md) - Message and payload formats~~ (Outdated - see Models)

## 🚀 Quick Start

### To Test the System

1. **Start Docker environment:**
   ```bash
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
- `POST /api/channel/identify` - Identify node with certificate (encrypted)
- `POST /api/node/register` - Register new unknown node (encrypted)
- `GET /api/node/nodes` - List registered nodes (admin)
- `PUT /api/node/{nodeId}/status` - Update node status (admin)

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
- **Redis 7.2 Alpine** - Sessions and channels cache
- **StackExchange.Redis 2.8.16** - .NET Redis client
- **PostgreSQL 15+** - Node registry database (planned)

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

All new documentation MUST be written in English. Existing Portuguese documentation is being translated incrementally. See [DOCUMENTATION_TRANSLATION_STATUS.md](DOCUMENTATION_TRANSLATION_STATUS.md) for current translation progress.
