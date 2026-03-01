# Known Issues

This document tracks known issues, limitations, and accepted technical debt in the InteroperableResearchNode backend.

---

## Active Issues

### Docker Network Configuration (Resolved 2025-10-08)

Two Docker Compose files must be used in order: persistence layer first, then application layer. All containers must share the same `irn-network`. Using a single compose file causes DNS resolution failures between containers.

**Status**: Documented pattern established. See `CLAUDE.md` for correct startup commands.

---

### RSA Signature Verification Tests (2025-10-03)

Two tests in the integration test suite fail due to an RSA signature verification edge case. This is a non-blocking known issue that does not affect production behavior.

**Status**: 73/75 tests passing (97.3%). Under investigation.

---

### Compiler Warning: Async Method Without Await

`NodeRegistryService.cs:44` — async method declared without an await expression. This is intentional; the method signature is required by the interface contract and the async wrapper is kept for future implementation.

**Status**: Accepted. Non-blocking.

---

## Phase 20 — Entity Mapping Corrections: Low-Severity Follow-Up Items

The following findings were identified during the Phase 20 TL review (`agent_docs/REVIEW_ENTITY_MAPPING_FIX.md`) and accepted for a follow-up cleanup pass. All medium-severity blockers (F-001, F-003, F-005) were resolved before the gate passed.

### F-002 — Duplicate Relationship Declaration in TargetAreaConfiguration

**File**: `Bioteca.Prism.Data/Persistence/Configurations/TargetAreaConfiguration.cs`

The `HasMany/WithOne` relationship between `TargetArea` and `TargetAreaTopographicalModifier` is declared in both `TargetAreaConfiguration` and `TargetAreaTopographicalModifierConfiguration`. EF Core resolves the duplication correctly because both declarations are consistent (same FK column, same cascade behavior). The redundant block in `TargetAreaConfiguration` adds noise without any runtime effect.

**Recommendation**: Remove the `HasMany(x => x.TopographicalModifiers).WithOne(x => x.TargetArea)` block from `TargetAreaConfiguration` and retain the declaration only in `TargetAreaTopographicalModifierConfiguration`, which is the conventional owned-side location.

**Severity**: Low. No behavior impact.

---

### F-006 — SyncImportService Update Path Does Not Repair Null TargetAreaId

**File**: `Bioteca.Prism.Service/Services/Sync/SyncImportService.cs` (update path, ~line 1158)

When an existing session is updated via sync import and already has a `TargetAreaId`, the update path correctly updates the TargetArea row in-place. However, if for any reason a session's `TargetAreaId` FK is null while a matching TargetArea row exists (e.g., a prior failed sync), the update path does not repair the FK pointer.

**Recommendation**: After the TargetArea upsert, unconditionally set `sessionToLink.TargetAreaId = taId` regardless of whether the session previously had a value.

**Severity**: Low. Only affects sessions in a partially-synced inconsistent state, which should not arise under normal operation.

---

### F-008 — Missing [JsonIgnore] on TargetArea.RecordSession Back-Navigation

**File**: `Bioteca.Prism.Domain/Entities/Record/TargetArea.cs`

The `RecordSession` back-navigation property on `TargetArea` is not annotated with `[System.Text.Json.Serialization.JsonIgnore]`. In `SyncExportService`, sessions are serialized directly via `System.Text.Json`. The `TargetArea` navigation is included via EF's `.Include()` chain, but the `TargetArea.RecordSession` back-nav is not explicitly loaded — so it is null at serialization time and does not cause a circular reference issue today.

However, if `ReferenceHandler` settings change or if navigation proxies are enabled in the future, this could produce circular reference errors or unexpected JSON output.

**Recommendation**: Add `[JsonIgnore]` to the `TargetArea.RecordSession` navigation property as a defensive measure.

**Severity**: Low. Latent risk only; no current impact.

---

### F-009 — Stale Comment in SyncImportService

**File**: `Bioteca.Prism.Service/Services/Sync/SyncImportService.cs` (~line 100)

A comment still reads `// → Sessions → Records → RecordChannels → TargetAreas`, reflecting the old entity structure where TargetArea was nested under RecordChannel. After Phase 20, TargetArea is a direct child of RecordSession.

**Recommendation**: Update the comment to `// → Sessions (with TargetArea) → Records → RecordChannels`.

**Severity**: Low. Documentation-only; no behavior impact.

---

## Pagination Limitation

`BaseController.HandleQueryParameters()` currently supports only `page` and `pageSize` query parameters. The `search` parameter is not handled at the base controller level and must be implemented per-controller if needed.

**Status**: Accepted limitation. Known before Phase 20.

---

*Last updated: 2026-03-01 (Phase 20 Entity Mapping Corrections)*
