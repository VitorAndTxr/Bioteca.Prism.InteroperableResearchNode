# Documentation Reorganization Plan

**Goal**: Reduce CLAUDE.md token usage from 30.1k to ~8-10k tokens (66-75% reduction)

**Current State**:
- Root CLAUDE.md: ~10.2k tokens (1685 lines)
- InteroperableResearchNode/CLAUDE.md: ~19.9k tokens (1685 lines)
- Total: 30.1k tokens (15.1% of 200k context window)

**Target State**:
- Root CLAUDE.md: ~3-4k tokens (~300-400 lines) - High-level navigation index
- InteroperableResearchNode/CLAUDE.md: ~5-6k tokens (~400-500 lines) - Project-specific essentials
- Total: ~8-10k tokens (4-5% of context window)

---

## Phase 1: Root CLAUDE.md Reorganization

### Keep in CLAUDE.md (Essential Navigation):
1. ✅ **Project Overview** (condensed) - 50 lines
2. ✅ **Repository Structure** (simplified tree) - 30 lines
3. ✅ **Quick Navigation Guide** - 100 lines
   - When to use each component
   - Links to detailed documentation
4. ✅ **Component Summary Table** - 40 lines
   - Brief description + link to detailed docs
5. ✅ **Common Commands Quick Reference** - 80 lines
   - Most frequently used commands only

**Total Target**: ~300 lines (~3-4k tokens)

### Extract to New Files:

1. **`docs/components/INTEROPERABLE_RESEARCH_NODE.md`**
   - Complete backend description
   - 4-phase handshake protocol summary
   - Dual-identifier architecture
   - Persistence layer details
   - Clinical data model overview
   - **Extracted from**: Root CLAUDE.md Section "1. InteroperableResearchNode"

2. **`docs/components/SEMG_DEVICE.md`**
   - Complete sEMG device description
   - Hardware components
   - Signal processing architecture
   - Bluetooth protocol (14 message codes)
   - Device operating modes
   - Integration with PRISM
   - **Extracted from**: Root CLAUDE.md Section "2. InteroperableResearchsEMGDevice"

3. **`docs/components/INTERFACE_SYSTEM.md`**
   - Interface layer description
   - Protocol translation
   - API gateway
   - **Extracted from**: Root CLAUDE.md Section "3. InteroperableResearchInterfaceSystem"

4. **`docs/components/MOBILE_APP.md`**
   - Mobile app description
   - React Native architecture
   - Bluetooth communication
   - **Extracted from**: Root CLAUDE.md Section "4. neurax_react_native_app"

5. **`docs/ARCHITECTURE_PHILOSOPHY.md`**
   - PRISM Model Abstraction diagram
   - Key Design Principles
   - Data Flow Example
   - Technology Stack Overview
   - **Extracted from**: Root CLAUDE.md "Architectural Philosophy" section

6. **`docs/SECURITY_OVERVIEW.md`**
   - Phase 1-4 security details
   - Cryptographic algorithms
   - Security requirements (LGPD/GDPR)
   - **Extracted from**: Root CLAUDE.md "Security & Cryptography" section

7. **`docs/development/COMMON_COMMANDS.md`**
   - All development commands (Backend, Device, Docker, Testing, Database)
   - **Extracted from**: Root CLAUDE.md "Common Development Commands" section

8. **`docs/KNOWN_ISSUES.md`**
   - Docker network configuration
   - Test status
   - Compiler warnings
   - **Extracted from**: Root CLAUDE.md "Known Issues & Warnings" section

---

## Phase 2: InteroperableResearchNode/CLAUDE.md Reorganization

### Keep in CLAUDE.md (Project Essentials):
1. ✅ **Project Overview** (brief) - 40 lines
2. ✅ **Documentation Standards** (condensed) - 40 lines
3. ✅ **Quick Start Commands** - 60 lines
4. ✅ **Architecture Summary** - 80 lines
   - Clean Architecture layers
   - Generic base pattern (brief)
   - Handshake protocol phases (links only)
5. ✅ **Important Files Reference** - 60 lines
   - Configuration, Documentation, Testing
6. ✅ **Navigation Guide** - 60 lines
   - Links to detailed docs

