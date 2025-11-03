
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Volunteer;
using Bioteca.Prism.Service.Services.Volunteer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// Controller for Phase 1: Encrypted Channel Establishment and Phase 2: Node Identification
/// </summary>
[ApiController]
[Route("api/channel")]
public class ChannelController : BaseController
{
    private readonly ILogger<ChannelController> _logger;
    private readonly IEphemeralKeyService _ephemeralKeyService;
    private readonly IChannelEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly INodeChannelClient _channelClient;
    private readonly IResearchNodeService _nodeRegistry;
    private readonly IChannelStore _channelStore;

    public ChannelController(
        ILogger<ChannelController> logger,
        IEphemeralKeyService ephemeralKeyService,
        IChannelEncryptionService encryptionService,
        IConfiguration configuration,
        INodeChannelClient channelClient,
        IResearchNodeService nodeRegistry,
        IChannelStore channelStore,
        IApiContext apiContext

        ) :base(logger, configuration, apiContext)
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
    /// Open a new encrypted channel using ephemeral ECDH keys
    /// </summary>
    /// <param name="request">Channel open request with ephemeral public key</param>
    /// <returns>Channel ready response with server's ephemeral public key</returns>
    [HttpPost("open")]
    [ProducesResponseType(typeof(ChannelReadyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenChannel([FromBody] ChannelOpenRequest request)
    {
        try
        {
            var validationError = ValidateChannelOpenRequest(request);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }

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

            // Generate response nonce (MUST be generated BEFORE deriving key)
            var responseNonce = _encryptionService.GenerateNonce();

            // Derive symmetric key using HKDF
            var salt = CombineNonces(request.Nonce, responseNonce);
            var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");

            var symmetricKey = _encryptionService.DeriveKey(sharedSecret, salt, info);

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
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30 minutes expiration
                Role = "server"
            };

            _channelStore.AddChannelAsync(channelId, channelContext).Wait();

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


            HttpContext.Response.Headers.Append("X-Channel-Id", channelId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open encrypted channel");

            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CHANNEL_FAILED",
                "Failed to establish encrypted channel:" + ex.Message,
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
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
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
    public async Task<IActionResult> GetChannel(string channelId)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId);
        if (channelContext != null)
        {
            return Ok(new
            {
                channelId = channelContext.ChannelId,
                cipher = channelContext.SelectedCipher,
                remoteNodeUrl = channelContext.RemoteNodeUrl,
                createdAt = channelContext.CreatedAt,
                expiresAt = channelContext.ExpiresAt,
                isExpired = channelContext.ExpiresAt < DateTime.UtcNow,
                role = channelContext.Role
            });
        }

        return NotFound();
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

    private Error? ValidateChannelOpenRequest(ChannelOpenRequest request)
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

        // Validate nonce format (must be valid Base64)
        try
        {
            var nonceBytes = Convert.FromBase64String(request.Nonce);

            // Validate nonce length (minimum 12 bytes for security)
            if (nonceBytes.Length < 12)
            {
                return CreateError(
                    "ERR_INVALID_NONCE",
                    "Nonce must be at least 12 bytes",
                    new Dictionary<string, object> { ["nonceSize"] = nonceBytes.Length },
                    retryable: true
                );
            }
        }
        catch (FormatException)
        {
            return CreateError(
                "ERR_INVALID_NONCE",
                "Nonce must be valid Base64 string",
                retryable: true
            );
        }

        // Validate timestamp (protect against replay attacks and clock skew)
        var now = DateTime.UtcNow;
        var timeDifference = Math.Abs((now - request.Timestamp).TotalMinutes);

        if (request.Timestamp > now.AddMinutes(5))
        {
            return CreateError(
                "ERR_INVALID_TIMESTAMP",
                "Timestamp is too far in the future",
                new Dictionary<string, object>
                {
                    ["clientTimestamp"] = request.Timestamp,
                    ["serverTimestamp"] = now,
                    ["maxSkewMinutes"] = 5
                },
                retryable: true
            );
        }

        if (request.Timestamp < now.AddMinutes(-5))
        {
            return CreateError(
                "ERR_INVALID_TIMESTAMP",
                "Timestamp is too old (possible replay attack)",
                new Dictionary<string, object>
                {
                    ["clientTimestamp"] = request.Timestamp,
                    ["serverTimestamp"] = now,
                    ["maxAgeMinutes"] = 5
                },
                retryable: true
            );
        }

        return null;
    }

    protected string ExtractCurveFromAlgorithm(string algorithm)
    {
        // Extract curve from "ECDH-P384" -> "P384"
        var parts = algorithm.Split('-');
        return parts.Length > 1 ? parts[1] : "P384";
    }

    protected List<string> GetSupportedCiphers()
    {
        var ciphers = _configuration.GetSection("ChannelSecurity:SupportedCiphers").Get<List<string>>();
        return ciphers ?? new List<string> { "AES-256-GCM", "ChaCha20-Poly1305" };
    }

    protected byte[] CombineNonces(string nonce1, string nonce2)
    {
        var bytes1 = Convert.FromBase64String(nonce1);
        var bytes2 = Convert.FromBase64String(nonce2);
        var combined = new byte[bytes1.Length + bytes2.Length];
        Buffer.BlockCopy(bytes1, 0, combined, 0, bytes1.Length);
        Buffer.BlockCopy(bytes2, 0, combined, bytes1.Length, bytes2.Length);
        return combined;
    }
}
