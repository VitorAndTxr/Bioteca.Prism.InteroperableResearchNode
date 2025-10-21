# PRISM Architecture Philosophy

**Document Version**: 1.0
**Last Updated**: October 2025

---

## PRISM Model Abstraction

The PRISM framework follows a clear separation of concerns across three main abstractions:

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

---

## Key Design Principles

### 1. Data Sovereignty
Each research institution maintains full control over its data:
- **Local Storage**: All data physically stored at the owning institution
- **Access Control**: Institution decides who can query their data
- **No Data Export**: Federated queries return aggregated results, not raw data
- **Audit Trails**: Complete logging of all data access requests

### 2. Cryptographic Trust
4-phase handshake ensures mutual authentication before data exchange:

**Phase 1 - Encrypted Channel**:
- Ephemeral key exchange prevents eavesdropping
- Perfect Forward Secrecy protects past communications
- AES-256-GCM provides authenticated encryption

**Phase 2 - Node Identification**:
- X.509 certificates establish node identity
- Certificate fingerprints serve as unique identifiers
- Manual approval workflow prevents unauthorized access

**Phase 3 - Mutual Authentication**:
- Challenge-response proves private key possession
- One-time challenges prevent replay attacks
- Cryptographic signatures verify authenticity

**Phase 4 - Session Management**:
- Capability-based access control (ReadOnly, ReadWrite, Admin)
- Rate limiting prevents abuse
- Automatic session expiration

### 3. Standardization
HL7 FHIR + SNOMED CT for interoperability:

**HL7 FHIR Alignment**:
- Research projects → ResearchStudy resource
- Volunteers → Patient resource
- Researchers → Practitioner resource
- Recording sessions → Observation resource
- Clinical conditions → Condition resource
- Medications → MedicationStatement resource

**SNOMED CT Integration**:
- Body structures and regions
- Lateralities (left/right)
- Topographical modifiers (medial/lateral)
- Severity codes (mild/moderate/severe)
- Clinical terminologies

**Benefits**:
- Global interoperability with health systems
- Consistent clinical coding
- Semantic reasoning capabilities
- Integration with EHR systems

### 4. Separation of Concerns
Device (capture) ≠ Application (context) ≠ Node (storage/federation)

**Device Layer (sEMG Hardware)**:
- **Responsibility**: Biosignal acquisition and therapeutic intervention
- **No Knowledge Of**: Volunteer identity, research protocols, data federation
- **Outputs**: Raw sEMG data, FES session events, trigger timestamps

**Application Layer (Mobile App)**:
- **Responsibility**: Add research context to device data
- **Adds**: Volunteer identity, research project, session notes, annotations
- **Packages**: Biosignal files + metadata for submission

**Node Layer (Backend Server)**:
- **Responsibility**: Secure storage, validation, and federated querying
- **Validates**: Data integrity, SNOMED CT codes, FHIR compliance
- **Federates**: Cross-institutional queries with aggregated results

**Rationale**:
- Devices remain reusable across applications
- Applications can control multiple device types
- Nodes handle security and federation uniformly

### 5. Privacy by Design
LGPD/GDPR compliance, encryption at rest and in transit:

**Data Minimization**:
- Anonymized volunteer codes (no PII in device data)
- Encrypted biosignal files (AES-256-GCM)
- Aggregated query results (no raw data export)

**Access Control**:
- Role-based permissions (Researcher, Principal Investigator, Admin)
- Capability-based authorization (ReadOnly, ReadWrite, Admin)
- Audit logs for all data access

**Consent Management**:
- Explicit consent required for data collection
- Consent version tracking
- Right to withdraw (data deletion)

**Data Retention**:
- Configurable retention policies
- Automatic anonymization after retention period
- Secure data destruction

---

## Data Flow Example (Complete Research Session)

### 1. Researcher Prepares Session via Mobile App
```
Researcher Actions:
├─> Configures FES parameters (amplitude, frequency, pulse width)
├─> Selects volunteer and research project
└─> Connects to sEMG Device via Bluetooth
```

### 2. sEMG Device Acquires Biosignals
```
Device Processing:
├─> Samples at 860 Hz with AD8232 sensor
├─> Applies Butterworth filter (10-40 Hz)
├─> Detects muscle activation threshold
└─> Triggers FES stimulation when threshold exceeded
```

### 3. Mobile App Collects Session Data
```
Application Processing:
├─> Receives real-time sEMG stream (13 samples/packet)
├─> Logs FES events and trigger timestamps
├─> Records volunteer vital signs and notes
└─> Packages data with SNOMED CT annotations
```

### 4. Data Submission to Research Node (Future)
```
Submission Flow:
├─> Authenticates with node via handshake protocol
│   ├─ Phase 1: Encrypted channel establishment
│   ├─ Phase 2: Node identification
│   ├─ Phase 3: Mutual authentication
│   └─ Phase 4: Session token acquisition
├─> Encrypts payload with AES-256-GCM
├─> Submits biosignal files + metadata
└─> Node validates and stores in PostgreSQL
```

