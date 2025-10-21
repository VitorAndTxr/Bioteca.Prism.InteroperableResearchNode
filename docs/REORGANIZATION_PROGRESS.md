# Documentation Reorganization Progress

**Date Started**: October 21, 2025
**Current Status**: Phase 1 Complete (Component and Architecture Extraction)
**Target**: Reduce CLAUDE.md token usage from 30.1k to ~8-10k tokens

---

## ✅ Completed (Phase 1)

### New Directory Structure
- `docs/components/` - Component descriptions
- `docs/workflows/` - Phase-by-phase workflows

### Component Documentation (4 files created)
1. ✅ `docs/components/INTEROPERABLE_RESEARCH_NODE.md` - Backend overview
2. ✅ `docs/components/SEMG_DEVICE.md` - sEMG device with full Bluetooth protocol
3. ✅ `docs/components/INTERFACE_SYSTEM.md` - Middleware layer
4. ✅ `docs/components/MOBILE_APP.md` - React Native application

### Architecture Documentation (2 files created)
1. ✅ `docs/ARCHITECTURE_PHILOSOPHY.md` - PRISM model, design principles, data flow
2. ✅ `docs/SECURITY_OVERVIEW.md` - Complete security architecture (Phases 1-4)

### Workflow Documentation (2 of 4 files created)
1. ✅ `docs/workflows/CHANNEL_FLOW.md` - Phase 1 encrypted channel
2. ✅ `docs/workflows/PHASE2_IDENTIFICATION_FLOW.md` - Phase 2 node identification
3. 🚧 `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md` - **TODO**
4. 🚧 `docs/workflows/PHASE4_SESSION_FLOW.md` - **TODO**

---

## 🚧 In Progress (Phase 2)

### Workflow Documentation (Remaining)
- [ ] `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md` - Challenge-response authentication
- [ ] `docs/workflows/PHASE4_SESSION_FLOW.md` - Session management

### Architecture Details (Remaining)
- [ ] `docs/architecture/PROJECT_STRUCTURE.md` - Complete folder structure
- [ ] `docs/architecture/GENERIC_BASE_PATTERN.md` - Repository/service pattern
- [ ] `docs/architecture/NODE_IDENTIFIER_ARCHITECTURE.md` - Dual-identifier system
- [ ] `docs/architecture/ATTRIBUTE_BASED_REQUEST_PROCESSING.md` - Middleware filters

### Development Documentation (Remaining)
- [ ] `docs/development/COMMON_COMMANDS.md` - All CLI commands consolidated
- [ ] `docs/development/PERSISTENCE_LAYER.md` - Redis + PostgreSQL architecture
- [ ] `docs/development/SERVICE_REGISTRATION.md` - DI container setup
- [ ] `docs/development/CERTIFICATE_MANAGEMENT.md` - Certificate handling

---

## 📋 Pending (Phase 3)

### CLAUDE.md Rewrite (Critical)
- [ ] **Root CLAUDE.md** → High-level navigation index (~300 lines, ~3.5k tokens)
- [ ] **InteroperableResearchNode/CLAUDE.md** → Project essentials (~340 lines, ~5.5k tokens)

### Navigation Guide
- [ ] `docs/NAVIGATION_INDEX.md` - Role-based and topic-based navigation

### Additional Documentation
- [ ] `docs/KNOWN_ISSUES.md` - Consolidated issues and solutions
- [ ] Update `CHANGELOG.md` - Document reorganization

---

## Expected Token Reduction

### Before Reorganization:
```
Root CLAUDE.md:         10.2k tokens (1685 lines)
IRN CLAUDE.md:          19.9k tokens (1685 lines)
──────────────────────────────────────────────
Total:                  30.1k tokens (15.1% of context window)
```

### After Reorganization (Target):
```
Root CLAUDE.md:          ~3.5k tokens (~300 lines)
IRN CLAUDE.md:           ~5.5k tokens (~340 lines)
──────────────────────────────────────────────
Total:                   ~9k tokens (4.5% of context window)

REDUCTION: 70% fewer tokens, 80% fewer lines
```

