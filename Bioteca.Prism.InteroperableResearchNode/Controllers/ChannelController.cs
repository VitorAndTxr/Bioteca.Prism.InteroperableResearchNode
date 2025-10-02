using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Node;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// Controller for Phase 1: Encrypted Channel Establishment and Phase 2: Node Identification
/// </summary>
[ApiController]
[Route("api/channel")]
public class ChannelController : ControllerBase
{
    private readonly ILogger<ChannelController> _logger;
    private readonly IEphemeralKeyService _ephemeralKeyService;
    private readonly IChannelEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly INodeChannelClient _channelClient;
    private readonly Service.Services.Node.INodeRegistryService _nodeRegistry;

    // In-memory storage for active channels (in production, use distributed cache)
    private static readonly ConcurrentDictionary<string, ChannelContext> _activeChannels = new();

    public ChannelController(
        ILogger<ChannelController> logger,
        IEphemeralKeyService ephemeralKeyService,
        IChannelEncryptionService encryptionService,
        IConfiguration configuration,
        INodeChannelClient channelClient,
        Service.Services.Node.INodeRegistryService nodeRegistry)
    {
        _logger = logger;
        _ephemeralKeyService = ephemeralKeyService;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _channelClient = channelClient;
        _nodeRegistry = nodeRegistry;
    }

