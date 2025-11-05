# PRISM Documentation Navigation Index

**Last Updated**: November 4, 2025
**Documentation Version**: 2.1 (Development Guides Added)

This index provides multiple navigation paths through the PRISM documentation based on your role, task, or interest.

---

## 🚀 Quick Start Paths

### New to PRISM?
1. Read `../CLAUDE.md` (project overview)
2. Read `ARCHITECTURE_PHILOSOPHY.md` (understand the big picture)
3. Choose your component from `components/`
4. Follow the relevant workflow from `workflows/`

### Ready to Code?
1. Read your component's CLAUDE.md:
   - Backend: `InteroperableResearchNode/CLAUDE.md`
   - Device: `InteroperableResearchsEMGDevice/CLAUDE.md`
2. Review `workflows/` for step-by-step implementation
3. Check `testing/manual-testing-guide.md` for testing strategy

### Troubleshooting?
1. Check `KNOWN_ISSUES.md` (common problems and solutions)
2. Review `PROJECT_STATUS.md` (current implementation status)
3. Check component-specific documentation in `components/`

---

## 📂 By Role

### Backend Developer

**Start Here**: `InteroperableResearchNode/CLAUDE.md`

**Essential Reading**:
- `architecture/handshake-protocol.md` - Complete 4-phase protocol
- `architecture/PROJECT_STRUCTURE.md` - Folder structure (TODO)
- `architecture/GENERIC_BASE_PATTERN.md` - Repository/service pattern (TODO)
- `SECURITY_OVERVIEW.md` - Security architecture
- `development/API_ENDPOINT_IMPLEMENTATION_GUIDE.md` - Step-by-step endpoint guide ✅
- `development/PAGINATION_SYSTEM.md` - Pagination implementation ✅
- `development/PERSISTENCE_LAYER.md` - PostgreSQL + Redis (TODO)

**Workflows** (step-by-step):
- `workflows/CHANNEL_FLOW.md` - Phase 1 implementation
- `workflows/PHASE2_IDENTIFICATION_FLOW.md` - Phase 2 implementation
- `workflows/PHASE3_AUTHENTICATION_FLOW.md` - Phase 3 implementation
- `workflows/PHASE4_SESSION_FLOW.md` - Phase 4 implementation

**Testing**:
- `testing/manual-testing-guide.md` - Manual testing procedures
- `testing/redis-testing-guide.md` - Redis persistence testing
- `testing/docker-compose-quick-start.md` - Docker setup

**Reference**:
- `components/INTEROPERABLE_RESEARCH_NODE.md` - Complete backend reference
- `PROJECT_STATUS.md` - Implementation status and roadmap

---

### Device/Firmware Developer

**Start Here**: `InteroperableResearchsEMGDevice/CLAUDE.md`

**Essential Reading**:
- `InteroperableResearchsEMGDevice/docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md` - Master protocol reference (v2.0)
- `InteroperableResearchsEMGDevice/docs/api/PROTOCOL_DOCUMENTATION_GUIDE.md` - Navigation guide
- `InteroperableResearchsEMGDevice/docs/api/bluetooth-protocol.md` - Binary streaming (v1.1)
- `InteroperableResearchsEMGDevice/docs/INTEGRATION_GUIDE.md` - PRISM integration

**Reference**:
- `components/SEMG_DEVICE.md` - Complete device reference
- `InteroperableResearchsEMGDevice/docs/api/streaming-protocol.md` - Quick 5-minute overview

**Integration**:
- `InteroperableResearchsEMGDevice/docs/DEVICE_NODE_PROTOCOL_REFERENCE.md` - Device-to-node communication

---

### Frontend/Mobile Developer

**Start Here**: `neurax_react_native_app/` (if documented)

**Essential Reading**:
- `components/MOBILE_APP.md` - Mobile app architecture and features
- `InteroperableResearchsEMGDevice/docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md` - Bluetooth protocol for device communication
- `InteroperableResearchNode/docs/DEVICE_NODE_PROTOCOL_REFERENCE.md` - Backend API integration

**Reference**:
- `components/INTERFACE_SYSTEM.md` - Middleware layer
- `architecture/handshake-protocol.md` - Backend authentication flow

---

### System Integrator / Architect

**Start Here**: `../CLAUDE.md` (root overview)

**Essential Reading**:
- `ARCHITECTURE_PHILOSOPHY.md` - PRISM model, design principles, data flow
- `SECURITY_OVERVIEW.md` - Complete security architecture
- All component docs in `components/`:
  - `INTEROPERABLE_RESEARCH_NODE.md`
  - `SEMG_DEVICE.md`
  - `INTERFACE_SYSTEM.md`
  - `MOBILE_APP.md`

**Technical Deep Dives**:
- `architecture/handshake-protocol.md` - 4-phase protocol specification
- `architecture/phase4-session-management.md` - Session management architecture
- `development/persistence-architecture.md` - Data persistence strategy

