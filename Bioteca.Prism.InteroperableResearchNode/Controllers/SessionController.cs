using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Session;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// Phase 4: Session management endpoints
/// All endpoints require:
/// 1. Encrypted channel (X-Channel-Id + encrypted payload)
/// 2. Authenticated session (Bearer token in encrypted payload)
/// </summary>
[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IChannelEncryptionService _encryptionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISessionService sessionService,
        IChannelEncryptionService encryptionService,
        ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get current session information (whoami endpoint)
    /// Request must be encrypted via channel
    /// </summary>
    /// <returns>Encrypted session details</returns>
    [HttpPost("whoami")]
    [PrismEncryptedChannelConnection<WhoAmIRequest>]
    [PrismAuthenticatedSession]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status401Unauthorized)]
    public IActionResult WhoAmI()
    {
        var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
        var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
        var request = HttpContext.Items["DecryptedRequest"] as WhoAmIRequest;

        if (sessionContext == null)
        {
            return Unauthorized(new { error = "ERR_NO_SESSION_CONTEXT" });
        }

        _logger.LogInformation(
            "WhoAmI request from node {NodeId}, session {SessionToken}",
            sessionContext.NodeId,
            sessionContext.SessionToken);

        var response = new
        {
            sessionToken = sessionContext.SessionToken,
            nodeId = sessionContext.NodeId,
            channelId = sessionContext.ChannelId,
            expiresAt = sessionContext.ExpiresAt,
            remainingSeconds = sessionContext.GetRemainingSeconds(),
            capabilities = sessionContext.NodeAccessLevel,
            requestCount = sessionContext.RequestCount,
            timestamp = DateTime.UtcNow
        };

        // Encrypt response
        var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext!.SymmetricKey);

        return Ok(encryptedResponse);
    }

    /// <summary>
    /// Renew current session (extend TTL)
    /// Request must be encrypted via channel
    /// </summary>
    /// <returns>Encrypted updated session info</returns>
    [HttpPost("renew")]
    [PrismEncryptedChannelConnection<RenewSessionRequest>]
    [PrismAuthenticatedSession]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RenewSession()
    {
        var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
        var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
        var request = HttpContext.Items["DecryptedRequest"] as RenewSessionRequest;

        if (sessionContext == null)
        {
            return Unauthorized(new { error = "ERR_NO_SESSION_CONTEXT" });
        }

        var additionalSeconds = request?.AdditionalSeconds ?? 3600; // Default: 1 hour

        _logger.LogInformation(
            "Renewing session {SessionToken} for node {NodeId}, adding {Seconds}s",
            sessionContext.SessionToken,
            sessionContext.NodeId,
            additionalSeconds);

        var renewedSession = await _sessionService.RenewSessionAsync(
            sessionContext.SessionToken,
            additionalSeconds);

        if (renewedSession == null)
        {
            return Unauthorized(new
            {
                error = "ERR_SESSION_RENEWAL_FAILED",
                message = "Session could not be renewed"
            });
        }

        var response = new
        {
            sessionToken = renewedSession.SessionToken,
            nodeId = renewedSession.NodeId,
            expiresAt = renewedSession.ExpiresAt,
            remainingSeconds = renewedSession.GetRemainingSeconds(),
            message = $"Session renewed for {additionalSeconds} seconds",
            timestamp = DateTime.UtcNow
        };

        // Encrypt response
        var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext!.SymmetricKey);

        return Ok(encryptedResponse);
    }

    /// <summary>
    /// Revoke current session (logout)
    /// Request must be encrypted via channel
    /// </summary>
    /// <returns>Encrypted revocation confirmation</returns>
    [HttpPost("revoke")]
    [PrismEncryptedChannelConnection<RevokeSessionRequest>]
    [PrismAuthenticatedSession]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeSession()
    {
        var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
        var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
        var request = HttpContext.Items["DecryptedRequest"] as RevokeSessionRequest;

        if (sessionContext == null)
        {
            return Unauthorized(new { error = "ERR_NO_SESSION_CONTEXT" });
        }

        _logger.LogInformation(
            "Revoking session {SessionToken} for node {NodeId}",
            sessionContext.SessionToken,
            sessionContext.NodeId);

        var revoked = await _sessionService.RevokeSessionAsync(sessionContext.SessionToken);

        if (!revoked)
        {
            return BadRequest(new
            {
                error = "ERR_SESSION_REVOCATION_FAILED",
                message = "Session could not be revoked"
            });
        }

        var response = new
        {
            sessionToken = sessionContext.SessionToken,
            nodeId = sessionContext.NodeId,
            revoked = true,
            message = "Session revoked successfully",
            timestamp = DateTime.UtcNow
        };

        // Encrypt response
        var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext!.SymmetricKey);

        return Ok(encryptedResponse);
    }

    /// <summary>
    /// Get session metrics for current node
    /// Requires admin:node capability
    /// Request must be encrypted via channel
    /// </summary>
    /// <returns>Encrypted session metrics</returns>
    [HttpPost("metrics")]
    [PrismEncryptedChannelConnection<GetMetricsRequest>]
    [PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.Admin)]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetrics()
    {
        var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
        var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
        var request = HttpContext.Items["DecryptedRequest"] as GetMetricsRequest;

        if (sessionContext == null)
        {
            return Unauthorized(new { error = "ERR_NO_SESSION_CONTEXT" });
        }

        // Use requested NodeId (parse from string) or default to current session's node
        Guid targetNodeId;
        if (request?.NodeId != null && Guid.TryParse(request.NodeId, out var parsedGuid))
        {
            targetNodeId = parsedGuid;
        }
        else
        {
            targetNodeId = sessionContext.NodeId;
        }

        _logger.LogInformation(
            "Getting metrics for node {NodeId} (requested by {SessionNodeId})",
            targetNodeId,
            sessionContext.NodeId);

        var metrics = await _sessionService.GetSessionMetricsAsync(targetNodeId);

        // Encrypt response
        var encryptedResponse = _encryptionService.EncryptPayload(metrics, channelContext!.SymmetricKey);

        return Ok(encryptedResponse);
    }
}
