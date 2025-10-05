# IRN Node Communication Architecture

**Status**: 🔄 In Progress (Phases 1-4 Complete)
**Last updated**: 2025-10-05

## Overview

Defines the communication architecture between Interoperable Research Node (IRN) instances, enabling multiple nodes to form a federated network for sharing biomedical research data.

## Architectural Principles

### 1. Decentralization
- No central server; each node is autonomous
- Peer-to-peer (P2P) communication
- Node discovery via distributed registry or DNS

### 2. Security by Design
- All data encrypted in transit (TLS 1.3+)
- Mutual authentication mandatory
- Zero-trust: continuous verification during session

### 3. Interoperability
- Standardized RESTful API
- Protocol versioning
- Support for multiple formats (JSON, Protocol Buffers)

### 4. Resilience
- Configurable timeout and retry
- Circuit breaker for unavailable nodes
- Fallback to local cache when necessary

## Architecture Components

```
┌─────────────────────────────────────────────────────────────┐
│                       IRN Instance A                        │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ API Gateway    │  │ Node Registry   │  │ Auth Manager │ │
│  └────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Session Mgr    │  │ Data Exchange   │  │ Query Router │ │
│  └────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │            Local Data Store & Validation                │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS/TLS 1.3
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       IRN Instance B                        │
│                      (same structure)                       │
└─────────────────────────────────────────────────────────────┘
```

## Communication Layers

### Layer 1: Transport
- **Protocol**: HTTPS (HTTP/2 or HTTP/3)
- **Default port**: 8443 (configurable)
- **TLS**: Minimum version 1.3
- **Compression**: gzip, brotli

### Layer 2: Authentication
- **Handshake**: Custom protocol over HTTPS (✅ Implemented - Phases 1-3)
- **Tokens**: Session tokens with 1-hour duration (✅ Implemented - Phase 4)
- **Refresh**: Renewal via `/api/session/renew` endpoint (✅ Implemented)
- **Revocation**: Session invalidation via `/api/session/revoke` (✅ Implemented)

### Layer 3: Session
- **Management**: Session state in Redis or in-memory (✅ Implemented)
- **Timeout**: 1 hour default (3600 seconds) (✅ Implemented)
- **Keep-alive**: Automatic TTL management via Redis (✅ Implemented)

### Layer 4: Application
- **REST API**: Standardized endpoints (✅ Phases 1-4 complete)
- **Versioning**: Semantic (v1, v2, etc.) - planned
- **Rate Limiting**: 60 requests/minute per session (✅ Implemented)

## Typical Communication Flow

### 1. Node Discovery
```
Client --> [DNS/Registry] --> List of available IRN nodes
```

### 2. Connection Establishment
```
Node A --> [Handshake] --> Node B
       <-- [Session ID] <--
```

**Implemented as 4-phase protocol:**
- Phase 1: Encrypted Channel (ECDH + AES-256-GCM) ✅
- Phase 2: Node Identification (X.509 certificates) ✅
- Phase 3: Mutual Authentication (Challenge-Response) ✅
- Phase 4: Session Management (Bearer tokens) ✅

### 3. Data Exchange
```
Node A --> [Query Request] --> Node B
       <-- [Query Results] <--
```

### 4. Termination
```
Node A --> [Session Close] --> Node B
       <-- [ACK] <--
```

## Main Node-to-Node API Endpoints

### Handshake and Authentication (✅ Implemented)
- `POST /api/channel/open` - Initiate encrypted channel (Phase 1)
- `POST /api/channel/initiate` - Client-side channel initiation (Phase 1)
- `POST /api/node/register` - Node registration (Phase 2)
- `POST /api/node/identify` - Node identification (Phase 2)
- `POST /api/node/challenge` - Request authentication challenge (Phase 3)
- `POST /api/node/authenticate` - Authenticate with signed challenge (Phase 3)
- `POST /api/session/whoami` - Get current session info (Phase 4)
- `POST /api/session/renew` - Renew session (Phase 4)
- `POST /api/session/revoke` - Revoke session (Phase 4)

### Discovery and Information (✅ Partially Implemented)
- `GET /api/channel/health` - Health status (✅ Implemented)
- `GET /api/node/info` - Node information (planned)
- `GET /api/node/capabilities` - Supported capabilities (planned)

### Data Exchange (📋 Planned - Phase 5)
- `POST /api/node/v1/query/metadata` - Query metadata
- `POST /api/node/v1/query/biosignal` - Query biosignals
- `GET /api/node/v1/data/{id}` - Retrieve specific data

## Node Discovery

### Option 1: Centralized Registry (MVP) - ✅ Implemented
- Manual registration of known nodes
- Configuration in `appsettings.json`