---

### QA / Tester

**Start Here**: `testing/manual-testing-guide.md`

**Essential Reading**:
- `PROJECT_STATUS.md` - Current test status (73/75 passing - 97.3%)
- `testing/redis-testing-guide.md` - Redis persistence testing
- `testing/docker-compose-quick-start.md` - Environment setup
- `KNOWN_ISSUES.md` - Known bugs and workarounds (TODO)

**Test Workflows**:
- `workflows/CHANNEL_FLOW.md` - Phase 1 testing
- `workflows/PHASE2_IDENTIFICATION_FLOW.md` - Phase 2 testing
- `workflows/PHASE3_AUTHENTICATION_FLOW.md` - Phase 3 testing
- `workflows/PHASE4_SESSION_FLOW.md` - Phase 4 testing

**Scripts**:
- `test-phase4.sh` - End-to-end automated test

---

### Researcher / Domain Expert

**Start Here**: `ARCHITECTURE_PHILOSOPHY.md`

**Essential Reading**:
- `../CLAUDE.md` - Project overview and goals
- `components/INTEROPERABLE_RESEARCH_NODE.md` - Clinical data model (28 tables)
- `architecture/handshake-protocol.md` - Security and privacy mechanisms

**Standards**:
- HL7 FHIR alignment for health data
- SNOMED CT integration for clinical terminologies
- LGPD/GDPR compliance for data protection

---

## 📑 By Topic

### Architecture & Design

**Overview**:
- `ARCHITECTURE_PHILOSOPHY.md` - PRISM model, key principles, data flow
- `architecture/handshake-protocol.md` - Complete 4-phase protocol
- `architecture/phase4-session-management.md` - Session management

**Backend Architecture**:
- `architecture/PROJECT_STRUCTURE.md` - Complete folder structure (TODO)
- `architecture/GENERIC_BASE_PATTERN.md` - Repository/service pattern (TODO)
- `architecture/NODE_IDENTIFIER_ARCHITECTURE.md` - Dual-identifier system (TODO)
- `architecture/ATTRIBUTE_BASED_REQUEST_PROCESSING.md` - Middleware (TODO)

**Device Architecture**:
- `components/SEMG_DEVICE.md` - Signal processing pipeline, hardware components

---

### Security & Cryptography

**Complete Security Overview**:
- `SECURITY_OVERVIEW.md` - All 4 phases detailed, cryptographic algorithms

**Phase-Specific**:
- `workflows/CHANNEL_FLOW.md` - Phase 1: ECDH + AES-256-GCM + PFS
- `workflows/PHASE2_IDENTIFICATION_FLOW.md` - Phase 2: X.509 + RSA-2048
- `workflows/PHASE3_AUTHENTICATION_FLOW.md` - Phase 3: Challenge-response
- `workflows/PHASE4_SESSION_FLOW.md` - Phase 4: Bearer tokens + rate limiting

---

### Development Workflows

**Backend Workflows** (step-by-step implementation):
1. `workflows/CHANNEL_FLOW.md` - Encrypted channel establishment
2. `workflows/PHASE2_IDENTIFICATION_FLOW.md` - Node identification and registration
3. `workflows/PHASE3_AUTHENTICATION_FLOW.md` - Mutual authentication
4. `workflows/PHASE4_SESSION_FLOW.md` - Session lifecycle management

**Development Guides**:
- `development/API_ENDPOINT_IMPLEMENTATION_GUIDE.md` - Complete guide for implementing new endpoints ✅
- `development/PAGINATION_SYSTEM.md` - Pagination architecture and implementation ✅
- `development/RECENT_IMPLEMENTATIONS.md` - Recent changes and migration guide ✅
- `development/COMMON_COMMANDS.md` - All CLI commands (TODO)
- `development/PERSISTENCE_LAYER.md` - PostgreSQL + Redis setup (TODO)
- `development/SERVICE_REGISTRATION.md` - DI container (TODO)
- `development/CERTIFICATE_MANAGEMENT.md` - X.509 certificates (TODO)
- `development/DOCKER-SETUP.md` - Docker multi-node architecture

---

### Component References

**Detailed Component Documentation**:
- `components/INTEROPERABLE_RESEARCH_NODE.md` - Backend (ASP.NET Core 8.0)
- `components/SEMG_DEVICE.md` - sEMG/FES device (ESP32, FreeRTOS)
- `components/INTERFACE_SYSTEM.md` - Middleware (TypeScript, Node.js)
- `components/MOBILE_APP.md` - Mobile app (React Native, Expo)

---

### Testing & Quality Assurance

**Testing Documentation**:
- `testing/manual-testing-guide.md` - Step-by-step manual testing
- `testing/redis-testing-guide.md` - Redis persistence testing
- `testing/docker-compose-quick-start.md` - Environment setup
- `testing/test-fixes-roadmap.md` - Test improvement roadmap

