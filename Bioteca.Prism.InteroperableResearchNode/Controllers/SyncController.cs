using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// Sync export and import endpoints for node-to-node data synchronization (Phase 17 pull model).
///
/// Export endpoints (GET + POST /manifest, POST /import, GET /log) are secured with:
///   - [PrismEncryptedChannelConnection]: requires an active encrypted channel (on export endpoints)
///   - [PrismAuthenticatedSession]: validates session token and enforces ReadWrite capability
///   - [PrismSyncEndpoint]: elevates rate limit to 600 req/min for this controller
///
/// Note: POST /import and GET /log use [PrismEncryptedChannelConnection] individually since they
/// may also be called via the channel infrastructure. The class-level attribute applies to all actions.
///
/// Note: This controller does NOT extend BaseController because the sync endpoints require
/// a custom ?since= query parameter that is incompatible with BaseController.HandleQueryParameters().
/// </summary>
[ApiController]
[Route("api/sync")]
[PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadWrite)]
[PrismSyncEndpoint]
public class SyncController : ControllerBase
{
    private readonly ISyncExportService _syncExportService;
    private readonly ISyncImportService _syncImportService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ISyncExportService syncExportService,
        ISyncImportService syncImportService,
        ILogger<SyncController> logger)
    {
        _syncExportService = syncExportService;
        _syncImportService = syncImportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a manifest of available entities with optional since-filtering.
    /// Used by the requesting node to estimate sync scope before pulling data.
    /// </summary>
    [HttpPost("manifest")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetManifest([FromBody] SyncManifestRequest request)
    {
        try
        {
            var manifest = await _syncExportService.GetManifestAsync(request?.Since);
            return Ok(manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sync manifest");
            return StatusCode(500, new { error = "ERR_MANIFEST_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export SNOMED catalog entities by type with pagination and since-filtering.
    /// Supported entityType values: body-regions, body-structures, topographical-modifiers,
    /// lateralities, clinical-conditions, clinical-events, medications, allergy-intolerances, severity-codes
    /// </summary>
    [HttpGet("snomed/{entityType}")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetSnomedEntities(
        string entityType,
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetSnomedEntitiesAsync(entityType, since, page, pageSize);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "ERR_UNKNOWN_ENTITY_TYPE", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting SNOMED entities of type {EntityType}", entityType);
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export volunteers with nested clinical sub-entities, with pagination and since-filtering.
    /// </summary>
    [HttpGet("volunteers")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetVolunteers(
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetVolunteersAsync(since, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting volunteers");
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export researchers with pagination and since-filtering.
    /// </summary>
    [HttpGet("researchers")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetResearchers(
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetResearchersAsync(since, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting researchers");
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export devices with pagination and since-filtering.
    /// </summary>
    [HttpGet("devices")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetDevices(
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetDevicesAsync(since, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting devices");
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export research projects with nested sub-entities, with pagination and since-filtering.
    /// </summary>
    [HttpGet("research")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetResearch(
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetResearchAsync(since, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting research");
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export recording sessions with nested records, channels, and annotations, with pagination and since-filtering.
    /// </summary>
    [HttpGet("sessions")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetSessions(
        [FromQuery] DateTime? since,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var result = await _syncExportService.GetSessionsAsync(since, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sessions");
            return StatusCode(500, new { error = "ERR_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Export a recording file by RecordChannel ID as an encrypted JSON response.
    /// Returns { contentBase64, contentType, fileName } so the middleware's invoke()
    /// can deserialize it as a standard JSON envelope — consistent with all other sync endpoints.
    /// The ID maps to record_channel.id — RecordChannel.FileUrl references the blob storage location.
    /// </summary>
    [HttpGet("recordings/{id:guid}/file")]
    [PrismEncryptedChannelConnection]
    public async Task<IActionResult> GetRecordingFile(Guid id)
    {
        try
        {
            var result = await _syncExportService.GetRecordingFileAsync(id);
            if (result == null)
            {
                return NotFound(new { error = "ERR_RECORDING_NOT_FOUND", message = $"Recording file for channel {id} not found" });
            }

            var (data, contentType, fileName) = result.Value;
            return Ok(new
            {
                ContentBase64 = Convert.ToBase64String(data),
                ContentType = contentType,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting recording file for channel {RecordChannelId}", id);
            return StatusCode(500, new { error = "ERR_FILE_EXPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Import all collected sync data in a single transactional operation.
    /// Called by the middleware against the requesting node's own local backend.
    /// Does not require an encrypted channel (local call).
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] SyncImportPayload payload)
    {
        if (payload == null)
        {
            return BadRequest(new { error = "ERR_INVALID_PAYLOAD", message = "Sync import payload is required" });
        }

        try
        {
            var result = await _syncImportService.ImportAsync(payload, payload.RemoteNodeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync import from remote node {RemoteNodeId}", payload.RemoteNodeId);
            return StatusCode(500, new { error = "ERR_IMPORT_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated sync log entries for a remote node, ordered by StartedAt descending.
    /// </summary>
    [HttpGet("log")]
    public async Task<IActionResult> GetSyncLog(
        [FromQuery] Guid? remoteNodeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!remoteNodeId.HasValue || remoteNodeId == Guid.Empty)
        {
            return BadRequest(new { error = "ERR_INVALID_REMOTE_NODE_ID", message = "remoteNodeId must be a valid UUID" });
        }

        try
        {
            var syncLogRepo = HttpContext.RequestServices
                .GetRequiredService<Bioteca.Prism.Data.Interfaces.Sync.ISyncLogRepository>();

            var (items, total) = await syncLogRepo.GetByRemoteNodeIdAsync(remoteNodeId.Value, page, pageSize);

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return Ok(new
            {
                data = items.Select(s => new
                {
                    id = s.Id,
                    remoteNodeId = s.RemoteNodeId,
                    startedAt = s.StartedAt,
                    completedAt = s.CompletedAt,
                    status = s.Status,
                    lastSyncedAt = s.LastSyncedAt,
                    entitiesReceived = s.EntitiesReceived,
                    errorMessage = s.ErrorMessage
                }),
                page,
                pageSize,
                totalRecords = total,
                totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync log for remote node {RemoteNodeId}", remoteNodeId);
            return StatusCode(500, new { error = "ERR_SYNC_LOG_FAILED", message = ex.Message });
        }
    }
}
