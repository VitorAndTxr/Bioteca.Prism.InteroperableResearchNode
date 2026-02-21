using System.Text.Json;

namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Full sync payload submitted by the middleware to POST /api/sync/import.
/// Contains all entities collected from the remote node, ready for transactional upsert.
/// SNOMED types are split into named buckets to avoid type-inference heuristics during import.
/// </summary>
public class SyncImportPayload
{
    /// <summary>
    /// Timestamp when the manifest was generated on the remote node.
    /// Used as LastSyncedAt watermark after successful import.
    /// </summary>
    public DateTime ManifestGeneratedAt { get; set; }

    /// <summary>
    /// Remote node ID (used for SyncLog FK and ResearchNodeId preservation override)
    /// </summary>
    public Guid RemoteNodeId { get; set; }

    /// <summary>
    /// Typed SNOMED entity buckets — each list maps to a specific entity type.
    /// </summary>
    public SyncSnomedPayload Snomed { get; set; } = new();

    /// <summary>
    /// Volunteer entities with nested clinical sub-entities
    /// </summary>
    public List<JsonElement> Volunteers { get; set; } = new();

    /// <summary>
    /// Research entities with nested sub-entities
    /// </summary>
    public List<JsonElement> Research { get; set; } = new();

    /// <summary>
    /// Session entities with nested records, channels, annotations
    /// </summary>
    public List<JsonElement> Sessions { get; set; } = new();

    /// <summary>
    /// Recording file references with binary content encoded as Base64
    /// </summary>
    public List<RecordingFileEntry> Recordings { get; set; } = new();
}

/// <summary>
/// Typed buckets for each SNOMED entity type, matching the export endpoint types.
/// </summary>
public class SyncSnomedPayload
{
    public List<JsonElement> BodyRegions { get; set; } = new();
    public List<JsonElement> BodyStructures { get; set; } = new();
    public List<JsonElement> Lateralities { get; set; } = new();
    public List<JsonElement> TopographicalModifiers { get; set; } = new();
    public List<JsonElement> SeverityCodes { get; set; } = new();
    public List<JsonElement> ClinicalConditions { get; set; } = new();
    public List<JsonElement> ClinicalEvents { get; set; } = new();
    public List<JsonElement> Medications { get; set; } = new();
    public List<JsonElement> AllergyIntolerances { get; set; } = new();
}

public class RecordingFileEntry
{
    /// <summary>
    /// RecordChannel ID (maps to record_channel.id)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Base64-encoded binary file content
    /// </summary>
    public string ContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Content type (e.g. application/octet-stream)
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Original file name for storage
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
