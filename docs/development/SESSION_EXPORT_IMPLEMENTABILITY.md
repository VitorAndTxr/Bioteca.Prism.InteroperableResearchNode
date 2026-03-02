# Session Export Revision - Implementability Notes

**Date**: 2026-03-01
**Task**: Revise session export: fix target_area fields and include SNOMED data for research records
**Status**: Ready for Implementation
**Complexity**: LOW-MEDIUM
**Risk Level**: MINIMAL

---

## [NOTES] Implementability Assessment

### 1. Architecture & Data Model ✅ SOLID

**Current State**:
- `ResearchExportService` is well-structured with separation of concerns per entity group
- Data loading avoids Cartesian explosion via isolated queries + eager loading
- Anonymous object projections prevent EF Core circular reference issues
- RecordSession.TargetArea is correctly included with ThenInclude for TopographicalModifiers (line 94-95)

**Phase 20 Completion**: TargetArea ownership correctly reassigned to RecordSession (verified in entity definition)

**Rating**: ✅ **NO BLOCKERS** - Architecture is sound for this enhancement

---

### 2. Required Changes - Scope & Complexity

#### 2.1 Primary Fix: Enrich TargetArea Export (Lines 276-285)

**What**: Replace code-only export with SNOMED data included

**Current Output**:
```json
{
  "targetAreaId": "guid",
  "targetArea": {
    "id": "guid",
    "bodyStructureCode": "123456",
    "lateralityCode": "7771000",
    "topographicalModifierCodes": ["..."]
  }
}
```

**Required Output**:
```json
{
  "targetAreaId": "guid",
  "targetArea": {
    "id": "guid",
    "bodyStructure": {
      "code": "123456",
      "displayName": "Muscle of arm",
      "description": "...",
      "structureType": "Muscle"
    },
    "laterality": {
      "code": "7771000",
      "displayName": "Left",
      "description": "..."
    },
    "topographicalModifiers": [
      {
        "code": "...",
        "displayName": "Distal to",
        "category": "..."
      }
    ]
  }
}
```

**Effort**: 1-2 hours
- Requires NO schema changes (SNOMED entities already exist in DB)
- Modify anonymous object projection in ResearchExportService (lines 270-295)
- Add nested navigation properties to include display names
- Test export ZIP structure

**Risk**: MINIMAL
- SNOMED entities have required FK relationships defined
- TargetArea already includes TopographicalModifiers collection (line 95)
- No database changes needed

#### 2.2 Secondary: Ensure Cascade Loading for Record Data (Optional Enhancement)

**Current State**: Records and RecordChannels are loaded (lines 96-97)

**Issue**: RecordChannel.SensorId references exist but Sensor display names not exported

**Recommendation**: Include sensor metadata in channel export if research analysis requires sensor specifications

**Effort**: 1 hour (add ThenInclude for Device → Sensors)

**Risk**: LOW

---

### 3. Potential Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| **NULL Reference**: TargetArea is optional; accessing properties without null check | Runtime exception | MEDIUM | Add null guards and test null scenarios |
| **Circular Reference**: SNOMED entities might have reciprocal navigation | Infinite JSON | LOW | Verify no circular navs; use [JsonIgnore] on SNOMED reverse navs (already applied in SnomedBodyStructure) |
| **Performance**: Expanded query with ThenInclude chains | Large ZIP file / slow export | LOW | Monitor query execution; current pattern (separate queries) already mitigates Cartesian explosion |
| **Missing Data**: SNOMED codes don't exist in DB | Missing JSON fields | LOW | Add data validation test before export; handle gracefully with fallback nulls |
| **Breaking Change**: Consumers expecting old format | Integration failures | MEDIUM | This is a NEW export format; versioning via ZIP structure recommended |

---

### 4. Implementation Strategy

### Step 1: Prepare Test Data
- Verify existing test database has TargetAreas with linked SNOMED records
- If not, add seed data via EF Core migrations or test fixtures
- **Effort**: 15-30 min

### Step 2: Modify ResearchExportService.ExportAsync()
**File**: `Bioteca.Prism.Service/Services/Research/ResearchExportService.cs`

**Changes**:
1. Lines 88-98: Query already includes TargetArea + TopographicalModifiers ✓
2. Lines 270-295: Enhance session.json projection:
   - Replace code-only TargetArea object with full SNOMED entity projection
   - Access `session.TargetArea.BodyStructure` for body structure details
   - Access `session.TargetArea.Laterality` for laterality details
   - Map TopographicalModifiers collection to include DisplayName + Category

**Code Pattern**:
```csharp
TargetArea = session.TargetArea == null ? null : new
{
    Id = session.TargetArea.Id,
    BodyStructure = new
    {
        Code = session.TargetArea.BodyStructureCode,
        DisplayName = session.TargetArea.BodyStructure.DisplayName,
        Description = session.TargetArea.BodyStructure.Description,
        StructureType = session.TargetArea.BodyStructure.StructureType
    },
    Laterality = session.TargetArea.Laterality == null ? null : new
    {
        Code = session.TargetArea.LateralityCode,
        DisplayName = session.TargetArea.Laterality.DisplayName,
        Description = session.TargetArea.Laterality.Description
    },
    TopographicalModifiers = session.TargetArea.TopographicalModifiers.Select(tm => new
    {
        Code = tm.TopographicalModifierCode,
        DisplayName = tm.TopographicalModifier.DisplayName,
        Category = tm.TopographicalModifier.Category,
        Description = tm.TopographicalModifier.Description
    }).ToList()
}
```

