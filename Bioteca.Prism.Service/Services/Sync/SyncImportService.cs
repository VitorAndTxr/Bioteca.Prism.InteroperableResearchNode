using System.Text.Json;
using Azure.Storage.Blobs;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Sync;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Domain.Entities.Device;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Domain.Entities.Sync;
using Bioteca.Prism.Domain.Entities.Volunteer;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// Aliases resolve namespace/type name collisions for entities sharing their namespace name
using ApplicationEntity = Bioteca.Prism.Domain.Entities.Application.Application;
using VolunteerEntity = Bioteca.Prism.Domain.Entities.Volunteer.Volunteer;

namespace Bioteca.Prism.Service.Services.Sync;

/// <summary>
/// Imports entities from a remote node payload in a single EF Core transaction.
/// Implements "newer wins" conflict resolution using UpdatedAt watermarks.
/// Bypasses BaseRepository intentionally — upsert semantics require direct DbContext access.
/// </summary>
public class SyncImportService : ISyncImportService
{
    private readonly PrismDbContext _context;
    private readonly INodeRepository _nodeRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncImportService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SyncImportService(
        PrismDbContext context,
        INodeRepository nodeRepository,
        ISyncLogRepository syncLogRepository,
        IConfiguration configuration,
        ILogger<SyncImportService> logger)
    {
        _context = context;
        _nodeRepository = nodeRepository;
        _syncLogRepository = syncLogRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SyncResultDTO> ImportAsync(SyncImportPayload payload, Guid remoteNodeId)
    {
        var startedAt = DateTime.UtcNow;
        var syncLogId = Guid.NewGuid();

        var counts = new Dictionary<string, int>
        {
            ["snomed"] = 0,
            ["volunteers"] = 0,
            ["research"] = 0,
            ["sessions"] = 0,
            ["recordings"] = 0
        };

        // Open the transaction FIRST so the SyncLog insert is covered by it.
        // On rollback, the SyncLog row is also rolled back, preventing orphaned "in_progress" entries.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create SyncLog inside the transaction using the DbContext directly.
            // Bypassing the repository here avoids an implicit SaveChangesAsync that would commit
            // the row outside the current transaction.
            var syncLog = new SyncLog
            {
                Id = syncLogId,
                RemoteNodeId = remoteNodeId,
                StartedAt = startedAt,
                Status = "in_progress"
            };
            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            // Resolve the local node ID so we can set ResearchNodeId on imported entities.
            // Volunteer and Research rows from the remote node carry the remote node's ID —
            // we must rewrite them to our own node ID so FK constraints are satisfied locally.
            var localNodes = await _nodeRepository.GetAllAsync();
            var localNodeId = localNodes.FirstOrDefault()?.Id
                ?? throw new InvalidOperationException("Local node not found in node registry.");

            // Dependency order: SNOMED catalog first (referenced by Volunteer clinical data and TargetAreas)
            // → Volunteers (referenced by Research and Sessions)
            // → Research (referenced by Sessions)
            // → Sessions → Records → RecordChannels → TargetAreas
            // → Recordings (blob files stored after metadata is committed)

            counts["snomed"] = await ImportSnomedAsync(payload.Snomed ?? new SyncSnomedPayload());
            await _context.SaveChangesAsync();

            counts["volunteers"] = await ImportVolunteersAsync(payload.Volunteers, localNodeId);
            await _context.SaveChangesAsync();

            counts["research"] = await ImportResearchAsync(payload.Research, localNodeId);
            await _context.SaveChangesAsync();

            counts["sessions"] = await ImportSessionsAsync(payload.Sessions);
            await _context.SaveChangesAsync();

            var completedAt = DateTime.UtcNow;
            syncLog.Status = "completed";
            syncLog.CompletedAt = completedAt;
            syncLog.LastSyncedAt = payload.ManifestGeneratedAt;
            syncLog.EntitiesReceived = JsonSerializer.Serialize(counts);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Blob uploads happen outside the DB transaction — they are idempotent
            counts["recordings"] = await ImportRecordingsAsync(payload.Recordings);

            return new SyncResultDTO
            {
                Status = "completed",
                StartedAt = startedAt,
                CompletedAt = completedAt,
                EntitiesReceived = counts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync import from remote node {RemoteNodeId} failed", remoteNodeId);

            try { await transaction.RollbackAsync(); }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Transaction rollback failed for sync import");
            }

            // The main transaction was rolled back, including the SyncLog insert.
            // We must log the failure in a separate, independent operation so that
            // monitoring and the UI can see what went wrong.
            await LogSyncFailureAsync(syncLogId, remoteNodeId, startedAt, counts, ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Persists a failed SyncLog entry using a fresh SaveChangesAsync call, independent of any
    /// previously rolled-back transaction. This guarantees the failure record reaches the database
    /// even when the main import transaction was rolled back.
    /// </summary>
    private async Task LogSyncFailureAsync(
        Guid syncLogId,
        Guid remoteNodeId,
        DateTime startedAt,
        Dictionary<string, int> counts,
        string errorMessage)
    {
        try
        {
            // After a rollback, the DbContext change tracker may hold stale entries.
            // Detach everything to start clean before writing the failure log.
            _context.ChangeTracker.Clear();

            _context.SyncLogs.Add(new SyncLog
            {
                Id = syncLogId,
                RemoteNodeId = remoteNodeId,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                Status = "failed",
                ErrorMessage = errorMessage,
                EntitiesReceived = JsonSerializer.Serialize(counts)
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "Failed to persist sync log failure record for {SyncLogId}", syncLogId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SNOMED catalog import
    // Each entity type arrives in its named bucket (SyncSnomedPayload) so no
    // type-inference heuristics are needed. PK naming differences are handled
    // per method: SnomedBodyRegion/SnomedBodyStructure use SnomedCode,
    // SnomedLaterality/SnomedTopographicalModifier/SnomedSeverityCode use Code.
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> ImportSnomedAsync(SyncSnomedPayload snomed)
    {
        int count = 0;

        foreach (var item in snomed.BodyRegions)
            count += await UpsertBodyRegionAsync(item, GetString(item, "snomedCode"));

        foreach (var item in snomed.BodyStructures)
            count += await UpsertBodyStructureAsync(item, GetString(item, "snomedCode"));

        foreach (var item in snomed.Lateralities)
            count += await UpsertLateralityAsync(item, GetString(item, "code"));

        foreach (var item in snomed.TopographicalModifiers)
            count += await UpsertTopographicalModifierAsync(item);

        foreach (var item in snomed.SeverityCodes)
            count += await UpsertSeverityCodeAsync(item, GetString(item, "code"));

        foreach (var item in snomed.ClinicalConditions)
            count += await UpsertClinicalConditionAsync(item, GetString(item, "snomedCode"));

        foreach (var item in snomed.ClinicalEvents)
            count += await UpsertClinicalEventAsync(item, GetString(item, "snomedCode"));

        foreach (var item in snomed.Medications)
            count += await UpsertMedicationAsync(item, GetString(item, "snomedCode"));

        foreach (var item in snomed.AllergyIntolerances)
            count += await UpsertAllergyIntoleranceAsync(item, GetString(item, "snomedCode"));

        return count;
    }

    private async Task<int> UpsertBodyRegionAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.SnomedBodyRegions.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.SnomedBodyRegions.Add(new SnomedBodyRegion
            {
                SnomedCode = snomedCode,
                DisplayName = GetString(item, "displayName"),
                ParentRegionCode = TryGetString(item, "parentRegionCode"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.ParentRegionCode = TryGetString(item, "parentRegionCode");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertBodyStructureAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.SnomedBodyStructures.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.SnomedBodyStructures.Add(new SnomedBodyStructure
            {
                SnomedCode = snomedCode,
                BodyRegionCode = GetString(item, "bodyRegionCode"),
                DisplayName = GetString(item, "displayName"),
                StructureType = GetString(item, "structureType"),
                ParentStructureCode = TryGetString(item, "parentStructureCode"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.BodyRegionCode = GetString(item, "bodyRegionCode");
            existing.DisplayName = GetString(item, "displayName");
            existing.StructureType = GetString(item, "structureType");
            existing.ParentStructureCode = TryGetString(item, "parentStructureCode");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertLateralityAsync(JsonElement item, string code)
    {
        var existing = await _context.SnomedLateralities.FindAsync(code);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.SnomedLateralities.Add(new SnomedLaterality
            {
                Code = code,
                DisplayName = GetString(item, "displayName"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertTopographicalModifierAsync(JsonElement item)
    {
        var code = GetString(item, "code");
        var existing = await _context.SnomedTopographicalModifiers.FindAsync(code);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.SnomedTopographicalModifiers.Add(new SnomedTopographicalModifier
            {
                Code = code,
                DisplayName = GetString(item, "displayName"),
                Category = GetString(item, "category"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.Category = GetString(item, "category");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertSeverityCodeAsync(JsonElement item, string code)
    {
        var existing = await _context.SnomedSeverityCodes.FindAsync(code);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.SnomedSeverityCodes.Add(new SnomedSeverityCode
            {
                Code = code,
                DisplayName = GetString(item, "displayName"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertClinicalConditionAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.ClinicalConditions.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.ClinicalConditions.Add(new ClinicalCondition
            {
                SnomedCode = snomedCode,
                DisplayName = GetString(item, "displayName"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertClinicalEventAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.ClinicalEvents.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.ClinicalEvents.Add(new ClinicalEvent
            {
                SnomedCode = snomedCode,
                DisplayName = GetString(item, "displayName"),
                Description = GetString(item, "description"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.DisplayName = GetString(item, "displayName");
            existing.Description = GetString(item, "description");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertMedicationAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.Medications.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.Medications.Add(new Medication
            {
                SnomedCode = snomedCode,
                MedicationName = GetString(item, "medicationName"),
                ActiveIngredient = GetString(item, "activeIngredient"),
                AnvisaCode = GetString(item, "anvisaCode"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.MedicationName = GetString(item, "medicationName");
            existing.ActiveIngredient = GetString(item, "activeIngredient");
            existing.AnvisaCode = GetString(item, "anvisaCode");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    private async Task<int> UpsertAllergyIntoleranceAsync(JsonElement item, string snomedCode)
    {
        var existing = await _context.AllergyIntolerances.FindAsync(snomedCode);
        var updatedAt = GetDateTime(item, "updatedAt");

        if (existing == null)
        {
            _context.AllergyIntolerances.Add(new AllergyIntolerance
            {
                SnomedCode = snomedCode,
                Category = GetString(item, "category"),
                SubstanceName = GetString(item, "substanceName"),
                Type = GetString(item, "type"),
                IsActive = GetBool(item, "isActive"),
                CreatedAt = GetDateTime(item, "createdAt"),
                UpdatedAt = updatedAt
            });
            return 1;
        }

        if (updatedAt > existing.UpdatedAt)
        {
            existing.Category = GetString(item, "category");
            existing.SubstanceName = GetString(item, "substanceName");
            existing.Type = GetString(item, "type");
            existing.IsActive = GetBool(item, "isActive");
            existing.UpdatedAt = updatedAt;
        }
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Volunteer import (with nested clinical sub-entities)
    // ResearchNodeId is overwritten to local node ID (the remote value must not leak).
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> ImportVolunteersAsync(List<JsonElement> items, Guid localNodeId)
    {
        int count = 0;
        foreach (var item in items)
        {
            var id = GetGuid(item, "volunteerId");
            var existing = await _context.Volunteers
                .Include(v => v.ClinicalConditions)
                .Include(v => v.ClinicalEvents)
                .Include(v => v.Medications)
                .Include(v => v.AllergyIntolerances)
                .Include(v => v.VitalSigns)
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            var updatedAt = GetDateTime(item, "updatedAt");

            if (existing == null)
            {
                var volunteer = new VolunteerEntity
                {
                    VolunteerId = id,
                    ResearchNodeId = localNodeId,
                    VolunteerCode = GetString(item, "volunteerCode"),
                    Name = GetString(item, "name"),
                    Email = GetString(item, "email"),
                    BirthDate = GetDateTime(item, "birthDate"),
                    Gender = GetString(item, "gender"),
                    BloodType = GetString(item, "bloodType"),
                    Height = TryGetFloat(item, "height"),
                    Weight = TryGetFloat(item, "weight"),
                    MedicalHistory = GetString(item, "medicalHistory"),
                    ConsentStatus = GetString(item, "consentStatus"),
                    EnrolledAt = GetDateTime(item, "enrolledAt"),
                    UpdatedAt = updatedAt
                };
                _context.Volunteers.Add(volunteer);
                count++;

                // Insert nested clinical sub-entities on first import
                UpsertVolunteerClinicalSubEntities(item, id, isNew: true);
            }
            else if (updatedAt > existing.UpdatedAt)
            {
                existing.ResearchNodeId = localNodeId;
                existing.VolunteerCode = GetString(item, "volunteerCode");
                existing.Name = GetString(item, "name");
                existing.Email = GetString(item, "email");
                existing.BirthDate = GetDateTime(item, "birthDate");
                existing.Gender = GetString(item, "gender");
                existing.BloodType = GetString(item, "bloodType");
                existing.Height = TryGetFloat(item, "height");
                existing.Weight = TryGetFloat(item, "weight");
                existing.MedicalHistory = GetString(item, "medicalHistory");
                existing.ConsentStatus = GetString(item, "consentStatus");
                existing.UpdatedAt = updatedAt;

                // Sync nested clinical sub-entities
                UpsertVolunteerClinicalSubEntities(item, id, isNew: false);
            }
        }
        return count;
    }

    private void UpsertVolunteerClinicalSubEntities(JsonElement item, Guid volunteerId, bool isNew)
    {
        // VitalSigns
        if (item.TryGetProperty("vitalSigns", out var vitalSignsArray))
        {
            foreach (var vs in vitalSignsArray.EnumerateArray())
            {
                var vsId = GetGuid(vs, "id");
                var vsExisting = isNew ? null : _context.VitalSigns
                    .Local.FirstOrDefault(v => v.Id == vsId);

                if (vsExisting == null && !isNew)
                    vsExisting = _context.VitalSigns.Find(vsId);

                var vsUpdatedAt = GetDateTime(vs, "updatedAt");
                if (vsExisting == null)
                {
                    _context.VitalSigns.Add(new VitalSigns
                    {
                        Id = vsId,
                        VolunteerId = volunteerId,
                        RecordSessionId = GetGuid(vs, "recordSessionId"),
                        SystolicBp = TryGetFloat(vs, "systolicBp"),
                        DiastolicBp = TryGetFloat(vs, "diastolicBp"),
                        HeartRate = TryGetFloat(vs, "heartRate"),
                        RespiratoryRate = TryGetFloat(vs, "respiratoryRate"),
                        Temperature = TryGetFloat(vs, "temperature"),
                        OxygenSaturation = TryGetFloat(vs, "oxygenSaturation"),
                        Weight = TryGetFloat(vs, "weight"),
                        Height = TryGetFloat(vs, "height"),
                        Bmi = TryGetFloat(vs, "bmi"),
                        MeasurementDatetime = GetDateTime(vs, "measurementDatetime"),
                        MeasurementContext = GetString(vs, "measurementContext"),
                        RecordedBy = GetGuid(vs, "recordedBy"),
                        CreatedAt = GetDateTime(vs, "createdAt"),
                        UpdatedAt = vsUpdatedAt
                    });
                }
                else if (vsUpdatedAt > vsExisting.UpdatedAt)
                {
                    vsExisting.SystolicBp = TryGetFloat(vs, "systolicBp");
                    vsExisting.DiastolicBp = TryGetFloat(vs, "diastolicBp");
                    vsExisting.HeartRate = TryGetFloat(vs, "heartRate");
                    vsExisting.RespiratoryRate = TryGetFloat(vs, "respiratoryRate");
                    vsExisting.Temperature = TryGetFloat(vs, "temperature");
                    vsExisting.OxygenSaturation = TryGetFloat(vs, "oxygenSaturation");
                    vsExisting.Weight = TryGetFloat(vs, "weight");
                    vsExisting.Height = TryGetFloat(vs, "height");
                    vsExisting.Bmi = TryGetFloat(vs, "bmi");
                    vsExisting.MeasurementContext = GetString(vs, "measurementContext");
                    vsExisting.UpdatedAt = vsUpdatedAt;
                }
            }
        }

        // ClinicalConditions
        if (item.TryGetProperty("clinicalConditions", out var conditionsArray))
        {
            foreach (var cond in conditionsArray.EnumerateArray())
            {
                var condId = GetGuid(cond, "id");
                var condExisting = isNew ? null : _context.VolunteerClinicalConditions.Find(condId);
                var condUpdatedAt = GetDateTime(cond, "updatedAt");

                if (condExisting == null)
                {
                    _context.VolunteerClinicalConditions.Add(new VolunteerClinicalCondition
                    {
                        Id = condId,
                        VolunteerId = volunteerId,
                        SnomedCode = GetString(cond, "snomedCode"),
                        ClinicalStatus = GetString(cond, "clinicalStatus"),
                        OnsetDate = TryGetDateTime(cond, "onsetDate"),
                        AbatementDate = TryGetDateTime(cond, "abatementDate"),
                        SeverityCode = TryGetString(cond, "severityCode"),
                        VerificationStatus = GetString(cond, "verificationStatus"),
                        ClinicalNotes = GetString(cond, "clinicalNotes"),
                        RecordedBy = GetGuid(cond, "recordedBy"),
                        CreatedAt = GetDateTime(cond, "createdAt"),
                        UpdatedAt = condUpdatedAt
                    });
                }
                else if (condUpdatedAt > condExisting.UpdatedAt)
                {
                    condExisting.ClinicalStatus = GetString(cond, "clinicalStatus");
                    condExisting.OnsetDate = TryGetDateTime(cond, "onsetDate");
                    condExisting.AbatementDate = TryGetDateTime(cond, "abatementDate");
                    condExisting.SeverityCode = TryGetString(cond, "severityCode");
                    condExisting.VerificationStatus = GetString(cond, "verificationStatus");
                    condExisting.ClinicalNotes = GetString(cond, "clinicalNotes");
                    condExisting.UpdatedAt = condUpdatedAt;
                }
            }
        }

        // ClinicalEvents
        if (item.TryGetProperty("clinicalEvents", out var eventsArray))
        {
            foreach (var ev in eventsArray.EnumerateArray())
            {
                var evId = GetGuid(ev, "id");
                var evExisting = isNew ? null : _context.VolunteerClinicalEvents.Find(evId);
                var evUpdatedAt = GetDateTime(ev, "updatedAt");

                if (evExisting == null)
                {
                    _context.VolunteerClinicalEvents.Add(new VolunteerClinicalEvent
                    {
                        Id = evId,
                        VolunteerId = volunteerId,
                        EventType = GetString(ev, "eventType"),
                        SnomedCode = GetString(ev, "snomedCode"),
                        EventDatetime = GetDateTime(ev, "eventDatetime"),
                        DurationMinutes = TryGetInt(ev, "durationMinutes"),
                        SeverityCode = TryGetString(ev, "severityCode"),
                        NumericValue = TryGetFloat(ev, "numericValue"),
                        ValueUnit = GetString(ev, "valueUnit"),
                        Characteristics = GetString(ev, "characteristics"),
                        TargetAreaId = TryGetGuid(ev, "targetAreaId"),
                        RecordSessionId = TryGetGuid(ev, "recordSessionId"),
                        RecordedBy = GetGuid(ev, "recordedBy"),
                        CreatedAt = GetDateTime(ev, "createdAt"),
                        UpdatedAt = evUpdatedAt
                    });
                }
                else if (evUpdatedAt > evExisting.UpdatedAt)
                {
                    evExisting.EventType = GetString(ev, "eventType");
                    evExisting.EventDatetime = GetDateTime(ev, "eventDatetime");
                    evExisting.DurationMinutes = TryGetInt(ev, "durationMinutes");
                    evExisting.SeverityCode = TryGetString(ev, "severityCode");
                    evExisting.NumericValue = TryGetFloat(ev, "numericValue");
                    evExisting.ValueUnit = GetString(ev, "valueUnit");
                    evExisting.Characteristics = GetString(ev, "characteristics");
                    evExisting.UpdatedAt = evUpdatedAt;
                }
            }
        }

        // Medications
        if (item.TryGetProperty("medications", out var medsArray))
        {
            foreach (var med in medsArray.EnumerateArray())
            {
                var medId = GetGuid(med, "id");
                var medExisting = isNew ? null : _context.VolunteerMedications.Find(medId);
                var medUpdatedAt = GetDateTime(med, "updatedAt");

                if (medExisting == null)
                {
                    _context.VolunteerMedications.Add(new VolunteerMedication
                    {
                        Id = medId,
                        VolunteerId = volunteerId,
                        MedicationSnomedCode = GetString(med, "medicationSnomedCode"),
                        ConditionId = TryGetGuid(med, "conditionId"),
                        Dosage = GetString(med, "dosage"),
                        Frequency = GetString(med, "frequency"),
                        Route = GetString(med, "route"),
                        StartDate = GetDateTime(med, "startDate"),
                        EndDate = TryGetDateTime(med, "endDate"),
                        Status = GetString(med, "status"),
                        Notes = GetString(med, "notes"),
                        RecordedBy = GetGuid(med, "recordedBy"),
                        CreatedAt = GetDateTime(med, "createdAt"),
                        UpdatedAt = medUpdatedAt
                    });
                }
                else if (medUpdatedAt > medExisting.UpdatedAt)
                {
                    medExisting.Dosage = GetString(med, "dosage");
                    medExisting.Frequency = GetString(med, "frequency");
                    medExisting.Route = GetString(med, "route");
                    medExisting.EndDate = TryGetDateTime(med, "endDate");
                    medExisting.Status = GetString(med, "status");
                    medExisting.Notes = GetString(med, "notes");
                    medExisting.UpdatedAt = medUpdatedAt;
                }
            }
        }

        // AllergyIntolerances
        if (item.TryGetProperty("allergyIntolerances", out var allergiesArray))
        {
            foreach (var allergy in allergiesArray.EnumerateArray())
            {
                var aId = GetGuid(allergy, "id");
                var aExisting = isNew ? null : _context.VolunteerAllergyIntolerances.Find(aId);
                var aUpdatedAt = GetDateTime(allergy, "updatedAt");

                if (aExisting == null)
                {
                    _context.VolunteerAllergyIntolerances.Add(new VolunteerAllergyIntolerance
                    {
                        Id = aId,
                        VolunteerId = volunteerId,
                        AllergyIntoleranceSnomedCode = GetString(allergy, "allergyIntoleranceSnomedCode"),
                        Criticality = GetString(allergy, "criticality"),
                        ClinicalStatus = GetString(allergy, "clinicalStatus"),
                        Manifestations = GetString(allergy, "manifestations"),
                        OnsetDate = TryGetDateTime(allergy, "onsetDate"),
                        LastOccurrence = TryGetDateTime(allergy, "lastOccurrence"),
                        VerificationStatus = GetString(allergy, "verificationStatus"),
                        RecordedBy = GetGuid(allergy, "recordedBy"),
                        CreatedAt = GetDateTime(allergy, "createdAt"),
                        UpdatedAt = aUpdatedAt
                    });
                }
                else if (aUpdatedAt > aExisting.UpdatedAt)
                {
                    aExisting.Criticality = GetString(allergy, "criticality");
                    aExisting.ClinicalStatus = GetString(allergy, "clinicalStatus");
                    aExisting.Manifestations = GetString(allergy, "manifestations");
                    aExisting.OnsetDate = TryGetDateTime(allergy, "onsetDate");
                    aExisting.LastOccurrence = TryGetDateTime(allergy, "lastOccurrence");
                    aExisting.VerificationStatus = GetString(allergy, "verificationStatus");
                    aExisting.UpdatedAt = aUpdatedAt;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Research import (with nested Applications, ResearchDevices, join tables)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> ImportResearchAsync(List<JsonElement> items, Guid localNodeId)
    {
        int count = 0;
        foreach (var item in items)
        {
            var id = GetGuid(item, "id");
            var existing = await _context.Research
                .Include(r => r.Applications)
                .Include(r => r.ResearchDevices)
                .Include(r => r.ResearchResearchers)
                .Include(r => r.ResearchVolunteers)
                .FirstOrDefaultAsync(r => r.Id == id);

            var updatedAt = GetDateTime(item, "updatedAt");

            if (existing == null)
            {
                _context.Research.Add(new Domain.Entities.Research.Research
                {
                    Id = id,
                    ResearchNodeId = localNodeId,
                    Title = GetString(item, "title"),
                    Description = GetString(item, "description"),
                    StartDate = GetDateTime(item, "startDate"),
                    EndDate = TryGetDateTime(item, "endDate"),
                    Status = GetString(item, "status"),
                    CreatedAt = GetDateTime(item, "createdAt"),
                    UpdatedAt = updatedAt
                });
                count++;
            }
            else if (updatedAt > existing.UpdatedAt)
            {
                existing.ResearchNodeId = localNodeId;
                existing.Title = GetString(item, "title");
                existing.Description = GetString(item, "description");
                existing.StartDate = GetDateTime(item, "startDate");
                existing.EndDate = TryGetDateTime(item, "endDate");
                existing.Status = GetString(item, "status");
                existing.UpdatedAt = updatedAt;
            }

            // Applications
            if (item.TryGetProperty("applications", out var appsArray))
            {
                foreach (var app in appsArray.EnumerateArray())
                {
                    var appId = GetGuid(app, "applicationId");
                    var appExisting = await _context.Applications.FindAsync(appId);
                    var appUpdatedAt = GetDateTime(app, "updatedAt");

                    if (appExisting == null)
                    {
                        _context.Applications.Add(new ApplicationEntity
                        {
                            ApplicationId = appId,
                            ResearchId = id,
                            AppName = GetString(app, "appName"),
                            Url = GetString(app, "url"),
                            Description = GetString(app, "description"),
                            AdditionalInfo = GetString(app, "additionalInfo"),
                            CreatedAt = GetDateTime(app, "createdAt"),
                            UpdatedAt = appUpdatedAt
                        });
                    }
                    else if (appUpdatedAt > appExisting.UpdatedAt)
                    {
                        appExisting.AppName = GetString(app, "appName");
                        appExisting.Url = GetString(app, "url");
                        appExisting.Description = GetString(app, "description");
                        appExisting.AdditionalInfo = GetString(app, "additionalInfo");
                        appExisting.UpdatedAt = appUpdatedAt;
                    }
                }
            }

            // ResearchDevices (composite PK: ResearchId + DeviceId)
            if (item.TryGetProperty("researchDevices", out var devicesArray))
            {
                foreach (var dev in devicesArray.EnumerateArray())
                {
                    var deviceId = GetGuid(dev, "deviceId");
                    var devExisting = await _context.ResearchDevices
                        .FindAsync(id, deviceId);

                    if (devExisting == null)
                    {
                        _context.ResearchDevices.Add(new ResearchDevice
                        {
                            ResearchId = id,
                            DeviceId = deviceId,
                            Role = GetString(dev, "role"),
                            AddedAt = GetDateTime(dev, "addedAt"),
                            RemovedAt = TryGetDateTime(dev, "removedAt"),
                            CalibrationStatus = GetString(dev, "calibrationStatus"),
                            LastCalibrationDate = TryGetDateTime(dev, "lastCalibrationDate")
                        });
                    }
                    else
                    {
                        devExisting.Role = GetString(dev, "role");
                        devExisting.RemovedAt = TryGetDateTime(dev, "removedAt");
                        devExisting.CalibrationStatus = GetString(dev, "calibrationStatus");
                        devExisting.LastCalibrationDate = TryGetDateTime(dev, "lastCalibrationDate");
                    }
                }
            }

            // ResearchVolunteers (composite PK: ResearchId + VolunteerId)
            if (item.TryGetProperty("researchVolunteers", out var rvArray))
            {
                foreach (var rv in rvArray.EnumerateArray())
                {
                    var volunteerId = GetGuid(rv, "volunteerId");
                    var rvExisting = await _context.ResearchVolunteers
                        .FindAsync(id, volunteerId);

                    if (rvExisting == null)
                    {
                        _context.ResearchVolunteers.Add(new ResearchVolunteer
                        {
                            ResearchId = id,
                            VolunteerId = volunteerId,
                            EnrollmentStatus = GetString(rv, "enrollmentStatus"),
                            ConsentDate = GetDateTime(rv, "consentDate"),
                            ConsentVersion = GetString(rv, "consentVersion"),
                            ExclusionReason = TryGetString(rv, "exclusionReason"),
                            EnrolledAt = GetDateTime(rv, "enrolledAt"),
                            WithdrawnAt = TryGetDateTime(rv, "withdrawnAt")
                        });
                    }
                    else
                    {
                        rvExisting.EnrollmentStatus = GetString(rv, "enrollmentStatus");
                        rvExisting.ExclusionReason = TryGetString(rv, "exclusionReason");
                        rvExisting.WithdrawnAt = TryGetDateTime(rv, "withdrawnAt");
                    }
                }
            }

            // ResearchResearchers (composite PK: ResearchId + ResearcherId)
            if (item.TryGetProperty("researchResearchers", out var rrArray))
            {
                foreach (var rr in rrArray.EnumerateArray())
                {
                    var researcherId = GetGuid(rr, "researcherId");
                    var rrExisting = await _context.ResearchResearchers
                        .FindAsync(id, researcherId);

                    if (rrExisting == null)
                    {
                        _context.ResearchResearchers.Add(new ResearchResearcher
                        {
                            ResearchId = id,
                            ResearcherId = researcherId,
                            IsPrincipal = GetBool(rr, "isPrincipal"),
                            AssignedAt = GetDateTime(rr, "assignedAt"),
                            RemovedAt = TryGetDateTime(rr, "removedAt")
                        });
                    }
                    else
                    {
                        rrExisting.IsPrincipal = GetBool(rr, "isPrincipal");
                        rrExisting.RemovedAt = TryGetDateTime(rr, "removedAt");
                    }
                }
            }
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Session import (with nested Records → RecordChannels → TargetAreas + Annotations)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> ImportSessionsAsync(List<JsonElement> items)
    {
        int count = 0;
        foreach (var item in items)
        {
            var id = GetGuid(item, "id");
            var existing = await _context.RecordSessions
                .Include(s => s.Records).ThenInclude(r => r.RecordChannels).ThenInclude(rc => rc.TargetAreas)
                .Include(s => s.SessionAnnotations)
                .FirstOrDefaultAsync(s => s.Id == id);

            var updatedAt = GetDateTime(item, "updatedAt");

            if (existing == null)
            {
                _context.RecordSessions.Add(new RecordSession
                {
                    Id = id,
                    ResearchId = TryGetGuid(item, "researchId"),
                    VolunteerId = GetGuid(item, "volunteerId"),
                    ClinicalContext = GetString(item, "clinicalContext"),
                    StartAt = GetDateTime(item, "startAt"),
                    FinishedAt = TryGetDateTime(item, "finishedAt"),
                    CreatedAt = GetDateTime(item, "createdAt"),
                    UpdatedAt = updatedAt
                });
                count++;
            }
            else if (updatedAt > existing.UpdatedAt)
            {
                existing.ResearchId = TryGetGuid(item, "researchId");
                existing.ClinicalContext = GetString(item, "clinicalContext");
                existing.FinishedAt = TryGetDateTime(item, "finishedAt");
                existing.UpdatedAt = updatedAt;
            }

            // Records → RecordChannels → TargetAreas
            if (item.TryGetProperty("records", out var recordsArray))
            {
                foreach (var rec in recordsArray.EnumerateArray())
                {
                    var recId = GetGuid(rec, "id");
                    var recExisting = existing?.Records.FirstOrDefault(r => r.Id == recId)
                        ?? await _context.Records.FindAsync(recId);
                    var recUpdatedAt = GetDateTime(rec, "updatedAt");

                    if (recExisting == null)
                    {
                        _context.Records.Add(new Domain.Entities.Record.Record
                        {
                            Id = recId,
                            RecordSessionId = id,
                            CollectionDate = GetDateTime(rec, "collectionDate"),
                            SessionId = GetString(rec, "sessionId"),
                            RecordType = GetString(rec, "recordType"),
                            Notes = GetString(rec, "notes"),
                            CreatedAt = GetDateTime(rec, "createdAt"),
                            UpdatedAt = recUpdatedAt
                        });
                    }
                    else if (recUpdatedAt > recExisting.UpdatedAt)
                    {
                        recExisting.CollectionDate = GetDateTime(rec, "collectionDate");
                        recExisting.RecordType = GetString(rec, "recordType");
                        recExisting.Notes = GetString(rec, "notes");
                        recExisting.UpdatedAt = recUpdatedAt;
                    }

                    // RecordChannels
                    if (rec.TryGetProperty("recordChannels", out var channelsArray))
                    {
                        foreach (var ch in channelsArray.EnumerateArray())
                        {
                            var chId = GetGuid(ch, "id");
                            var chExisting = await _context.RecordChannels.FindAsync(chId);
                            var chUpdatedAt = GetDateTime(ch, "updatedAt");

                            if (chExisting == null)
                            {
                                _context.RecordChannels.Add(new RecordChannel
                                {
                                    Id = chId,
                                    RecordId = recId,
                                    SensorId = TryGetGuid(ch, "sensorId"),
                                    SignalType = GetString(ch, "signalType"),
                                    FileUrl = GetString(ch, "fileUrl"),
                                    SamplingRate = GetFloat(ch, "samplingRate"),
                                    SamplesCount = GetInt(ch, "samplesCount"),
                                    StartTimestamp = GetDateTime(ch, "startTimestamp"),
                                    CreatedAt = GetDateTime(ch, "createdAt"),
                                    UpdatedAt = chUpdatedAt
                                });
                            }
                            else if (chUpdatedAt > chExisting.UpdatedAt)
                            {
                                chExisting.SignalType = GetString(ch, "signalType");
                                chExisting.FileUrl = GetString(ch, "fileUrl");
                                chExisting.SamplingRate = GetFloat(ch, "samplingRate");
                                chExisting.SamplesCount = GetInt(ch, "samplesCount");
                                chExisting.UpdatedAt = chUpdatedAt;
                            }

                            // TargetAreas
                            if (ch.TryGetProperty("targetAreas", out var areasArray))
                            {
                                foreach (var ta in areasArray.EnumerateArray())
                                {
                                    var taId = GetGuid(ta, "id");
                                    var taExisting = await _context.TargetAreas.FindAsync(taId);
                                    var taUpdatedAt = GetDateTime(ta, "updatedAt");

                                    if (taExisting == null)
                                    {
                                        _context.TargetAreas.Add(new TargetArea
                                        {
                                            Id = taId,
                                            RecordChannelId = chId,
                                            BodyStructureCode = GetString(ta, "bodyStructureCode"),
                                            LateralityCode = TryGetString(ta, "lateralityCode"),
                                            TopographicalModifierCode = TryGetString(ta, "topographicalModifierCode"),
                                            Notes = GetString(ta, "notes"),
                                            CreatedAt = GetDateTime(ta, "createdAt"),
                                            UpdatedAt = taUpdatedAt
                                        });
                                    }
                                    else if (taUpdatedAt > taExisting.UpdatedAt)
                                    {
                                        taExisting.BodyStructureCode = GetString(ta, "bodyStructureCode");
                                        taExisting.LateralityCode = TryGetString(ta, "lateralityCode");
                                        taExisting.TopographicalModifierCode = TryGetString(ta, "topographicalModifierCode");
                                        taExisting.Notes = GetString(ta, "notes");
                                        taExisting.UpdatedAt = taUpdatedAt;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // SessionAnnotations
            if (item.TryGetProperty("sessionAnnotations", out var annotationsArray))
            {
                foreach (var ann in annotationsArray.EnumerateArray())
                {
                    var annId = GetGuid(ann, "id");
                    var annExisting = existing?.SessionAnnotations.FirstOrDefault(a => a.Id == annId)
                        ?? await _context.SessionAnnotations.FindAsync(annId);
                    var annUpdatedAt = GetDateTime(ann, "updatedAt");

                    if (annExisting == null)
                    {
                        _context.SessionAnnotations.Add(new SessionAnnotation
                        {
                            Id = annId,
                            RecordSessionId = id,
                            Text = GetString(ann, "text"),
                            CreatedAt = GetDateTime(ann, "createdAt"),
                            UpdatedAt = annUpdatedAt
                        });
                    }
                    else if (annUpdatedAt > annExisting.UpdatedAt)
                    {
                        annExisting.Text = GetString(ann, "text");
                        annExisting.UpdatedAt = annUpdatedAt;
                    }
                }
            }
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recording file import — uploads to blob storage outside DB transaction.
    // Updates RecordChannel.FileUrl after successful upload.
    // Idempotent: if blob already exists, overwrites it (same content expected).
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<int> ImportRecordingsAsync(List<RecordingFileEntry> recordings)
    {
        if (recordings.Count == 0) return 0;

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "recordings";

        int count = 0;
        foreach (var entry in recordings)
        {
            try
            {
                var channel = await _context.RecordChannels.FindAsync(entry.Id);
                if (channel == null)
                {
                    _logger.LogWarning("RecordChannel {Id} not found; skipping recording upload", entry.Id);
                    continue;
                }

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobName = string.IsNullOrEmpty(entry.FileName)
                    ? $"{entry.Id}.bin"
                    : entry.FileName;
                var blobClient = containerClient.GetBlobClient(blobName);

                var fileBytes = Convert.FromBase64String(entry.ContentBase64);
                using var ms = new MemoryStream(fileBytes);
                await blobClient.UploadAsync(ms, overwrite: true);

                channel.FileUrl = blobClient.Uri.ToString();
                await _context.SaveChangesAsync();
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload recording file for RecordChannel {Id}", entry.Id);
                // Non-fatal: continue with remaining recordings
            }
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JsonElement accessor helpers — keep SNOMED dispatch code concise
    // ─────────────────────────────────────────────────────────────────────────

    private static string GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty
            : string.Empty;

    private static string? TryGetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static bool GetBool(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;

    private static DateTime GetDateTime(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return DateTime.Parse(v.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return DateTime.UtcNow;
    }

    private static DateTime? TryGetDateTime(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.Null) return null;
            if (v.ValueKind == JsonValueKind.String)
                return DateTime.Parse(v.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        return null;
    }

    private static Guid GetGuid(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) ? v.GetGuid() : Guid.Empty;

    private static Guid? TryGetGuid(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.Null) return null;
            if (v.TryGetGuid(out var g)) return g;
        }
        return null;
    }

    private static float? TryGetFloat(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null)
        {
            if (v.TryGetSingle(out var f)) return f;
        }
        return null;
    }

    private static float GetFloat(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.TryGetSingle(out var f)) return f;
        return 0f;
    }

    private static int GetInt(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.TryGetInt32(out var i)) return i;
        return 0;
    }

    private static int? TryGetInt(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null
            && v.TryGetInt32(out var i)) return i;
        return null;
    }
}