**Total Target**: ~340 lines (~5-6k tokens)

### Extract to New Files:

1. **`docs/architecture/PROJECT_STRUCTURE.md`**
   - Complete folder structure tree
   - Layer descriptions (Domain, Core, Data, Service, API)
   - Entity configurations (28 tables)
   - **Extracted from**: IRN CLAUDE.md "Project Structure" section

2. **`docs/architecture/GENERIC_BASE_PATTERN.md`**
   - Design philosophy
   - Core interfaces
   - Base implementations
   - Domain-specific extensions
   - Benefits
   - **Extracted from**: IRN CLAUDE.md "Generic Base Pattern Architecture" section

3. **`docs/architecture/NODE_IDENTIFIER_ARCHITECTURE.md`**
   - Dual-identifier system explanation
   - NodeId vs RegistrationId vs Certificate Fingerprint
   - Usage patterns
   - Key endpoints
   - Database schema
   - **Extracted from**: IRN CLAUDE.md "Node Identifier Architecture" section

4. **`docs/architecture/ATTRIBUTE_BASED_REQUEST_PROCESSING.md`**
   - PrismEncryptedChannelConnectionAttribute details
   - Flow diagram
   - Payload format
   - Usage examples
   - **Extracted from**: IRN CLAUDE.md "Attribute-Based Request Processing" section

5. **`docs/development/SERVICE_REGISTRATION.md`**
   - Singleton vs Scoped services
   - Feature flags
   - DI container setup
   - **Extracted from**: IRN CLAUDE.md "Service Registration" section

6. **`docs/development/PERSISTENCE_LAYER.md`**
   - Redis persistence (multi-instance)
   - PostgreSQL persistence (28 tables schema)
   - Repository pattern
   - Benefits
   - **Extracted from**: IRN CLAUDE.md "Redis Persistence" + "PostgreSQL Node Registry" sections

7. **`docs/development/CERTIFICATE_MANAGEMENT.md`**
   - Development certificates
   - Production requirements
   - **Extracted from**: IRN CLAUDE.md "Certificate Management" section

8. **`docs/workflows/CHANNEL_FLOW.md`**
   - Phase 1 step-by-step flow
   - **Extracted from**: IRN CLAUDE.md "Channel Flow" section

9. **`docs/workflows/PHASE2_IDENTIFICATION_FLOW.md`**
   - Phase 2 step-by-step flow
   - Testing helpers
   - **Extracted from**: IRN CLAUDE.md "Phase 2: Node Identification Flow" section

10. **`docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`**
    - Phase 3 step-by-step flow
    - Challenge-response details
    - Testing helpers
    - **Extracted from**: IRN CLAUDE.md "Phase 3: Challenge-Response Authentication Flow" section

11. **`docs/workflows/PHASE4_SESSION_FLOW.md`**
    - Phase 4 step-by-step flow
    - Session validation
    - Access level authorization
    - Rate limiting
    - Request/response formats
    - **Extracted from**: IRN CLAUDE.md "Phase 4: Session Management Flow" section

---

## Phase 3: Consolidate Duplicate Content

### Actions:
1. **Development Commands**:
   - Merge all command references into `docs/development/COMMON_COMMANDS.md`
   - Remove duplicates from CLAUDE.md files
   - Keep only 5-10 most common commands in CLAUDE.md

2. **Docker Documentation**:
   - Already exists: `docs/development/DOCKER-SETUP.md`
   - Remove Docker details from CLAUDE.md
   - Keep only quick start commands

3. **Test Status**:
   - Already exists: `docs/PROJECT_STATUS.md`
   - Remove test status from CLAUDE.md
   - Add link to PROJECT_STATUS.md

4. **Known Issues**:
   - Create `docs/KNOWN_ISSUES.md`
   - Remove from CLAUDE.md
   - Add link only

5. **Next Steps / Roadmap**:
   - Already exists: `docs/development/implementation-roadmap.md`
   - Update PROJECT_STATUS.md with current sprint
   - Remove from CLAUDE.md

---

## Phase 4: Create Navigation Index

### New File: `docs/NAVIGATION_INDEX.md`