**Test Scripts**:
- `../test-phase4.sh` - Complete end-to-end test (Phases 1→2→3→4)
- `../test-phase3.sh` - Phases 1→2→3 (deprecated)
- `../test-phase2-full.ps1` - Phases 1+2 (deprecated)

**Test Status**:
- `PROJECT_STATUS.md` - Current test results (73/75 passing - 97.3%)

---

### Clinical Data & Standards

**Data Model**:
- `components/INTEROPERABLE_RESEARCH_NODE.md` - 28-table clinical schema
- `diagrams/database/er-diagram.md` - Entity-relationship diagram

**Standards Compliance**:
- HL7 FHIR alignment (ResearchStudy, Patient, Practitioner, Observation, Condition)
- SNOMED CT integration (body structures, laterality, severity codes, clinical terminologies)
- LGPD/GDPR compliance (privacy by design, consent management, data retention)

---

### Troubleshooting & Support

**Known Issues**:
- `KNOWN_ISSUES.md` - Common problems and solutions (TODO)

**Project Status**:
- `PROJECT_STATUS.md` - Implementation status, test results, known limitations

**Debugging Guides**:
- `development/debugging-docker.md` - Docker troubleshooting
- `testing/manual-testing-guide.md` - Testing procedures

---

## 📊 Documentation Statistics

**Total Documentation Files**: 35+ files

**By Category**:
- Components: 4 files
- Workflows: 4 files
- Architecture: 6+ files
- Development: 8+ files
- Testing: 6+ files
- Diagrams: 3+ files

**Languages**: English (all new documentation)

**Format**: Markdown (CommonMark specification)

---

## 🔗 External Resources

**PRISM Ecosystem**:
- Root documentation: `../CLAUDE.md`
- Device documentation: `../InteroperableResearchsEMGDevice/CLAUDE.md`
- Interface system: `../InteroperableResearchInterfaceSystem/`
- Mobile app: `../neurax_react_native_app/`

**Standards**:
- HL7 FHIR: https://www.hl7.org/fhir/
- SNOMED CT: https://www.snomed.org/
- X.509 Certificates: https://www.itu.int/rec/T-REC-X.509

**Technologies**:
- ASP.NET Core: https://docs.microsoft.com/aspnet/core
- Entity Framework Core: https://docs.microsoft.com/ef/core
- PostgreSQL: https://www.postgresql.org/docs/
- Redis: https://redis.io/documentation

---

## 📝 Documentation Conventions

### File Naming
- UPPERCASE.md for top-level index files (e.g., `CLAUDE.md`, `README.md`)
- PascalCase.md for specific documentation (e.g., `ChannelFlow.md`)
- SCREAMING_SNAKE_CASE.md for reference files (e.g., `NAVIGATION_INDEX.md`)

### Internal Links
- Use relative paths: `../CLAUDE.md`, `workflows/CHANNEL_FLOW.md`
- Prefer Markdown links: `[Link Text](path/to/file.md)`
- Include section anchors when available: `[Section](#section-name)`

### Code Examples
- Use language-specific code blocks: ```csharp, ```bash, ```json
- Include comments for clarity
- Provide complete, runnable examples when possible

---

## 🆘 Getting Help

### Documentation Issues
- Missing documentation? Check `REORGANIZATION_PROGRESS.md` for TODO list
- Broken links? File an issue in the repository
- Unclear content? Suggest improvements via pull request

### Technical Support
- Backend: Review `InteroperableResearchNode/CLAUDE.md`
- Device: Review `InteroperableResearchsEMGDevice/CLAUDE.md`
- Testing: Review `testing/manual-testing-guide.md`
- Known issues: Review `KNOWN_ISSUES.md` (TODO)

### Contribution Guidelines
- All documentation MUST be in English
- Follow existing formatting conventions
- Update `NAVIGATION_INDEX.md` when adding new files
- Include cross-references to related documentation

---

## 📅 Documentation Roadmap

### Completed ✅
- Root CLAUDE.md (navigation index)
- IRN CLAUDE.md (project essentials)
- Component documentation (4 files)
- Workflow documentation (4 files)
- Architecture philosophy and security overview
- Development guides (API_ENDPOINT_IMPLEMENTATION_GUIDE, PAGINATION_SYSTEM, RECENT_IMPLEMENTATIONS)

### In Progress 🚧
- Architecture detail files (PROJECT_STRUCTURE, GENERIC_BASE_PATTERN, NODE_IDENTIFIER_ARCHITECTURE)
- Remaining development guides (COMMON_COMMANDS, PERSISTENCE_LAYER, SERVICE_REGISTRATION)

### Planned 📋
- KNOWN_ISSUES.md (consolidated troubleshooting)
- Additional architecture diagrams
- Video walkthroughs (for complex workflows)
- API reference documentation (OpenAPI/Swagger export)

---

**For the most up-to-date documentation status, see**: `REORGANIZATION_PROGRESS.md`