    /// <summary>
    /// Open a new encrypted channel using ephemeral ECDH keys
    /// </summary>
    /// <param name="request">Channel open request with ephemeral public key</param>
    /// <returns>Channel ready response with server's ephemeral public key</returns>
    [HttpPost("open")]
    [ProducesResponseType(typeof(ChannelReadyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status500InternalServerError)]
    public IActionResult OpenChannel([FromBody] ChannelOpenRequest request)
    {
        try
        {
            _logger.LogInformation("Received channel open request from client");

            // Validate request
            var validationError = ValidateChannelOpenRequest(request);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }

            // Validate ephemeral public key
            var curve = ExtractCurveFromAlgorithm(request.KeyExchangeAlgorithm);
            if (!_ephemeralKeyService.ValidatePublicKey(request.EphemeralPublicKey, curve))
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_EPHEMERAL_KEY",
                    "Ephemeral public key is invalid or malformed",
                    new Dictionary<string, object> { ["reason"] = "invalid_curve_point" },
                    retryable: true
                ));
            }

            // Select cipher
            var supportedCiphers = GetSupportedCiphers();
            var selectedCipher = request.SupportedCiphers.FirstOrDefault(c => supportedCiphers.Contains(c));

            if (selectedCipher == null)
            {
                return BadRequest(CreateError(
                    "ERR_CHANNEL_FAILED",
                    "No compatible cipher found",
                    new Dictionary<string, object>
                    {
                        ["clientCiphers"] = request.SupportedCiphers,
                        ["serverCiphers"] = supportedCiphers
                    },
                    retryable: false
                ));
            }

            // Generate server's ephemeral key pair
            var serverEcdh = _ephemeralKeyService.GenerateEphemeralKeyPair(curve);
            var serverPublicKey = _ephemeralKeyService.ExportPublicKey(serverEcdh);

            // Import client's public key
            var clientEcdh = _ephemeralKeyService.ImportPublicKey(request.EphemeralPublicKey, curve);

            // Derive shared secret
            var sharedSecret = _ephemeralKeyService.DeriveSharedSecret(serverEcdh, clientEcdh);

            // Derive symmetric key using HKDF
            var salt = CombineNonces(request.Nonce, _encryptionService.GenerateNonce());
            var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");
            var symmetricKey = _encryptionService.DeriveKey(sharedSecret, salt, info);

            // Generate response nonce
            var responseNonce = _encryptionService.GenerateNonce();

            // Store channel context (with expiration)
            var channelId = Guid.NewGuid().ToString();
            var channelContext = new ChannelContext
            {
                ChannelId = channelId,
                SymmetricKey = symmetricKey,
                SelectedCipher = selectedCipher,
                ClientNonce = request.Nonce,
                ServerNonce = responseNonce,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30 minutes expiration
            };

            _activeChannels.TryAdd(channelId, channelContext);

            // Clean up ECDH objects
            serverEcdh.Dispose();
            clientEcdh.Dispose();
            Array.Clear(sharedSecret, 0, sharedSecret.Length);

            _logger.LogInformation("Channel {ChannelId} established successfully with cipher {Cipher}",
                channelId, selectedCipher);

            // Return response
            var response = new ChannelReadyResponse
            {
                ProtocolVersion = request.ProtocolVersion,
                EphemeralPublicKey = serverPublicKey,
                KeyExchangeAlgorithm = request.KeyExchangeAlgorithm,
                SelectedCipher = selectedCipher,
                Timestamp = DateTime.UtcNow,
                Nonce = responseNonce
            };

            // Add channel ID in response header for client to use in subsequent requests
            Response.Headers.Append("X-Channel-Id", channelId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open encrypted channel");

            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CHANNEL_FAILED",
                "Failed to establish encrypted channel",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    /// <summary>
    /// Initiate handshake with a remote node (acting as client)
    /// </summary>
    /// <param name="request">Request with remote node URL</param>
    /// <returns>Channel establishment result</returns>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(ChannelEstablishmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateHandshake([FromBody] InitiateHandshakeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RemoteNodeUrl))
        {
            return BadRequest(CreateError("ERR_INVALID_REQUEST", "Remote node URL is required", retryable: false));
        }

        var result = await _channelClient.OpenChannelAsync(request.RemoteNodeUrl);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get channel information (for testing/debugging)
    /// </summary>
    [HttpGet("{channelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetChannel(string channelId)
    {
        // Check server-side channels
        if (_activeChannels.TryGetValue(channelId, out var serverContext))
        {
            return Ok(new
            {
                channelId = serverContext.ChannelId,
                cipher = serverContext.SelectedCipher,
                createdAt = serverContext.CreatedAt,
                expiresAt = serverContext.ExpiresAt,
                isExpired = serverContext.ExpiresAt < DateTime.UtcNow,
                role = "server"
            });
        }

        // Check client-side channels
        var clientContext = _channelClient.GetChannel(channelId);
        if (clientContext != null)
        {
            return Ok(new
            {
                channelId = clientContext.ChannelId,
                cipher = clientContext.SelectedCipher,
                remoteNodeUrl = clientContext.RemoteNodeUrl,
                createdAt = clientContext.CreatedAt,
                expiresAt = clientContext.ExpiresAt,
                isExpired = clientContext.ExpiresAt < DateTime.UtcNow,
                role = "client"
            });
        }

        return NotFound();
    }

    /// <summary>
    /// Phase 2: Identify node after encrypted channel is established
    /// </summary>
    /// <param name="request">Node identification request with certificate and signature</param>
    /// <returns>Node status response (known/unknown)</returns>
    [HttpPost("identify")]
    [ProducesResponseType(typeof(NodeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IdentifyNode([FromBody] NodeIdentifyRequest request)
    {
        try
        {
            _logger.LogInformation("Received node identification request for NodeId: {NodeId}", request.NodeId);

            // Validate channel exists
            if (!_activeChannels.ContainsKey(request.ChannelId))
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_CHANNEL",
                    "Channel does not exist or has expired",
                    retryable: true
                ));
            }

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

                return Ok(new NodeStatusResponse
                {
                    IsKnown = false,
                    Status = AuthorizationStatus.Unknown,
                    NodeId = request.NodeId,
                    Timestamp = DateTime.UtcNow,
                    RegistrationUrl = $"{Request.Scheme}://{Request.Host}/api/node/register",
                    Message = "Node is not registered. Please register using the provided URL.",
                    NextPhase = null
                });
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

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying node {NodeId}", request.NodeId);
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
    /// <param name="request">Node registration request</param>
    /// <returns>Registration response</returns>
    [HttpPost("register")]
    [Route("/api/node/register")]
    [ProducesResponseType(typeof(NodeRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterNode([FromBody] NodeRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Received node registration request for NodeId: {NodeId}", request.NodeId);

            var response = await _nodeRegistry.RegisterNodeAsync(request);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering node {NodeId}", request.NodeId);
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
    [HttpGet("nodes")]
    [Route("/api/node/nodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNodes()
    {
        var nodes = await _nodeRegistry.GetAllNodesAsync();
        return Ok(nodes);
    }

    /// <summary>
    /// Update node authorization status (for admin purposes)
    /// </summary>
    [HttpPut("nodes/{nodeId}/status")]
    [Route("/api/node/{nodeId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNodeStatus(string nodeId, [FromBody] UpdateNodeStatusRequest request)
    {
        var success = await _nodeRegistry.UpdateNodeStatusAsync(nodeId, request.Status);

        if (!success)
        {
            return NotFound(new { message = "Node not found" });
        }

        return Ok(new { message = "Node status updated successfully", nodeId, status = request.Status });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private HandshakeError? ValidateChannelOpenRequest(ChannelOpenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProtocolVersion))
        {
            return CreateError("ERR_INCOMPATIBLE_VERSION", "Protocol version is required", retryable: false);
        }

        if (request.ProtocolVersion != "1.0")
        {
            return CreateError(
                "ERR_INCOMPATIBLE_VERSION",
                "Unsupported protocol version",
                new Dictionary<string, object>
                {
                    ["clientVersion"] = request.ProtocolVersion,
                    ["serverVersion"] = "1.0"
                },
                retryable: false
            );
        }

        if (string.IsNullOrWhiteSpace(request.EphemeralPublicKey))
        {
            return CreateError("ERR_INVALID_EPHEMERAL_KEY", "Ephemeral public key is required", retryable: true);
        }

        if (string.IsNullOrWhiteSpace(request.KeyExchangeAlgorithm))
        {
            return CreateError("ERR_CHANNEL_FAILED", "Key exchange algorithm is required", retryable: false);
        }

        if (request.SupportedCiphers == null || !request.SupportedCiphers.Any())
        {
            return CreateError("ERR_CHANNEL_FAILED", "At least one cipher must be specified", retryable: false);
        }

        if (string.IsNullOrWhiteSpace(request.Nonce))
        {
            return CreateError("ERR_CHANNEL_FAILED", "Nonce is required", retryable: true);
        }

        return null;
    }

    private List<string> GetSupportedCiphers()
    {
        var ciphers = _configuration.GetSection("ChannelSecurity:SupportedCiphers").Get<List<string>>();
        return ciphers ?? new List<string> { "AES-256-GCM", "ChaCha20-Poly1305" };
    }

    private string ExtractCurveFromAlgorithm(string algorithm)
    {
        // Extract curve from "ECDH-P384" -> "P384"
        var parts = algorithm.Split('-');
        return parts.Length > 1 ? parts[1] : "P384";
    }

    private byte[] CombineNonces(string nonce1, string nonce2)
    {
        var bytes1 = Convert.FromBase64String(nonce1);
        var bytes2 = Convert.FromBase64String(nonce2);
        var combined = new byte[bytes1.Length + bytes2.Length];
        Buffer.BlockCopy(bytes1, 0, combined, 0, bytes1.Length);
        Buffer.BlockCopy(bytes2, 0, combined, bytes1.Length, bytes2.Length);
        return combined;
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

/// <summary>
/// Context for an active channel
/// </summary>
internal class ChannelContext
{
    public string ChannelId { get; set; } = string.Empty;
    public byte[] SymmetricKey { get; set; } = Array.Empty<byte>();
    public string SelectedCipher { get; set; } = string.Empty;
    public string ClientNonce { get; set; } = string.Empty;
    public string ServerNonce { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