```markdown
# PRISM Documentation Navigation Index

## By Role

### Backend Developer
- Start: InteroperableResearchNode/CLAUDE.md
- Architecture: docs/architecture/
- Workflows: docs/workflows/
- Development: docs/development/

### Device/Firmware Developer
- Start: InteroperableResearchsEMGDevice/CLAUDE.md
- Bluetooth Protocol: docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md
- Integration: docs/INTEGRATION_GUIDE.md

### Frontend/Mobile Developer
- Start: neurax_react_native_app/
- Device Communication: docs/DEVICE_NODE_PROTOCOL_REFERENCE.md

### System Integrator
- Start: Root CLAUDE.md
- Architecture: docs/ARCHITECTURE_PHILOSOPHY.md
- Components: docs/components/

## By Topic

### Architecture
- Overall: docs/ARCHITECTURE_PHILOSOPHY.md
- Backend: docs/architecture/PROJECT_STRUCTURE.md
- Handshake: docs/architecture/handshake-protocol.md
- Sessions: docs/architecture/phase4-session-management.md

### Development
- Commands: docs/development/COMMON_COMMANDS.md
- Docker: docs/development/DOCKER-SETUP.md
- Persistence: docs/development/PERSISTENCE_LAYER.md

### Testing
- Manual: docs/testing/manual-testing-guide.md
- Redis: docs/testing/redis-testing-guide.md
- Status: docs/PROJECT_STATUS.md

### Workflows
- Phase 1: docs/workflows/CHANNEL_FLOW.md
- Phase 2: docs/workflows/PHASE2_IDENTIFICATION_FLOW.md
- Phase 3: docs/workflows/PHASE3_AUTHENTICATION_FLOW.md
- Phase 4: docs/workflows/PHASE4_SESSION_FLOW.md
```

---

## Expected Outcome

### Before:
```
Root CLAUDE.md: 1685 lines, 10.2k tokens
IRN CLAUDE.md: 1685 lines, 19.9k tokens
Total: 3370 lines, 30.1k tokens (15.1% context)
```

### After:
```
Root CLAUDE.md: ~300 lines, ~3.5k tokens
IRN CLAUDE.md: ~340 lines, ~5.5k tokens
Total: ~640 lines, ~9k tokens (4.5% context)

Reduction: 80% fewer lines, 70% fewer tokens
```

### New Documentation Structure:
```
docs/
├── NAVIGATION_INDEX.md (new)
├── ARCHITECTURE_PHILOSOPHY.md (new)
├── SECURITY_OVERVIEW.md (new)
├── KNOWN_ISSUES.md (new)
├── components/ (new directory)
│   ├── INTEROPERABLE_RESEARCH_NODE.md
│   ├── SEMG_DEVICE.md
│   ├── INTERFACE_SYSTEM.md
│   └── MOBILE_APP.md
├── architecture/
│   ├── PROJECT_STRUCTURE.md (new)
│   ├── GENERIC_BASE_PATTERN.md (new)
│   ├── NODE_IDENTIFIER_ARCHITECTURE.md (new)
│   ├── ATTRIBUTE_BASED_REQUEST_PROCESSING.md (new)
│   └── ... (existing files)
├── development/
│   ├── COMMON_COMMANDS.md (new)
│   ├── PERSISTENCE_LAYER.md (new)
│   ├── SERVICE_REGISTRATION.md (new)
│   ├── CERTIFICATE_MANAGEMENT.md (new)
│   └── ... (existing files)
└── workflows/ (new directory)
    ├── CHANNEL_FLOW.md
    ├── PHASE2_IDENTIFICATION_FLOW.md
    ├── PHASE3_AUTHENTICATION_FLOW.md
    └── PHASE4_SESSION_FLOW.md
```

---

## Implementation Order

1. ✅ Create this plan document
2. Create new directory structure (components/, workflows/)
3. Extract content from Root CLAUDE.md → new files
4. Extract content from IRN CLAUDE.md → new files
5. Rewrite Root CLAUDE.md as navigation index
6. Rewrite IRN CLAUDE.md as project essentials
7. Create NAVIGATION_INDEX.md
8. Update cross-references in all files
9. Verify all links work
10. Update CHANGELOG.md with reorganization notes