```json
{
  "NodeRegistry": {
    "KnownNodes": [
      {
        "nodeId": "uuid-node-b",
        "name": "IRN-Lab-Beta",
        "endpoint": "https://irn-beta.example.com:8443",
        "publicKey": "base64-encoded-key",
        "trusted": true
      }
    ]
  }
}
```

### Option 2: DNS-SD (Future)
- Service Discovery via DNS
- Automatic registration using mDNS/DNS-SD

### Option 3: Blockchain/DLT (Research)
- Immutable node registry
- Distributed reputation

## Security and Trust

### Trust Model

1. **Whitelist**: Only pre-approved nodes (✅ Implemented - MVP)
2. **Web of Trust**: Nodes trust nodes trusted by their peers
3. **Certificate Authority**: Institutional PKI

### Audit

All inter-node communications must be logged:

```json
{
  "eventType": "node-communication",
  "timestamp": "2025-10-01T12:34:56Z",
  "sourceNode": "uuid-node-a",
  "targetNode": "uuid-node-b",
  "action": "query-metadata",
  "result": "success",
  "dataTransferred": 1024,
  "userId": "researcher-xyz"
}
```

## Scalability

### Recommended Limits (MVP)
- **Simultaneous connections**: 50 per node
- **Requests per minute**: 1000 (implemented: 60/min per session)
- **Max payload size**: 10 MB
- **Active sessions**: 100

### Scaling Strategies
- Load balancing between multiple IRN instances
- Distributed cache (Redis) ✅ Implemented
- Data compression
- Pagination for large queries

## Monitoring

### Important Metrics
- Handshake latency
- Authentication success rate
- Data throughput
- Number of active sessions
- Error rate per endpoint

### Health Checks (✅ Implemented)
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "uptime": 86400,
  "activeSessions": 23,
  "lastSync": "2025-10-01T12:00:00Z",
  "dependencies": {
    "database": "healthy",
    "certificateValidation": "healthy",
    "nodeRegistry": "healthy"
  }
}
```

## Implementation

### Current Status
🔄 **In Progress** - Phases 1-4 complete, Phase 5 (Federated Queries) planned

### Folder Structure (Implemented)

```
Bioteca.Prism.InteroperableResearchNode/
├── Controllers/
│   ├── ChannelController.cs              ✅ Phase 1
│   ├── NodeConnectionController.cs       ✅ Phases 2-3
│   ├── SessionController.cs              ✅ Phase 4
│   └── TestingController.cs              ✅ Dev/test utilities
├── Services/
│   ├── Node/
│   │   ├── NodeChannelClient.cs          ✅ HTTP client for handshake
│   │   ├── NodeRegistryService.cs        ✅ Node registry
│   │   ├── ChallengeService.cs           ✅ Challenge-response auth
│   │   └── CertificateHelper.cs          ✅ X.509 utilities
│   ├── Session/
│   │   └── SessionService.cs             ✅ Session management
│   └── Cache/
│       ├── RedisConnectionService.cs     ✅ Redis connection
│       ├── RedisSessionStore.cs          ✅ Redis session persistence
│       ├── InMemorySessionStore.cs       ✅ In-memory fallback
│       └── RedisChannelStore.cs          ✅ Redis channel persistence
├── Domain/
│   ├── Entities/Node/                    ✅ Domain entities
│   ├── Requests/Node/                    ✅ Request DTOs
│   └── Responses/Node/                   ✅ Response DTOs
├── Core/
│   └── Middleware/
│       ├── Channel/
│       │   ├── PrismEncryptedChannelConnectionAttribute.cs  ✅
│       │   └── ChannelContext.cs         ✅
│       └── Session/
│           └── PrismAuthenticatedSessionAttribute.cs       ✅
└── Configuration/
    └── NodeConfiguration.cs              ✅ Node configuration
```

### Next Steps (Phase 5)

1. Implement federated query endpoints
2. Create data submission endpoints
3. Implement query result aggregation
4. Add caching for federated queries
5. Implement audit system
6. Add comprehensive monitoring and metrics

## AI Context

### Suggested Prompt

```
Based on the architecture defined in docs/architecture/node-communication-en.md,
create the initial folder and class structure for node-to-node communication.
Start with Models and Service Interfaces.
```

### Current Implementation Status

**Completed:**
- ✅ Phase 1: Encrypted channel establishment
- ✅ Phase 2: Node identification and registration
- ✅ Phase 3: Mutual authentication
- ✅ Phase 4: Session management with Redis persistence
- ✅ Rate limiting (60 requests/minute)
- ✅ Capability-based authorization

**Next Phase:**
- 📋 Phase 5: Federated query implementation

## References

- REST API Best Practices
- Microservices Communication Patterns
- Federated Learning System Architecture
- FHIR Server-to-Server Communication
- OAuth 2.0 and JWT Standards