**Effort**: 30-45 min (copy-paste + syntax validation)

### Step 3: Enhance Eager Loading (if needed)
**File**: `ResearchExportService.ExportAsync()`, lines 88-98

Current:
```csharp
.Include(s => s.TargetArea)
    .ThenInclude(ta => ta!.TopographicalModifiers)
```

Optional add (for sensor metadata):
```csharp
.Include(s => s.Records)
    .ThenInclude(r => r.RecordChannels)
        .ThenInclude(rc => rc.Sensor) // NEW
```

**Effort**: 5 min

**Test**: Verify no N+1 queries via SQL profiler

### Step 4: Write Unit Test
**File**: `Bioteca.Prism.InteroperableResearchNode.Test/ResearchExportServiceTests.cs`

**Test Case**:
- Create research + session + targetArea with SNOMED data
- Export research
- Verify ZIP contains session.json
- Deserialize JSON and assert:
  - TargetArea.BodyStructure.DisplayName populated
  - TargetArea.Laterality.DisplayName populated
  - TargetArea.TopographicalModifiers[0].DisplayName populated

**Effort**: 45 min

### Step 5: Integration Test (End-to-End)
**File**: Same test file

**Test Case**:
- Run full Phase 1-4 handshake with data export endpoint
- Verify encrypted response decodes to valid ZIP
- Validate JSON schema

**Effort**: 1 hour (depends on existing test harness)

### Step 6: Manual Verification
1. Start docker-compose (persistence + application)
2. Create research via Swagger
3. Create session + targetArea via API
4. Call export endpoint
5. Extract ZIP + inspect session.json
6. Verify SNOMED fields populated

**Effort**: 30 min

---

### 5. Dependencies & Blockers

| Dependency | Status | Impact |
|-----------|--------|--------|
| RecordSession.TargetArea navigation property | ✅ EXISTS | NONE - already loaded |
| TargetArea.BodyStructure FK + navigation | ✅ EXISTS | NONE - verified in entity |
| TargetArea.Laterality FK + navigation | ✅ EXISTS | NONE - verified in entity |
| TargetAreaTopographicalModifier join table | ✅ EXISTS | NONE - correctly modeled |
| SnomedBodyStructure display fields | ✅ EXISTS | NONE - all needed fields present |
| SnomedTopographicalModifier display fields | ✅ EXISTS | NONE - all needed fields present |
| EF Core eager loading ThenInclude support | ✅ EXISTS | NONE - already using v8.0.10 |
| Test data with SNOMED linkages | ⚠️ VERIFY | LOW - may need seed data |

**No API blockers identified.** Ready to proceed.

---

### 6. Version/Release Considerations

**Breaking Change**: YES (export format enhanced)

**Recommendation**:
1. Version the export format (e.g., `_export_version: 2` in root ZIP)
2. Consumers should handle both v1 (code-only) and v2 (enriched) formats during transition
3. Document in CHANGELOG.md

---

### 7. Testing Matrix

| Test Type | Scenario | Status |
|-----------|----------|--------|
| Unit | TargetArea null | TODO |
| Unit | TargetArea with all SNOMED fields | TODO |
| Unit | TargetArea with null Laterality | TODO |
| Unit | Multiple topographical modifiers | TODO |
| Integration | Full export ZIP structure | TODO |
| Integration | Federated sync with enriched data | TODO |
| Manual | Swagger export endpoint | TODO |
| Performance | Large research with 1000+ sessions | TODO |

---

### 8. Code Review Checklist

- [ ] Anonymous object projection includes all SNOMED display fields
- [ ] Null reference guards for optional TargetArea
- [ ] Null reference guards for optional Laterality
- [ ] No circular reference issues in JSON serialization
- [ ] Eager loading queries optimized (no N+1)
- [ ] Export ZIP schema documented
- [ ] Test coverage > 80%
- [ ] CHANGELOG.md updated with breaking change notice
- [ ] API documentation updated

---

## Summary

### Green Lights ✅
1. Data model correctly restructured (Phase 20 complete)
2. SNOMED entities have all required fields
3. EF Core queries properly structured
4. No database schema changes needed
5. Low implementation complexity (JSON projection change only)

### Recommendations
1. **DO**: Implement full SNOMED enrichment as described in Step 2
2. **DO**: Add null guards for optional TargetArea/Laterality
3. **CONSIDER**: Add sensor metadata to record export (Step 2.2)
4. **VERIFY**: Test data has TargetAreas linked to SNOMED records
5. **DOCUMENT**: Version the export format for consumer compatibility

### Confidence Level
**HIGH (90%)** - Straightforward implementation with minimal risk. All architectural components in place.

---

**Next Steps for Implementer**:
1. Verify test database has SNOMED-linked TargetAreas
2. Begin implementation in Step 2 (ResearchExportService)
3. Write test before running implementation for TDD approach
4. Run full docker-compose test cycle before merge
