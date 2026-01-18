using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.ResearchNode;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [ApiController]
    [Route("api/node")]
    public class NodeConnectionController : BaseController
    {
        private readonly ILogger<NodeConnectionController> _logger;
        private readonly IChannelEncryptionService _encryptionService;
        private readonly IResearchNodeService _nodeRegistry;
        private readonly IChannelStore _channelStore;
        private readonly IChallengeService _challengeService;

        public NodeConnectionController(
            ILogger<NodeConnectionController> logger,
            IEphemeralKeyService ephemeralKeyService,
            IChannelEncryptionService encryptionService,
            IConfiguration configuration,
            INodeChannelClient channelClient,
            IResearchNodeService nodeRegistry,
            IChannelStore channelStore,
            IChallengeService challengeService,
            IApiContext apiContext
            ) : base(logger, configuration, apiContext)
        {
            _logger = logger;
            _encryptionService = encryptionService;
            _nodeRegistry = nodeRegistry;
            _channelStore = channelStore;
            _challengeService = challengeService;
        }
        /// <summary>
        /// Phase 2: Identify node after encrypted channel is established
        /// </summary>
        /// <param name="encryptedRequest">Encrypted node identification request</param>
        /// <returns>Encrypted node status response (known/unknown)</returns>
        [HttpPost("identify")]
        [PrismEncryptedChannelConnection<NodeIdentifyRequest>]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IdentifyNode()
        {
            try
            {
                var request = HttpContext.Items["DecryptedRequest"] as NodeIdentifyRequest;

                var registeredNode = await _nodeRegistry.GetNodeByRequestAsync(request);

                if (registeredNode == null)
                {
                    // Unknown node - return registration information
                    _logger.LogWarning("Unknown node attempted to connect: {NodeId}", request.NodeId);

                    var unknownResponse = new NodeStatusResponse
                    {
                        IsKnown = false,
                        Status = AuthorizationStatus.Unknown,
                        NodeId = request.NodeId,
                        Timestamp = DateTime.UtcNow,
                        RegistrationUrl = $"{Request.Scheme}://{Request.Host}/api/node/register",
                        Message = "Node is not registered. Please register using the provided URL.",
                        NextPhase = null
                    };
                    return Ok(unknownResponse);
                }

                var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

                // Known node - check authorization status
                _logger.LogInformation("Known node identified: {NodeId} (DB ID: {DbId}) with status {Status}",
                    request.NodeId, registeredNode.Id, registeredNode.Status);

                // Store identified node info in channel context for subsequent operations
                channelContext.IdentifiedNodeId = registeredNode.Id;
                channelContext.CertificateFingerprint = registeredNode.CertificateFingerprint;
                // Update the channel context by removing and re-adding
                await _channelStore.RemoveChannelAsync(channelContext.ChannelId);
                await _channelStore.AddChannelAsync(channelContext.ChannelId!, channelContext);

                var response = new NodeStatusResponse
                {
                    IsKnown = true,
                    Status = registeredNode.Status,
                    NodeId = request.NodeId, // Return the protocol NodeId from request
                    RegistrationId = registeredNode.Id, // Return internal Guid for reference
                    NodeName = registeredNode.NodeName,
                    Timestamp = DateTime.UtcNow
                };

                switch (registeredNode.Status)
                {
                    case AuthorizationStatus.Authorized:
                        response.Message = "Node is authorized. Proceed to Phase 3 (Mutual Authentication).";
                        response.NextPhase = "phase3_authenticate";
                        break;

                    case AuthorizationStatus.Pending:
                        response.Message = "Node registration is pending approval.";
                        response.NextPhase = null;
                        break;

                    case AuthorizationStatus.Revoked:
                        response.Message = "Node authorization has been revoked.";
                        response.NextPhase = null;
                        break;
                }

                return Ok(response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying node");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_IDENTIFICATION_FAILED",
                    "Failed to identify node",
                    retryable: true
                ));
            }
        }




        /// <summary>
        /// Register a new node
        /// </summary>
        /// <param name="encryptedRequest">Encrypted node registration request</param>
        /// <returns>Encrypted registration response</returns>
        [HttpPost("/api/node/register")]
        [PrismEncryptedChannelConnection<NodeRegistrationRequest>]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterNode()
        {
            try
            {

                var decryptedPayload = HttpContext.Items["DecryptedRequest"] as NodeRegistrationRequest;

                var response = await _nodeRegistry.RegisterNodeAsync(decryptedPayload);

                // Check if registration was successful
                if (!response.Success)
                {
                    return BadRequest(CreateError(
                        "ERR_REGISTRATION_FAILED",
                        response.Message ?? "Registration failed",
                        retryable: false
                    ));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering node");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_REGISTRATION_FAILED",
                    "Failed to register node",
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Get all registered nodes (for admin purposes)
        /// </summary>
        [HttpGet("/api/node/nodes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllNodes()
        {
            var nodes = await _nodeRegistry.GetAllNodesAsync();
            return Ok(nodes);
        }

        /// <summary>
        /// Update node authorization status (for admin purposes)
        /// </summary>
        [HttpPut("/api/node/{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateNodeStatus(Guid id, [FromBody] UpdateNodeStatusRequest request)
        {
            // Validate status enum
            if (!Enum.IsDefined(typeof(AuthorizationStatus), request.Status))
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_STATUS",
                    $"Invalid status value: {(int)request.Status}",
                    retryable: false
                ));
            }

            var success = await _nodeRegistry.UpdateNodeStatusAsync(id, request.Status);

            if (!success)
            {
                return NotFound(new { message = "Node not found" });
            }

            return Ok(new { message = "Node status updated successfully", id, status = request.Status });
        }

        /// <summary>
        /// Phase 3: Request challenge for mutual authentication
        /// </summary>
        [HttpPost("challenge")]
        [PrismEncryptedChannelConnection<ChallengeRequest>]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestChallenge()
        {
            try
            {
                var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
                var request = HttpContext.Items["DecryptedRequest"] as ChallengeRequest;

                // Get identified node from channel context (set during Phase 2 identification)
                if (channelContext!.IdentifiedNodeId == null)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_IDENTIFIED",
                        "Node must complete Phase 2 identification before requesting challenge",
                        retryable: false
                    ));
                }

                // Verify node is authorized
                var registeredNode = await _nodeRegistry.GetNodeAsync(channelContext.IdentifiedNodeId.Value);
                if (registeredNode == null)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_REGISTERED",
                        "Node is not registered",
                        retryable: false
                    ));
                }

                if (registeredNode.Status != AuthorizationStatus.Authorized)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_AUTHORIZED",
                        $"Node status is {registeredNode.Status}. Only authorized nodes can authenticate.",
                        retryable: false
                    ));
                }

                // Generate challenge
                var challengeResponse = await _challengeService.GenerateChallengeAsync(channelContext.ChannelId, request.NodeId);

                return Ok(challengeResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating challenge");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CHALLENGE_FAILED",
                    "Failed to generate challenge",
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Phase 3: Verify challenge response and complete authentication
        /// </summary>
        [HttpPost("authenticate")]
        [PrismEncryptedChannelConnection<ChallengeResponseRequest>]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Authenticate()
        {
            try
            {
                var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
                var request = HttpContext.Items["DecryptedRequest"] as ChallengeResponseRequest;

                // Get identified node from channel context (set during Phase 2 identification)
                if (channelContext!.IdentifiedNodeId == null)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_IDENTIFIED",
                        "Node must complete Phase 2 identification before authentication",
                        retryable: false
                    ));
                }

                // Get node information
                var registeredNode = await _nodeRegistry.GetNodeAsync(channelContext.IdentifiedNodeId.Value);
                if (registeredNode == null)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_REGISTERED",
                        "Node is not registered",
                        retryable: false
                    ));
                }

                if (registeredNode.Status != AuthorizationStatus.Authorized)
                {
                    return BadRequest(CreateError(
                        "ERR_NODE_NOT_AUTHORIZED",
                        $"Node status is {registeredNode.Status}",
                        retryable: false
                    ));
                }

                // Verify challenge response
                var isValid = await _challengeService.VerifyChallengeResponseAsync(request, registeredNode.Certificate);

                if (!isValid)
                {
                    await _challengeService.InvalidateChallengeAsync(channelContext.ChannelId, request.NodeId);

                    return BadRequest(CreateError(
                        "ERR_INVALID_CHALLENGE_RESPONSE",
                        "Challenge response verification failed",
                        retryable: false
                    ));
                }

                // Generate authentication result
                var authResponse = await _challengeService.GenerateAuthenticationResultAsync(
                    registeredNode.Id,
                    channelContext.ChannelId,
                    registeredNode.NodeAccessLevel);

                // Update last authentication timestamp
                await _nodeRegistry.UpdateLastAuthenticationAsync(registeredNode.Id);

                // Invalidate challenge (one-time use)
                await _challengeService.InvalidateChallengeAsync(channelContext.ChannelId, request.NodeId);

                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating node");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_AUTHENTICATION_FAILED",
                    "Failed to authenticate node",
                    retryable: true
                ));
            }
        }


        [Route("[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveNodeConnectionsPaginated()
        {
            try
            {
                return await ServiceInvoke(_nodeRegistry.GetAllConnectionsPaginated);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve All Connections");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_ALL_CONNECTIONS_RETRIEVAL_FAILED",
                    "Failed to retrieve All Connections",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUnaprovedPaginated()
        {
            try
            {
                return await ServiceInvoke(_nodeRegistry.GetAllUnaprovedPaginated);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Unaproved Connections");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_UNAPROVED_CONNECTIONS_RETRIEVAL_FAILED",
                    "Failed to retrieve Unaproved Connections",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddResearchNodeConnectionDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult NewConnection()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddResearchNodeConnectionDTO;
                return ServiceInvoke(_nodeRegistry.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new connection");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CONNECTION_REGISTRATION_FAILED",
                    "Failed to register new new connection:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Approve a pending connection request
        /// </summary>
        /// <param name="connectionId">The connection ID to approve</param>
        /// <returns>200 OK if approved, 404 if not found, 400 if not pending</returns>
        [HttpPost("{connectionId}/approve")]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveConnection(Guid connectionId)
        {
            try
            {
                var success = await _nodeRegistry.ApproveConnectionAsync(connectionId);

                if (!success)
                {
                    return NotFound(new { error = "Connection not found" });
                }

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve connection {ConnectionId}", connectionId);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_APPROVE_CONNECTION_FAILED",
                    "Failed to approve connection",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Reject a pending connection request
        /// </summary>
        /// <param name="connectionId">The connection ID to reject</param>
        /// <returns>200 OK if rejected, 404 if not found, 400 if not pending</returns>
        [HttpPost("{connectionId}/reject")]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectConnection(Guid connectionId)
        {
            try
            {
                var success = await _nodeRegistry.RejectConnectionAsync(connectionId);

                if (!success)
                {
                    return NotFound(new { error = "Connection not found" });
                }

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject connection {ConnectionId}", connectionId);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_REJECT_CONNECTION_FAILED",
                    "Failed to reject connection",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Get a node connection by ID
        /// </summary>
        /// <param name="id">The connection ID</param>
        /// <returns>The node connection if found, 404 if not found</returns>
        [HttpGet("{id:guid}")]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetConnectionById(Guid id)
        {
            try
            {
                var connection = await _nodeRegistry.GetConnectionByIdAsync(id);

                if (connection == null)
                {
                    return NotFound(new { error = "Connection not found" });
                }

                return await ServiceInvoke(() => Task.FromResult(connection));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve connection by ID {ConnectionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CONNECTION_RETRIEVAL_FAILED",
                    "Failed to retrieve connection",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Update an existing node connection
        /// </summary>
        /// <param name="id">The connection ID</param>
        /// <returns>The updated node connection if found, 404 if not found</returns>
        [HttpPut("Update/{id:guid}")]
        [PrismEncryptedChannelConnection<UpdateResearchNodeConnectionDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateConnection(Guid id)
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as UpdateResearchNodeConnectionDTO;

                var updatedConnection = await _nodeRegistry.UpdateConnectionAsync(id, payload!);

                if (updatedConnection == null)
                {
                    return NotFound(new { error = "Connection not found" });
                }

                return await ServiceInvoke(() => Task.FromResult(updatedConnection));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update connection {ConnectionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CONNECTION_UPDATE_FAILED",
                    "Failed to update connection",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
