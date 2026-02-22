using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// User-authenticated pull/preview endpoints for triggering backend-to-backend sync.
///
/// These endpoints use standard JWT [Authorize] (user authentication), NOT the
/// [PrismAuthenticatedSession] node-session attribute used by SyncController.
/// Having two controllers share the "api/sync" route prefix is fully supported by
/// ASP.NET Core — attribute routing resolves actions by method + path independently.
///
/// The backend performs the full 4-phase node handshake internally via INodeChannelClient.
/// Sensitive entity data never transits through the UI layer.
/// </summary>
[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncPullController : ControllerBase
{
    private readonly ISyncPullService _syncPullService;
    private readonly ILogger<SyncPullController> _logger;

    public SyncPullController(ISyncPullService syncPullService, ILogger<SyncPullController> logger)
    {
        _syncPullService = syncPullService;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the sync manifest from a remote node without importing any data.
    /// Useful for showing the user how many entities will be synced before confirming.
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] SyncPullRequest request)
    {
        if (request.RemoteNodeId == Guid.Empty)
        {
            return BadRequest(new { error = "ERR_INVALID_REQUEST", message = "remoteNodeId is required" });
        }

        try
        {
            var result = await _syncPullService.PreviewAsync(request.RemoteNodeId, request.Since);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_NODE_NOT_FOUND"))
        {
            _logger.LogWarning("Preview failed — node not found or not authorized: {Message}", ex.Message);
            return NotFound(new { error = "ERR_NODE_NOT_FOUND", message = ex.Message, stage = "resolve_node" });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_HANDSHAKE_FAILED"))
        {
            _logger.LogWarning("Preview failed — channel open failed: {Message}", ex.Message);
            return StatusCode(502, new { error = "ERR_HANDSHAKE_FAILED", message = ex.Message, stage = "phase1_channel" });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_AUTHENTICATION_FAILED"))
        {
            _logger.LogWarning("Preview failed — authentication rejected: {Message}", ex.Message);
            return StatusCode(502, new { error = "ERR_AUTHENTICATION_FAILED", message = ex.Message, stage = "phase3_authenticate" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview failed for remote node {RemoteNodeId}", request.RemoteNodeId);
            return StatusCode(502, new { error = "ERR_MANIFEST_FAILED", message = ex.Message, stage = "manifest" });
        }
    }

    /// <summary>
    /// Triggers a full backend-to-backend sync pull from a remote node.
    /// Performs the 4-phase handshake, fetches all entity pages, and imports
    /// them transactionally. Returns a summary of synced entities.
    /// </summary>
    [HttpPost("pull")]
    public async Task<IActionResult> Pull([FromBody] SyncPullRequest request)
    {
        if (request.RemoteNodeId == Guid.Empty)
        {
            return BadRequest(new { error = "ERR_INVALID_REQUEST", message = "remoteNodeId is required" });
        }

        try
        {
            var result = await _syncPullService.PullAsync(request.RemoteNodeId, request.Since);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_NODE_NOT_FOUND"))
        {
            _logger.LogWarning("Pull failed — node not found or not authorized: {Message}", ex.Message);
            return NotFound(new { error = "ERR_NODE_NOT_FOUND", message = ex.Message, stage = "resolve_node" });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_HANDSHAKE_FAILED"))
        {
            _logger.LogWarning("Pull failed — channel open failed: {Message}", ex.Message);
            return StatusCode(502, new { error = "ERR_HANDSHAKE_FAILED", message = ex.Message, stage = "phase1_channel" });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ERR_AUTHENTICATION_FAILED"))
        {
            _logger.LogWarning("Pull failed — authentication rejected: {Message}", ex.Message);
            return StatusCode(502, new { error = "ERR_AUTHENTICATION_FAILED", message = ex.Message, stage = "phase3_authenticate" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed for remote node {RemoteNodeId}", request.RemoteNodeId);
            return StatusCode(500, new { error = "ERR_IMPORT_FAILED", message = ex.Message, stage = "import" });
        }
    }
}
