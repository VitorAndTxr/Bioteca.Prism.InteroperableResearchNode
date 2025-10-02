# Interoperable Research Node (IRN)

[![Status](https://img.shields.io/badge/Status-Phase%202%20Complete-success)](https://github.com)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

The Interoperable Research Node (IRN) is the core component of the PRISM (Project Research Interoperability and Standardization Model) framework. It is designed to break down data silos in biomedical research by creating a federated network of standardized, discoverable, and accessible research data.

## 📊 Current Implementation Status

| Component | Status | Description |
|-----------|--------|-------------|
| **Phase 1: Encrypted Channel** | ✅ Complete | ECDH ephemeral keys with Perfect Forward Secrecy |
| **Phase 2: Node Identification** | ✅ Complete | X.509 certificates and digital signatures |
| **Phase 3: Mutual Authentication** | 📋 Planned | Challenge/response authentication |
| **Phase 4: Session Establishment** | 📋 Planned | Capability-based authorization |
| **Data Ingestion** | 📋 Planned | Standardized biosignal data storage |
| **Federated Queries** | 📋 Planned | Cross-node data queries |

## About The Project
Biomedical research, especially involving biosignals, often suffers from data fragmentation. Each research project tends to create its own isolated data ecosystem, making large-scale, collaborative studies difficult and inefficient.

The PRISM model proposes a new way to organize research projects by abstracting them into two fundamental elements: the Device (specialized hardware/software for biosignal capture) and the Application (general-purpose systems for adding context, processing, and storage).

The Interoperable Research Node (IRN) is the cornerstone of this model. It acts as a trusted, standardized gateway that manages data ingestion, authentication, storage, validation, and access. By requiring participating projects to adhere to its standardized interface, each IRN instance can communicate and share data with other nodes, creating a powerful, distributed network for scientific collaboration.

### Key Features (Implemented & Planned)

#### ✅ Implemented
- **🔐 Secure Channel Establishment**
  - ECDH P-384 ephemeral key exchange
  - Perfect Forward Secrecy (PFS)
  - HKDF-SHA256 key derivation
  - AES-256-GCM encryption

- **🆔 Node Identification & Authorization**
  - X.509 certificate-based identification
  - RSA-2048 digital signatures
  - Node registry with approval workflow
  - Multiple authorization states (Unknown, Pending, Authorized, Revoked)

- **🐳 Docker Deployment**
  - Multi-container orchestration
  - Environment-specific configurations
  - Health checks and monitoring

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

# Start both nodes
docker-compose up -d

# Check health
curl http://localhost:5000/api/channel/health  # Node A
curl http://localhost:5001/api/channel/health  # Node B

# Access Swagger UI
# Node A: http://localhost:5000/swagger
# Node B: http://localhost:5001/swagger
```

### Running Tests

```powershell
# Automated test (Phases 1 + 2)
.\test-phase2-full.ps1

# Expected output:
# ✅ Canal criptografado estabelecido (Fase 1)
# ✅ Certificados auto-assinados gerados
# ✅ Nó desconhecido pode se registrar
# ✅ Identificação com status Pending funciona
# ✅ Aprovação de nós funciona
# ✅ Identificação com status Authorized funciona
```

### Manual Testing & Debugging

For detailed manual testing and debugging instructions, see:
- [📖 Manual Testing Guide](docs/testing/manual-testing-guide.md) - Step-by-step debugging guide
- [📋 Phase 1 Test Plan](docs/testing/phase1-test-plan.md) - Encrypted channel tests
- [📋 Phase 2 Test Plan](docs/testing/phase2-test-plan.md) - Node identification tests

## 🏗️ Architecture

### Current Implementation (Phases 1-2)

```
┌─────────────────────────────────────────────────────────────┐
│                    Interoperable Research Node               │
├─────────────────────────────────────────────────────────────┤
│  Phase 1: Encrypted Channel (✅ Complete)                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ • ECDH P-384 Ephemeral Key Exchange                  │  │
│  │ • HKDF-SHA256 Key Derivation                         │  │
│  │ • AES-256-GCM Symmetric Encryption                   │  │
│  │ • Perfect Forward Secrecy                            │  │
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
│  Phase 3: Mutual Authentication (📋 Planned)                │
│  Phase 4: Session Establishment (📋 Planned)                │
└─────────────────────────────────────────────────────────────┘
```

### Handshake Protocol Flow

See [Handshake Protocol Documentation](docs/architecture/handshake-protocol.md) for complete details.

## 📚 Documentation

- **[📖 Documentation Index](docs/README.md)** - Complete documentation index
- **[🔐 Handshake Protocol](docs/architecture/handshake-protocol.md)** - Detailed protocol specification
- **[🧪 Manual Testing Guide](docs/testing/manual-testing-guide.md)** - Step-by-step debugging guide
- **[🏗️ Implementation Roadmap](docs/development/implementation-roadmap.md)** - Development roadmap

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0, C# 12
- **Cryptography**:
  - ECDH (Elliptic Curve Diffie-Hellman) P-384
  - HKDF (HMAC-based Key Derivation Function) SHA-256
  - AES-256-GCM
  - RSA-2048
  - X.509 Certificates
- **Containerization**: Docker, Docker Compose
- **API Documentation**: Swagger/OpenAPI
- **Testing**: PowerShell, curl, Postman

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
