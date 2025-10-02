using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Node;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [ApiController]
    [Route("api/node")]
    public class NodeConnectionController : ControllerBase
    {
        private readonly ILogger<NodeConnectionController> _logger;
        private readonly IEphemeralKeyService _ephemeralKeyService;
        private readonly IChannelEncryptionService _encryptionService;
        private readonly IConfiguration _configuration;
        private readonly INodeChannelClient _channelClient;
        private readonly Service.Services.Node.INodeRegistryService _nodeRegistry;
        private readonly IChannelStore _channelStore;
        public NodeConnectionController(
            ILogger<NodeConnectionController> logger,
             IEphemeralKeyService ephemeralKeyService,
            IChannelEncryptionService encryptionService,
            IConfiguration configuration,
            INodeChannelClient channelClient,
            Service.Services.Node.INodeRegistryService nodeRegistry,
            IChannelStore channelStore)
        {
            _logger = logger;
            _ephemeralKeyService = ephemeralKeyService;
            _encryptionService = encryptionService;
            _configuration = configuration;
            _channelClient = channelClient;
            _nodeRegistry = nodeRegistry;
            _channelStore = channelStore;
        }
        /// <summary>
        /// Phase 2: Identify node after encrypted channel is established
        /// </summary>
        /// <param name="encryptedRequest">Encrypted node identification request</param>
        /// <returns>Encrypted node status response (known/unknown)</returns>
        [HttpPost("identify")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IdentifyNode([FromBody] EncryptedPayload encryptedRequest)
        {
            try
            {
                // Get ChannelId from header
                if (!Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
                {
                    return BadRequest(CreateError(
                        "ERR_MISSING_CHANNEL_ID",
                        "X-Channel-Id header is required",
                        retryable: false
                    ));
                }

                var channelId = channelIdHeader.ToString();

                // Validate channel exists
                var channelContext = _channelStore.GetChannel(channelId);
                if (channelContext == null)
                {
                    return BadRequest(CreateError(
                        "ERR_INVALID_CHANNEL",
                        "Channel does not exist or has expired",
                        retryable: true
                    ));
                }

                // Decrypt request
                NodeIdentifyRequest request;
                try
                {
                    request = _encryptionService.DecryptPayload<NodeIdentifyRequest>(encryptedRequest, channelContext.SymmetricKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt identify request");
                    return BadRequest(CreateError(
                        "ERR_DECRYPTION_FAILED",
                        "Failed to decrypt request payload",
                        retryable: false
                    ));
                }

                _logger.LogInformation("Received encrypted node identification for NodeId: {NodeId}", request.NodeId);

                // Inject ChannelId from header into request for signature verification
                request.ChannelId = channelId;

                // Verify signature
                var signatureValid = await _nodeRegistry.VerifyNodeSignatureAsync(request);
                if (!signatureValid)
                {
                    return BadRequest(CreateError(
                        "ERR_INVALID_SIGNATURE",
                        "Node signature verification failed",
                        retryable: false
                    ));
                }

                // Check if node is known
                var registeredNode = await _nodeRegistry.GetNodeAsync(request.NodeId);

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

                    // Encrypt response
                    var encryptedUnknownResponse = _encryptionService.EncryptPayload(unknownResponse, channelContext.SymmetricKey);
                    return Ok(encryptedUnknownResponse);
                }

                // Known node - check authorization status
                _logger.LogInformation("Known node identified: {NodeId} with status {Status}",
                    request.NodeId, registeredNode.Status);

                var response = new NodeStatusResponse
                {
                    IsKnown = true,
                    Status = registeredNode.Status,
                    NodeId = registeredNode.NodeId,
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

                // Encrypt response
                var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);

                return Ok(encryptedResponse);
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
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterNode([FromBody] EncryptedPayload encryptedRequest)
        {
            try
            {
                // Get ChannelId from header
                if (!Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
                {
                    return BadRequest(CreateError(
                        "ERR_MISSING_CHANNEL_ID",
                        "X-Channel-Id header is required",
                        retryable: false
                    ));
                }

                var channelId = channelIdHeader.ToString();

                // Validate channel exists
                var channelContext = _channelStore.GetChannel(channelId);
                if (channelContext == null)
                {
                    return BadRequest(CreateError(
                        "ERR_INVALID_CHANNEL",
                        "Channel does not exist or has expired",
                        retryable: true
                    ));
                }

                // Decrypt request
                NodeRegistrationRequest request;
                try
                {
                    request = _encryptionService.DecryptPayload<NodeRegistrationRequest>(encryptedRequest, channelContext.SymmetricKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt registration request");
                    return BadRequest(CreateError(
                        "ERR_DECRYPTION_FAILED",
                        "Failed to decrypt request payload",
                        retryable: false
                    ));
                }

                _logger.LogInformation("Received encrypted node registration for NodeId: {NodeId}", request.NodeId);

                var response = await _nodeRegistry.RegisterNodeAsync(request);

                // Check if registration was successful
                if (!response.Success)
                {
                    return BadRequest(CreateError(
                        "ERR_REGISTRATION_FAILED",
                        response.Message ?? "Registration failed",
                        retryable: false
                    ));
                }

                // Encrypt response
                var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);

                return Ok(encryptedResponse);
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
        [HttpPut("/api/node/{nodeId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateNodeStatus(string nodeId, [FromBody] UpdateNodeStatusRequest request)
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

            var success = await _nodeRegistry.UpdateNodeStatusAsync(nodeId, request.Status);

            if (!success)
            {
                return NotFound(new { message = "Node not found" });
            }

            return Ok(new { message = "Node status updated successfully", nodeId, status = request.Status });
        }

        private HandshakeError CreateError(
            string code,
            string message,
            Dictionary<string, object>? details = null,
            bool retryable = false,
            string? retryAfter = null)
        {
            return new HandshakeError
            {
                Error = new ErrorDetails
                {
                    Code = code,
                    Message = message,
                    Details = details,
                    Retryable = retryable,
                    RetryAfter = retryAfter
                }
            };
        }
    }
}
