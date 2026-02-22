using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Snomed;
using VolunteerEntity = Bioteca.Prism.Domain.Entities.Volunteer.Volunteer;

namespace Bioteca.Prism.Service.Interfaces.Sync;

/// <summary>
/// Service for exporting entities from this node to a requesting node (sync pull model).
/// All methods support incremental sync via the <paramref name="since"/> watermark parameter.
/// </summary>
public interface ISyncExportService
{
    /// <summary>
    /// Generate a manifest summarizing available entities since the given timestamp.
    /// </summary>
    Task<SyncManifestResponse> GetManifestAsync(DateTime? since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export SNOMED catalog entities by entity type with pagination.
    /// Supported entity types: body-regions, body-structures, topographical-modifiers,
    /// lateralities, clinical-conditions, clinical-events, medications, allergy-intolerances, severity-codes
    /// </summary>
    Task<PagedSyncResult<object>> GetSnomedEntitiesAsync(
        string entityType, DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export volunteers with nested clinical sub-entities (VitalSigns, ClinicalConditions, Medications, Allergies).
    /// </summary>
    Task<PagedSyncResult<VolunteerEntity>> GetVolunteersAsync(
        DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export researchers with pagination and since-filtering.
    /// </summary>
    Task<PagedSyncResult<Domain.Entities.Researcher.Researcher>> GetResearchersAsync(
        DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export devices with pagination and since-filtering.
    /// </summary>
    Task<PagedSyncResult<Domain.Entities.Device.Device>> GetDevicesAsync(
        DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export research projects with nested sub-entities (Applications, ResearchDevices, ResearchResearchers).
    /// </summary>
    Task<PagedSyncResult<Domain.Entities.Research.Research>> GetResearchAsync(
        DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export recording sessions with nested records, record channels, annotations, and target areas.
    /// </summary>
    Task<PagedSyncResult<Domain.Entities.Record.RecordSession>> GetSessionsAsync(
        DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recording file bytes by RecordChannel ID.
    /// Returns null if the file does not exist.
    /// </summary>
    Task<(byte[] data, string contentType, string fileName)?> GetRecordingFileAsync(
        Guid recordChannelId,
        CancellationToken cancellationToken = default);
}