### 5. Federated Query Across Network (Future)
```
Query Federation:
├─> Node A queries "sEMG sessions with spasticity events"
├─> Request authenticated and encrypted to Node B, C, D
├─> Each node executes query on local data
├─> Aggregated results returned to Node A
└─> Privacy-preserving: raw data never leaves source nodes
```

---

## Technology Stack Overview

| Component | Language | Runtime | Database | Communication |
|-----------|----------|---------|----------|---------------|
| **InteroperableResearchNode** | C# 12 | .NET 8.0 | PostgreSQL 18 + Redis 7.2 | HTTPS (TLS 1.3) |
| **sEMG Device Firmware** | C++ | ESP32 FreeRTOS | N/A | Bluetooth SPP (JSON) |
| **Interface System** | TypeScript | Node.js | N/A | WebSocket/HTTP |
| **Mobile App** | TypeScript/JSX | React Native (Expo) | SQLite (local) | Bluetooth + HTTPS |

---

## Architectural Patterns

### Clean Architecture (Backend)
```
Domain Layer (Entities, DTOs, Enums)
    ↓
Core Layer (Interfaces, Base Implementations)
    ↓
Data Layer (PostgreSQL + Redis, EF Core, Repositories)
    ↓
Service Layer (Business Logic, Domain Services)
    ↓
API Layer (Controllers, Middleware, Filters)
```

**Benefits**:
- Independence from frameworks and UI
- Testability at every layer
- Flexibility to change infrastructure (database, cache)

### Repository Pattern (Backend)
```
Generic Base Repository<TEntity, TKey>
    ↓
Domain-Specific Repository (IVolunteerRepository)
    ↓
Service Layer (VolunteerService)
    ↓
API Controller (VolunteerController)
```

**Benefits**:
- Code reusability (CRUD operations implemented once)
- Consistency across all entities
- Easy to add domain-specific queries

### Event-Driven Architecture (Device)
```
Core 0 (Real-Time) → Event Queue → Core 1 (Communication)
    ↓
FreeRTOS Tasks with Mutex Protection
```

**Benefits**:
- Time-critical sampling isolated from communication
- Thread-safe event handling
- Scalability for multiple concurrent operations

---

## Security Architecture

### Defense in Depth

**Layer 1 - Transport Security**:
- TLS 1.3 for HTTPS communication
- Bluetooth encryption (device pairing)
- Certificate pinning (planned)

**Layer 2 - Channel Security**:
- ECDH P-384 ephemeral key exchange
- AES-256-GCM authenticated encryption
- Perfect Forward Secrecy

**Layer 3 - Node Authentication**:
- X.509 certificate-based identity
- RSA-2048 digital signatures
- Challenge-response mutual authentication

**Layer 4 - Session Management**:
- Bearer token authentication
- Capability-based authorization
- Rate limiting (60 requests/minute)
- Automatic session expiration (1 hour)

**Layer 5 - Data Validation**:
- Input sanitization
- SNOMED CT code validation
- HL7 FHIR compliance checks
- Cryptographic integrity verification

---

## Scalability Considerations

### Horizontal Scaling (Planned)
- **Load Balancing**: Nginx or HAProxy for API requests
- **Database Replication**: PostgreSQL read replicas
- **Cache Clustering**: Redis Sentinel for high availability
- **Stateless API**: Session persistence in Redis (not in-memory)

### Vertical Scaling
- **Multi-Core Processing**: EF Core async operations
- **Connection Pooling**: Database and Redis connections
- **Caching Strategy**: Redis for frequently accessed data

### Multi-Instance Architecture
- **Isolated Databases**: Each node has its own PostgreSQL instance
- **Isolated Cache**: Each node has its own Redis instance
- **Docker Compose**: Multi-node orchestration on single host
- **Kubernetes** (Planned): Multi-node orchestration across clusters

---

## Monitoring and Observability (Planned)

### Metrics
- **Application Metrics**: Request rate, latency, error rate
- **Database Metrics**: Connection pool usage, query performance
- **Cache Metrics**: Hit rate, eviction rate
- **System Metrics**: CPU, memory, disk I/O

### Tracing
- **OpenTelemetry**: Distributed tracing across components
- **Trace Context**: Propagate trace IDs through handshake phases
- **Span Attributes**: Capture channel ID, node ID, session token

### Logging
- **Structured Logging**: JSON format with consistent fields
- **Log Levels**: Debug, Info, Warning, Error, Critical
- **Correlation IDs**: Track requests across services

---

## Documentation References

For implementation details, see:

- **Components**: `docs/components/` (detailed component descriptions)
- **Workflows**: `docs/workflows/` (phase-by-phase flows)
- **Architecture**: `docs/architecture/` (technical specifications)
- **Security**: `docs/SECURITY_OVERVIEW.md` (cryptographic details)
- **Development**: `docs/development/` (implementation guides)
