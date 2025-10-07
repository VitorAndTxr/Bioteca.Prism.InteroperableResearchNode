# Interoperable Research Node (IRN)

[![Status](https://img.shields.io/badge/Status-v0.8.0%20Full%20Persistence-success)](https://github.com)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-blue)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7.2-red)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

The Interoperable Research Node (IRN) is the core component of the PRISM (Project Research Interoperability and Standardization Model) framework. It is designed to break down data silos in biomedical research by creating a federated network of standardized, discoverable, and accessible research data.

## 📊 Current Implementation Status

| Component | Status | Description |
|-----------|--------|-------------|
| **Phase 1: Encrypted Channel** | ✅ Complete | ECDH ephemeral keys with Perfect Forward Secrecy + Redis persistence |
| **Phase 2: Node Identification** | ✅ Complete | X.509 certificates, RSA signatures + PostgreSQL node registry |
| **Phase 3: Mutual Authentication** | ✅ Complete | Challenge/response with proof of private key possession |
| **Phase 4: Session Management** | ✅ Complete | Capability-based authorization, rate limiting + Redis persistence |
| **PostgreSQL Persistence** | ✅ Complete | Multi-instance PostgreSQL with EF Core 8.0 + Guid architecture |
| **Redis Persistence** | ✅ Complete | Multi-instance Redis for sessions and channels |
| **Dual-Identifier Architecture** | ✅ Complete | NodeId (protocol) + RegistrationId (database) + Certificate fingerprint |
| **Phase 5: Federated Queries** | 📋 Next | Cross-node data queries with session-based authorization |
| **Data Ingestion** | 📋 Planned | Standardized biosignal data storage |

## About The Project
Biomedical research, especially involving biosignals, often suffers from data fragmentation. Each research project tends to create its own isolated data ecosystem, making large-scale, collaborative studies difficult and inefficient.

The PRISM model proposes a new way to organize research projects by abstracting them into two fundamental elements: the Device (specialized hardware/software for biosignal capture) and the Application (general-purpose systems for adding context, processing, and storage).

The Interoperable Research Node (IRN) is the cornerstone of this model. It acts as a trusted, standardized gateway that manages data ingestion, authentication, storage, validation, and access. By requiring participating projects to adhere to its standardized interface, each IRN instance can communicate and share data with other nodes, creating a powerful, distributed network for scientific collaboration.

### Key Features (Implemented & Planned)

#### ✅ Implemented
- **🔐 Secure Channel Establishment (Phase 1)**
  - ECDH P-384 ephemeral key exchange
  - Perfect Forward Secrecy (PFS)
  - HKDF-SHA256 key derivation
  - AES-256-GCM encryption

- **🔒 End-to-End Encrypted Communication (Phase 2+)**
  - **ALL** payloads after channel establishment are encrypted
  - Mandatory `X-Channel-Id` header for channel validation
  - AES-256-GCM payload encryption/decryption
  - JSON serialization with camelCase compatibility

- **🆔 Node Identification & Authorization (Phase 2)**
  - X.509 certificate-based identification
  - RSA-2048 digital signatures
  - Node registry with approval workflow
  - Multiple authorization states (Unknown, Pending, Authorized, Revoked)
  - **Fully encrypted** registration and identification endpoints

- **🔴 Challenge/Response Mutual Authentication (Phase 3)**
  - Cryptographic proof of private key possession
  - 32-byte random challenges with 5-minute TTL
  - One-time use challenges (replay protection)
  - Session token generation (1-hour TTL)
  - **Fully encrypted** challenge and authentication endpoints

- **🎫 Session Management & Access Control (Phase 4)**
  - Session lifecycle management (whoami, renew, revoke)
  - Capability-based authorization (ReadOnly, ReadWrite, Admin)
  - Per-session rate limiting (60 requests/minute)
  - Session metrics and monitoring
  - **All session operations encrypted** via AES-256-GCM channel

- **🗄️ PostgreSQL Persistence (NEW!)**
  - Multi-instance PostgreSQL 18 Alpine (one database per node)
  - Entity Framework Core 8.0.10 with Npgsql
  - Guid-based primary keys with automatic generation
  - Certificate fingerprint as natural key
  - 4 EF Core migrations successfully applied
  - Connection resiliency with retry policy
  - pgAdmin 4 integration for database management

- **🔴 Redis Persistence**
  - Multi-instance Redis 7.2 Alpine (one per node)
  - Automatic TTL management for sessions (1 hour) and channels (30 minutes)
  - Rate limiting via Redis Sorted Sets (sliding window)
  - Graceful fallback to in-memory storage
  - Feature flags for conditional Redis usage
  - Session and channel data survives node restarts

- **🏗️ Dual-Identifier Architecture**
  - **NodeId (string)**: Protocol-level identifier for external communication
  - **RegistrationId (Guid)**: Internal database primary key
  - **Certificate Fingerprint (SHA-256)**: Natural key for authentication
  - No `node_id` column in database (protocol-only)

- **🐳 Docker Deployment**
  - Multi-container orchestration (2 nodes + 2 PostgreSQL + 2 Redis + pgAdmin + Azurite)
  - Separated architecture: persistence layer + application layer
  - Environment-specific configurations
  - Health checks and monitoring
  - Named volumes for data persistence
  - Quick restart of application without data loss

- **📊 API Documentation**
  - OpenAPI/Swagger integration
  - Interactive testing interface

#### 📋 Planned
- **Standardized Data Ingestion**: Enforces a common data structure for biosignal records and metadata
- **Federated Identity & Access Management**: Cross-network authentication and authorization
- **Data Validation Engine**: Ensures data conforms to PRISM standards
- **Secure & Auditable Storage**: Repository for sensitive research data
- **Inter-Node Communication API**: Secure data queries and exchange between nodes

## 🚀 Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Running with Docker

```powershell
# Clone the repository
git clone https://github.com/your-org/InteroperableResearchNode.git
cd InteroperableResearchNode

# RECOMMENDED: Start persistence layer first (PostgreSQL + Redis + pgAdmin + Azurite)
docker-compose -f docker-compose.persistence.yml up -d

# Then start application layer (Node A + Node B)
docker-compose -f docker-compose.application.yml up -d

# Check status
docker-compose -f docker-compose.application.yml ps

# Check health
curl http://localhost:5000/api/channel/health  # Node A
curl http://localhost:5001/api/channel/health  # Node B

# Access pgAdmin for database management
# http://localhost:5050 (admin@prism.local / prism-admin-password-2025)

# View logs
docker logs -f irn-node-a          # Node A logs
docker logs -f irn-redis-node-a    # Redis Node A logs

# Access Swagger UI
# Node A: http://localhost:5000/swagger
# Node B: http://localhost:5001/swagger

# Access Redis CLI (optional)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a
```

### Running Tests

#### **Option 1: Automated Integration Tests (Recommended)**

```bash
# Run all automated tests
dotnet test

# Run specific test suite
dotnet test --filter "FullyQualifiedName~Phase4SessionManagementTests"

# Run with detailed output
dotnet test --verbosity detailed
```

**Test Coverage:**
- ✅ Phase 1: Encrypted channel establishment (6/6 tests)
- ✅ Phase 2: Node identification and registration (6/6 tests)
- ✅ Phase 3: Mutual authentication (5/5 tests)
- ✅ Phase 4: Session management (8/8 tests)
- ✅ Certificate and signature validation (13/15 tests)
- ✅ Security and edge cases (23/23 tests)
- **Overall: 72/75 tests passing (96%)**

#### **Option 2: Manual Testing via Swagger**

**Step-by-step guide**: [docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md](docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md)

Quick walkthrough:
1. Open Swagger UI: http://localhost:5000/swagger
2. Establish channel: `POST /api/channel/initiate`
3. Generate certificate: `POST /api/testing/generate-certificate`
4. Sign data: `POST /api/testing/sign-data`
5. Encrypt payload: `POST /api/testing/encrypt-payload`
6. Identify node: `POST /api/node/identify` (with `X-Channel-Id` header)
7. Register node: `POST /api/node/register`
8. Approve node: `PUT /api/node/{nodeId}/status`
9. Re-identify to get `nextPhase: "phase3_authenticate"`

#### **Option 3: End-to-End Test Script (Bash)**

```bash
# Complete end-to-end test (Phases 1→2→3→4)
bash test-phase4.sh
```

**What it tests:**
- ✅ Complete handshake flow (all 4 phases)
- ✅ Channel establishment + encryption
- ✅ Node identification + registration
- ✅ Challenge-response authentication
- ✅ Session management (whoami, renew, revoke)

### Manual Testing & Debugging

For detailed manual testing and debugging instructions, see:
- [📖 Manual Testing Guide](docs/testing/manual-testing-guide.md) - Step-by-step debugging guide
- [🔴 Redis Testing Guide](docs/testing/redis-testing-guide.md) - Redis persistence testing
- [🐳 Docker Compose Quick Start](docs/testing/docker-compose-quick-start.md) - Docker testing scenarios
- [📋 Phase 1 Test Plan](docs/testing/phase1-test-plan.md) - Encrypted channel tests
- [📋 Phase 2 Test Plan](docs/testing/phase2-test-plan.md) - Node identification tests

## 🏗️ Architecture

### Current Implementation (Phases 1-4 + Redis)

```
┌─────────────────────────────────────────────────────────────┐
│              Interoperable Research Node (IRN)              │
├─────────────────────────────────────────────────────────────┤
│  Phase 1: Encrypted Channel (✅ Complete)                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • ECDH P-384 Ephemeral Key Exchange                  │  │
│  │ • HKDF-SHA256 Key Derivation                         │  │
│  │ • AES-256-GCM Symmetric Encryption                   │  │
│  │ • Perfect Forward Secrecy                            │  │
│  │ • Redis/In-Memory Storage (30 min TTL)              │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Phase 2: Node Identification (✅ Complete)                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • X.509 Certificate Management                       │  │
│  │ • RSA-2048 Digital Signatures                        │  │
│  │ • Node Registry & Authorization                      │  │
│  │ • Admin Approval Workflow                            │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Phase 3: Mutual Authentication (✅ Complete)               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • Challenge-Response Protocol                        │  │
│  │ • RSA Signature Verification                         │  │
│  │ • Proof of Private Key Possession                    │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Phase 4: Session Management (✅ Complete)                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • Bearer Token Authentication                        │  │
│  │ • Capability-Based Authorization                     │  │
│  │ • Rate Limiting (60 req/min via Redis)               │  │
│  │ • Redis/In-Memory Storage (1 hour TTL)               │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Redis Persistence (✅ Complete)                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • Multi-Instance Architecture (1 per node)           │  │
│  │ • Automatic TTL Management                           │  │
│  │ • Graceful Fallback to In-Memory                     │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Handshake Protocol Flow

See [Handshake Protocol Documentation](docs/architecture/handshake-protocol.md) for complete details.

## 📚 Documentation

- **[📖 Documentation Index](docs/README.md)** - Complete documentation index
- **[🔐 Handshake Protocol](docs/architecture/handshake-protocol.md)** - Detailed protocol specification
- **[💾 Persistence Architecture](docs/development/persistence-architecture.md)** - Redis and PostgreSQL architecture
- **[🔴 Redis Testing Guide](docs/testing/redis-testing-guide.md)** - Comprehensive Redis testing
- **[🐳 Docker Quick Start](docs/testing/docker-compose-quick-start.md)** - Docker testing scenarios
- **[🧪 Manual Testing Guide](docs/testing/manual-testing-guide.md)** - Step-by-step debugging guide
- **[🏗️ Implementation Roadmap](docs/development/implementation-roadmap.md)** - Development roadmap

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0, C# 12
- **Persistence**:
  - Redis 7.2 Alpine (sessions and channels)
  - StackExchange.Redis 2.8.16
  - PostgreSQL 15+ (planned for node registry)
- **Cryptography**:
  - ECDH (Elliptic Curve Diffie-Hellman) P-384
  - HKDF (HMAC-based Key Derivation Function) SHA-256
  - AES-256-GCM
  - RSA-2048
  - X.509 Certificates
- **Containerization**: Docker, Docker Compose
- **API Documentation**: Swagger/OpenAPI
- **Testing**: xUnit, PowerShell, Bash, Postman

## Conceptual Architecture
The IRN facilitates the flow of information between the core components of a research project and the wider research network.





## Contributing
Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are greatly appreciated.

Please see the CONTRIBUTING.md file for details on our code of conduct and the process for submitting pull requests.

## License
Distributed under the MIT License. See LICENSE for more information.

## Acknowledgments
This project is part of a Computer Engineering monograph on biomedical data standards.

Inspired by modern data architecture paradigms like Data Mesh.

... (any other acknowledgments)
