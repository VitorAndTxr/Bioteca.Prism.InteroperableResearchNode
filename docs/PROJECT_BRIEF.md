# PROJECT BRIEF: Fix Volunteer-Clinical Entity Relationship Persistence

## Problem Statement

Volunteer clinical data (conditions, events, medications, allergies) is not being persisted to the database. The join tables `volunteer_clinical_conditions`, `volunteer_clinical_events`, `volunteer_medications`, and `volunteer_allergy_intolerances` are all empty (0 rows), while their SNOMED catalog counterparts (`clinical_conditions`, `clinical_events`, `medications`, `allergy_intolerances`) contain data.

## Root Cause Analysis

Two distinct bugs were identified, affecting different data flow paths:

### Bug 1: No API Endpoints Expose Volunteer Clinical CRUD (Primary Bug)

`IVolunteerClinicalService` is registered in DI (`NativeInjectorBootStrapper.cs:118`) and implemented in `VolunteerClinicalService.cs`, but **no controller injects or calls it**. The `VolunteerController` only manages the core `Volunteer` entity (create, update, delete, list) and has no endpoints for adding clinical conditions, events, medications, or allergies to a volunteer.

**Affected files:**
- `Bioteca.Prism.InteroperableResearchNode/Controllers/VolunteerController.cs` -- missing clinical sub-entity endpoints
- `Bioteca.Prism.Service/Services/Clinical/VolunteerClinicalService.cs` -- service exists but is unreachable from API
- `Bioteca.Prism.Service/Interfaces/Clinical/IVolunteerClinicalService.cs` -- interface defined but unused by controllers

### Bug 2: Missing DI Registrations for Generic Repositories (Blocking Bug)

Even if a controller were added, `VolunteerClinicalService` would fail at runtime with a DI resolution error. The service constructor requires:

```csharp
IBaseRepository<VolunteerClinicalCondition, Guid> conditionRepository
IBaseRepository<VolunteerClinicalEvent, Guid> eventRepository
IBaseRepository<VolunteerMedication, Guid> medicationRepository
IBaseRepository<VolunteerAllergyIntolerance, Guid> allergyRepository
```

None of these are registered in `NativeInjectorBootStrapper.RegisterRepositories()`. There is no open generic registration of `IBaseRepository<,>` to `BaseRepository<,>` either, so DI cannot auto-resolve them.

**Affected file:**
- `Bioteca.Prism.CrossCutting/NativeInjectorBootStrapper.cs` -- missing 4 repository registrations

### Bug 3: Missing ClinicalEvents Include in Sync Export (Secondary Bug)

`SyncExportService.GetVolunteersAsync()` (line 208-214) includes `VitalSigns`, `ClinicalConditions`, `Medications`, and `AllergyIntolerances` but **omits** `.Include(v => v.ClinicalEvents)`. This means even if clinical events were persisted, they would not be exported during node-to-node sync.

**Affected file:**
- `Bioteca.Prism.Service/Services/Sync/SyncExportService.cs:211-213` -- missing `.Include(v => v.ClinicalEvents)`

## What Works Correctly

- **Entity models** (`VolunteerClinicalCondition`, `VolunteerClinicalEvent`, `VolunteerMedication`, `VolunteerAllergyIntolerance`) are properly defined with all required properties and navigation properties.
- **EF Core configurations** (`VolunteerClinicalConditionConfiguration`, etc.) correctly map tables, columns, relationships, and indexes.
- **DbContext** has all four `DbSet<>` properties registered (`VolunteerClinicalConditions`, `VolunteerClinicalEvents`, `VolunteerMedications`, `VolunteerAllergyIntolerances`).
- **Database schema** is correct -- tables exist with proper FK constraints.
- **Sync Import** (`SyncImportService.UpsertVolunteerClinicalSubEntities`) correctly handles all four entity types using direct DbContext access (bypasses the missing repository registrations).
- **Sync Export** includes 3 of 4 entity types (missing ClinicalEvents).
- **Volunteer entity** has navigation collections for all four clinical types.

## Required Fixes

### Fix 1: Register Generic Repositories in DI

In `NativeInjectorBootStrapper.RegisterRepositories()`, add:

```csharp
using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Domain.Entities.Volunteer;

// Volunteer clinical repositories (generic)
services.AddScoped<IBaseRepository<VolunteerClinicalCondition, Guid>, BaseRepository<VolunteerClinicalCondition, Guid>>();
services.AddScoped<IBaseRepository<VolunteerClinicalEvent, Guid>, BaseRepository<VolunteerClinicalEvent, Guid>>();
services.AddScoped<IBaseRepository<VolunteerMedication, Guid>, BaseRepository<VolunteerMedication, Guid>>();
services.AddScoped<IBaseRepository<VolunteerAllergyIntolerance, Guid>, BaseRepository<VolunteerAllergyIntolerance, Guid>>();
```

Note: `BaseRepository` takes a `DbContext` in its constructor. Verify that `PrismDbContext` is registered as `DbContext` in the DI container, or use `PrismDbContext` directly.

### Fix 2: Add Volunteer Clinical API Endpoints

Create endpoints on `VolunteerController` (or a new `VolunteerClinicalController`) that expose the `IVolunteerClinicalService` methods:

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/volunteer/{id}/conditions` | List volunteer's clinical conditions |
| POST | `/api/volunteer/{id}/conditions` | Add a clinical condition |
| PUT | `/api/volunteer/{id}/conditions/{conditionId}` | Update a clinical condition |
| GET | `/api/volunteer/{id}/events` | List volunteer's clinical events |
| POST | `/api/volunteer/{id}/events` | Add a clinical event |
| GET | `/api/volunteer/{id}/medications` | List volunteer's medications |
| POST | `/api/volunteer/{id}/medications` | Add a medication |
| GET | `/api/volunteer/{id}/allergies` | List volunteer's allergies |
| POST | `/api/volunteer/{id}/allergies` | Add an allergy |
| GET | `/api/volunteer/{id}/clinical-summary` | Get clinical summary |

Payload DTOs will need to be created for each clinical entity type (or reuse the entity directly if acceptable for the project's patterns).

### Fix 3: Add Missing ClinicalEvents Include in Sync Export

In `SyncExportService.GetVolunteersAsync()`, add the missing include:

```csharp
var query = _context.Volunteers
    .AsNoTracking()
    .Include(v => v.VitalSigns)
    .Include(v => v.ClinicalConditions)
    .Include(v => v.ClinicalEvents)        // <-- ADD THIS
    .Include(v => v.Medications)
    .Include(v => v.AllergyIntolerances)
    .AsQueryable();
```

## Implementation Priority

1. **Fix 2 (DI registrations)** -- prerequisite, quick fix, unblocks everything
2. **Fix 1 (API endpoints)** -- primary deliverable, enables clinical data entry
3. **Fix 3 (Sync export)** -- secondary, ensures data propagation across nodes

## Architecture Notes

- The `BaseRepository<TEntity, TKey>` constructor requires `DbContext` and `IApiContext`. Verify that `PrismDbContext` resolves as `DbContext` in the DI container (it is registered via `AddDbContext<PrismDbContext>` which typically registers as both `PrismDbContext` and `DbContext`).
- `VolunteerClinicalService` fetches all records then filters in memory (e.g., `GetAllAsync().Where(c => c.VolunteerId == volunteerId)`). For production, this should be replaced with filtered queries, but that's a performance optimization outside the scope of this bug fix.
- The sync import path (`SyncImportService`) uses direct `DbContext` access and is unaffected by the missing repository registrations. It will continue to work as-is.
- New endpoints should follow the existing security pattern: `[PrismEncryptedChannelConnection]`, `[PrismAuthenticatedSession]`, `[Authorize("sub")]`.

## Files Requiring Changes

| File | Change |
|------|--------|
| `Bioteca.Prism.CrossCutting/NativeInjectorBootStrapper.cs` | Add 4 generic repository registrations |
| `Bioteca.Prism.InteroperableResearchNode/Controllers/VolunteerController.cs` | Add clinical sub-entity endpoints (or create new controller) |
| `Bioteca.Prism.Domain/Payloads/Volunteer/` | Create payload DTOs for clinical sub-entities |
| `Bioteca.Prism.Service/Services/Sync/SyncExportService.cs` | Add missing `.Include(v => v.ClinicalEvents)` |

## Open Questions

1. Should clinical endpoints live on the existing `VolunteerController` or a new `VolunteerClinicalController`? The existing pattern uses separate controllers per entity group.
2. Are dedicated repositories (with custom queries) needed for the volunteer clinical entities, or are the generic `BaseRepository` instances sufficient for now?
3. Should the `VolunteerClinicalService` in-memory filtering be addressed in this fix, or deferred as a separate performance task?