### New Documentation Files (15+ files):
```
docs/
├── NAVIGATION_INDEX.md (new)
├── ARCHITECTURE_PHILOSOPHY.md (new)
├── SECURITY_OVERVIEW.md (new)
├── KNOWN_ISSUES.md (new)
├── components/
│   ├── INTEROPERABLE_RESEARCH_NODE.md (new)
│   ├── SEMG_DEVICE.md (new)
│   ├── INTERFACE_SYSTEM.md (new)
│   └── MOBILE_APP.md (new)
├── workflows/
│   ├── CHANNEL_FLOW.md (new)
│   ├── PHASE2_IDENTIFICATION_FLOW.md (new)
│   ├── PHASE3_AUTHENTICATION_FLOW.md (pending)
│   └── PHASE4_SESSION_FLOW.md (pending)
├── architecture/
│   ├── PROJECT_STRUCTURE.md (pending)
│   ├── GENERIC_BASE_PATTERN.md (pending)
│   ├── NODE_IDENTIFIER_ARCHITECTURE.md (pending)
│   └── ATTRIBUTE_BASED_REQUEST_PROCESSING.md (pending)
└── development/
    ├── COMMON_COMMANDS.md (pending)
    ├── PERSISTENCE_LAYER.md (pending)
    ├── SERVICE_REGISTRATION.md (pending)
    └── CERTIFICATE_MANAGEMENT.md (pending)
```

---

## Next Steps (Recommended Priority)

### Immediate (Phase 2 Completion):
1. Create remaining workflow files (Phase 3, Phase 4)
2. Extract architecture details from IRN CLAUDE.md
3. Extract development guides from both CLAUDE.md files

### Critical (Phase 3 - Token Reduction):
1. **Rewrite Root CLAUDE.md** as navigation index
   - Keep only: Quick navigation, component summary table, most common commands
   - Remove: All detailed descriptions (moved to docs/components/)

2. **Rewrite IRN CLAUDE.md** as project essentials
   - Keep only: Quick start, architecture summary, key endpoints
   - Remove: All detailed flows (moved to docs/workflows/), all database schema details

3. Create `NAVIGATION_INDEX.md` - Central documentation hub

### Final (Phase 4 - Cleanup):
1. Update CHANGELOG.md with reorganization notes
2. Verify all internal links
3. Test navigation from CLAUDE.md → detailed docs
4. Remove deprecated documentation (if any)

---

## Benefits of Reorganization

### For LLM Context Management:
- ✅ **70% token reduction** in core CLAUDE.md files
- ✅ **Lazy loading**: Only load detailed docs when needed
- ✅ **Faster context parsing**: Less text to process upfront
- ✅ **More room for code**: 109k free tokens for actual development

### For Human Developers:
- ✅ **Better organization**: Related content grouped together
- ✅ **Easier navigation**: Role-based and topic-based indexes
- ✅ **Reduced duplication**: Single source of truth for each topic
- ✅ **Maintainability**: Update one file instead of multiple CLAUDE.md sections

### For Project Maintenance:
- ✅ **Version control**: Smaller diffs, easier reviews
- ✅ **Modularity**: Update components independently
- ✅ **Scalability**: Add new components without bloating CLAUDE.md
- ✅ **Documentation debt**: Clear structure prevents technical debt accumulation

---

## How to Use This Reorganization

### For AI Assistants (Claude Code):
1. **Start**: Read condensed CLAUDE.md (~9k tokens)
2. **Navigate**: Use links to load specific docs when needed
3. **Deep Dive**: Only load detailed files for active tasks
4. **Example**: Working on Phase 3? Load `docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`

### For Human Developers:
1. **Start**: Read root CLAUDE.md for project overview
2. **Component Work**: Navigate to `docs/components/` for component details
3. **Implementation**: Use `docs/workflows/` for step-by-step guides
4. **Troubleshooting**: Check `docs/KNOWN_ISSUES.md`

---

## Completion Checklist

- [x] Plan reorganization strategy
- [x] Create new directory structure
- [x] Extract component documentation (4 files)
- [x] Extract architecture philosophy
- [x] Extract security overview
- [x] Extract workflow documentation (2 of 4)
- [ ] Complete workflow documentation (2 remaining)
- [ ] Extract architecture details (4 files)
- [ ] Extract development guides (4 files)
- [ ] Rewrite root CLAUDE.md
- [ ] Rewrite IRN CLAUDE.md
- [ ] Create navigation index
- [ ] Create known issues doc
- [ ] Update CHANGELOG.md
- [ ] Verify all links
- [ ] Test documentation flow

**Estimated Completion**: 60-70% complete

---

## Notes

- All new documentation follows English-only policy (PRISM documentation standards)
- Cross-references use relative paths for portability
- Files organized by consumption pattern (role-based, topic-based)
- Markdown files use consistent formatting (headers, code blocks, tables)
