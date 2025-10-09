using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Security.Certificate;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

/// <summary>
/// Controller for testing utilities (certificate generation, etc.)
/// Only available in Development, NodeA, and NodeB environments
/// </summary>
[ApiController]
[Route("api/testing")]
public class TestingController : ControllerBase
{
    private readonly ILogger<TestingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChannelStore _channelStore;
    private readonly IChannelEncryptionService _channelEncryptionService;
    private readonly INodeChannelClient _nodeChannelClient;

    public TestingController(
        ILogger<TestingController> logger,
        IConfiguration configuration,
        IChannelStore channelStore,
        IChannelEncryptionService channelEncryptionService,
        INodeChannelClient nodeChannelClient)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._channelStore = channelStore;
        this._channelEncryptionService = channelEncryptionService;
        this._nodeChannelClient = nodeChannelClient;
    }

    /// <summary>
    /// Generate a self-signed certificate for testing
    /// </summary>
    /// <param name="subjectName">Subject name (CN) for the certificate</param>
    /// <returns>Certificate in Base64 format and private key (PFX)</returns>
    [HttpPost("generate-certificate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GenerateCertificate([FromBody] GenerateCertificateRequest request)
    {
        try
        {
            // Validate subject name
            if (string.IsNullOrWhiteSpace(request.SubjectName))
            {
                return BadRequest(new { error = "SubjectName is required" });
            }

            _logger.LogInformation("Generating self-signed certificate for {SubjectName}", request.SubjectName);

            // Generate certificate
            var certificate = CertificateHelper.GenerateSelfSignedCertificate(
                request.SubjectName,
                request.ValidityYears);

            // Export certificate (public key only)
            var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);

            // Export certificate with private key (PFX format)
            var pfxBase64 = CertificateHelper.ExportCertificateWithPrivateKeyToBase64(
                certificate,
                request.Password ?? "test123");

            _logger.LogInformation("Certificate generated successfully for {SubjectName}", request.SubjectName);

            return Ok(new
            {
                subjectName = request.SubjectName,
                certificate = certBase64,
                certificateWithPrivateKey = pfxBase64,
                password = request.Password ?? "test123",
                validFrom = certificate.NotBefore,
                validTo = certificate.NotAfter,
                thumbprint = certificate.Thumbprint,
                serialNumber = certificate.SerialNumber,
                usage = new
                {
                    certificate = "Use this for registration and identification (public key)",
                    certificateWithPrivateKey = "Use this to sign data (includes private key)",
                    password = "Password to load the PFX certificate"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate certificate");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to generate certificate",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Sign data with a certificate's private key
    /// </summary>
    [HttpPost("sign-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SignData([FromBody] SignDataRequest request)
    {
        try
        {
            _logger.LogInformation("Signing data with certificate");

            // Load certificate with private key
            var certificate = CertificateHelper.LoadCertificateWithPrivateKeyFromBase64(
                request.CertificateWithPrivateKey,
                request.Password);

            // Sign data
            var signature = CertificateHelper.SignData($"{request.Data}{request.ChannelId}{request.NodeId}{request.Timestamp:O}", certificate);

            _logger.LogInformation("Data signed successfully");

            return Ok(new
            {
                data = request.Data,
                signature = signature,
                algorithm = "RSA-SHA256"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign data");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to sign data",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Verify signature with a certificate's public key
    /// </summary>
    [HttpPost("verify-signature")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult VerifySignature([FromBody] VerifySignatureRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying signature");

            // Load certificate (public key only)
            var certificate = CertificateHelper.LoadCertificateFromBase64(request.Certificate);

            // Verify signature
            var isValid = CertificateHelper.VerifySignature(
                request.Data,
                request.Signature,
                certificate);

            _logger.LogInformation("Signature verification result: {IsValid}", isValid);

            return Ok(new
            {
                data = request.Data,
                signature = request.Signature,
                isValid = isValid,
                algorithm = "RSA-SHA256"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to verify signature",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a complete node identity package (certificate + test signature)
    /// </summary>
    [HttpPost("generate-node-identity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GenerateNodeIdentity([FromBody] GenerateNodeIdentityRequest request)
    {
        try
        {
            _logger.LogInformation("Generating node identity for {NodeId}", request.NodeId);

            // Generate certificate
            var certificate = CertificateHelper.GenerateSelfSignedCertificate(
                request.NodeId,
                request.ValidityYears);

            var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);
            var pfxBase64 = CertificateHelper.ExportCertificateWithPrivateKeyToBase64(
                certificate,
                request.Password ?? "test123");

            // Generate test signature for identification
            var timestamp = DateTime.UtcNow;
            var dataToSign = $"{request.ChannelId}{request.NodeId}{timestamp:O}";
            var signature = CertificateHelper.SignData(dataToSign, certificate);

            _logger.LogInformation("Node identity generated successfully for {NodeId}", request.NodeId);

            return Ok(new
            {
                nodeId = request.NodeId,
                nodeName = request.NodeName,
                certificate = certBase64,
                certificateWithPrivateKey = pfxBase64,
                password = request.Password ?? "test123",
                identificationRequest = new
                {
                    channelId = request.ChannelId,
                    nodeId = request.NodeId,
                    nodeName = request.NodeName,
                    certificate = certBase64,
                    timestamp = timestamp,
                    signature = signature
                },
                usage = "Use 'identificationRequest' object to call /api/channel/identify"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate node identity");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to generate node identity",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Encrypt a payload using an active channel's symmetric key
    /// This endpoint is useful for testing encrypted communication
    /// </summary>
    /// <param name="request">Request with channel ID and payload to encrypt</param>
    /// <returns>Encrypted payload with IV and auth tag</returns>
    [HttpPost("encrypt-payload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EncryptPayload([FromBody] EncryptPayloadRequest request)
    {
        try
        {
            _logger.LogInformation("Encrypting payload for channel {ChannelId}", request.ChannelId);

            // 1. Get channel from store
            var channel = await _channelStore.GetChannelAsync(request.ChannelId);
            if (channel == null)
            {
                _logger.LogWarning("Channel {ChannelId} not found or expired", request.ChannelId);
                return NotFound(new
                {
                    error = "Channel not found",
                    message = $"Channel {request.ChannelId} does not exist or has expired",
                    hint = "Use POST /api/channel/open to create a new channel first"
                });
            }
           

            // 3. Encrypt the payload
            var encryptedPayload = _channelEncryptionService.EncryptPayload(request.Payload, channel.SymmetricKey);

            _logger.LogInformation("Successfully encrypted payload for channel {ChannelId}", request.ChannelId);

            return Ok(new
            {
                channelId = request.ChannelId,
                encryptedPayload = encryptedPayload,
                channelInfo = new
                {
                    cipher = channel.SelectedCipher,
                    role = channel.Role,
                    createdAt = channel.CreatedAt,
                    expiresAt = channel.ExpiresAt
                },
                usage = "Use this encrypted payload in requests to /api/channel/send-message or similar encrypted endpoints"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload for channel {ChannelId}", request.ChannelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to encrypt payload",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Decrypt a payload using an active channel's symmetric key
    /// This endpoint is useful for testing and debugging encrypted communication
    /// </summary>
    /// <param name="request">Request with channel ID and encrypted payload</param>
    /// <returns>Decrypted payload as JSON</returns>
    [HttpPost("decrypt-payload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DecryptPayload([FromBody] DecryptPayloadRequest request)
    {
        try
        {
            _logger.LogInformation("Decrypting payload for channel {ChannelId}", request.ChannelId);

            // 1. Get channel from store
            var channel = await _channelStore.GetChannelAsync(request.ChannelId);
            if (channel == null)
            {
                _logger.LogWarning("Channel {ChannelId} not found or expired", request.ChannelId);
                return NotFound(new
                {
                    error = "Channel not found",
                    message = $"Channel {request.ChannelId} does not exist or has expired"
                });
            }

            // 2. Decrypt the payload
            var decryptedPayload = _channelEncryptionService.DecryptPayload<JsonElement>(
                request.EncryptedPayload, 
                channel.SymmetricKey);

            _logger.LogInformation("Successfully decrypted payload for channel {ChannelId}", request.ChannelId);

            return Ok(new
            {
                channelId = request.ChannelId,
                decryptedPayload = decryptedPayload,
                channelInfo = new
                {
                    cipher = channel.SelectedCipher,
                    role = channel.Role,
                    createdAt = channel.CreatedAt,
                    expiresAt = channel.ExpiresAt
                }
            });
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload - authentication failed");
            return BadRequest(new
            {
                error = "Decryption failed",
                message = "Authentication failed. The payload may have been tampered with or the wrong key was used.",
                details = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload for channel {ChannelId}", request.ChannelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to decrypt payload",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Phase 3: Request authentication challenge from a remote node
    /// This is a testing helper that wraps NodeChannelClient.RequestChallengeAsync
    /// </summary>
    /// <param name="request">Challenge request with remote URL, channel ID, and node ID</param>
    /// <returns>Challenge response with challenge data and expiration</returns>
    [HttpPost("request-challenge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestChallenge([FromBody] TestChallengeRequest request)
    {
        try
        {
            _logger.LogInformation("Testing: Requesting challenge for node {NodeId} on channel {ChannelId}",
                request.NodeId, request.ChannelId);

            // Use NodeChannelClient to request challenge
            var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(
                request.ChannelId,
                request.NodeId);

            _logger.LogInformation("Challenge received successfully for node {NodeId}", request.NodeId);

            return Ok(new
            {
                success = true,
                channelId = request.ChannelId,
                nodeId = request.NodeId,
                challengeResponse = new
                {
                    challengeData = challengeResponse.ChallengeData,
                    challengeTimestamp = challengeResponse.ChallengeTimestamp,
                    challengeTtlSeconds = challengeResponse.ChallengeTtlSeconds,
                    expiresAt = challengeResponse.ExpiresAt
                },
                nextStep = new
                {
                    action = "Sign the challengeData with your node's private key",
                    format = "{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}",
                    endpoint = "POST /api/testing/authenticate"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request challenge for node {NodeId}", request.NodeId);
            return BadRequest(new
            {
                success = false,
                error = "Failed to request challenge",
                message = ex.Message,
                hint = "Make sure the node is registered and authorized (status=Authorized)"
            });
        }
    }

    /// <summary>
    /// Phase 3: Sign challenge data for authentication
    /// This helper signs the challenge data in the correct format for Phase 3
    /// Format: {ChallengeData}{ChannelId}{NodeId}{Timestamp:O}
    /// </summary>
    /// <param name="request">Challenge signing request</param>
    /// <returns>Signature that can be used in authenticate endpoint</returns>
    [HttpPost("sign-challenge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SignChallenge([FromBody] SignChallengeRequest request)
    {
        try
        {
            _logger.LogInformation("Signing challenge for node {NodeId}", request.NodeId);

            // Load certificate with private key
            var certificate = CertificateHelper.LoadCertificateWithPrivateKeyFromBase64(
                request.CertificateWithPrivateKey,
                request.Password);

            // Build data to sign in Phase 3 format: ChallengeData + ChannelId + NodeId + Timestamp
            var dataToSign = $"{request.ChallengeData}{request.ChannelId}{request.NodeId}{request.Timestamp:O}";
            var signature = CertificateHelper.SignData(dataToSign, certificate);

            _logger.LogInformation("Challenge signed successfully for node {NodeId}", request.NodeId);

            return Ok(new
            {
                success = true,
                challengeData = request.ChallengeData,
                channelId = request.ChannelId,
                nodeId = request.NodeId,
                timestamp = request.Timestamp,
                signature = signature,
                signedData = dataToSign,
                algorithm = "RSA-SHA256",
                usage = new
                {
                    endpoint = "POST /api/testing/authenticate",
                    description = "Use this signature in the authenticate request"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign challenge for node {NodeId}", request.NodeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                error = "Failed to sign challenge",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Phase 3: Authenticate node using signed challenge
    /// This is a testing helper that wraps NodeChannelClient.AuthenticateAsync
    /// </summary>
    /// <param name="request">Authentication request with signed challenge</param>
    /// <returns>Authentication response with session token</returns>
    [HttpPost("authenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Authenticate([FromBody] TestAuthenticateRequest request)
    {
        try
        {
            _logger.LogInformation("Testing: Authenticating node {NodeId} on channel {ChannelId}",
                request.NodeId, request.ChannelId);

            // Build challenge response request
            var challengeResponseRequest = new ChallengeResponseRequest
            {
                ChannelId = request.ChannelId,
                NodeId = request.NodeId,
                ChallengeData = request.ChallengeData,
                Signature = request.Signature,
                Timestamp = request.Timestamp
            };

            // Use NodeChannelClient to authenticate
            var authResponse = await _nodeChannelClient.AuthenticateAsync(
                request.ChannelId,
                challengeResponseRequest);

            _logger.LogInformation("Node {NodeId} authenticated successfully", request.NodeId);

            return Ok(new
            {
                success = true,
                channelId = request.ChannelId,
                nodeId = request.NodeId,
                authenticationResponse = new
                {
                    authenticated = authResponse.Authenticated,
                    sessionToken = authResponse.SessionToken,
                    sessionExpiresAt = authResponse.SessionExpiresAt,
                    grantedCapabilities = authResponse.GrantedNodeAccessLevel,
                    message = authResponse.Message,
                    nextPhase = authResponse.NextPhase,
                    timestamp = authResponse.Timestamp
                },
                usage = new
                {
                    sessionToken = "Use this token in subsequent authenticated requests",
                    ttl = "Session expires in 1 hour",
                    capabilities = authResponse.GrantedNodeAccessLevel
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate node {NodeId}", request.NodeId);
            return BadRequest(new
            {
                success = false,
                error = "Failed to authenticate",
                message = ex.Message,
                possibleCauses = new[]
                {
                    "Challenge has expired (TTL: 5 minutes)",
                    "Challenge data does not match the one generated",
                    "Invalid signature (wrong private key or wrong data format)",
                    "Challenge was already used (one-time use only)"
                }
            });
        }
    }

    /// <summary>
    /// Get information about an active channel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Channel information (without sensitive keys)</returns>
    [HttpGet("channel-info/{channelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChannelInfo(string channelId)
    {
        try
        {
            _logger.LogInformation("Getting channel info for {ChannelId}", channelId);

            var channel = await _channelStore.GetChannelAsync(channelId);
            if (channel == null)
            {
                return NotFound(new
                {
                    error = "Channel not found",
                    message = $"Channel {channelId} does not exist or has expired"
                });
            }

            return Ok(new
            {
                channelId = channel.ChannelId,
                cipher = channel.SelectedCipher,
                role = channel.Role,
                createdAt = channel.CreatedAt,
                expiresAt = channel.ExpiresAt,
                isExpired = channel.ExpiresAt <= DateTime.UtcNow,
                remoteNodeUrl = channel.RemoteNodeUrl,
                clientNonce = channel.ClientNonce,
                serverNonce = channel.ServerNonce,
                symmetricKeyLength = channel.SymmetricKey.Length,
                identifiedNodeId = channel.IdentifiedNodeId, // Guid (internal) set after Phase 2
                certificateFingerprint = channel.CertificateFingerprint
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channel info for {ChannelId}", channelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to get channel info",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Helper endpoint: Complete Phase 1+2 flow (open channel + identify node)
    /// This simplifies manual testing by combining channel establishment and node identification
    /// </summary>
    /// <param name="request">Complete handshake request</param>
    /// <returns>Channel ID, registration ID (Guid), and next steps</returns>
    [HttpPost("complete-phase1-phase2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompletePhase1Phase2([FromBody] CompletePhase1Phase2Request request)
    {
        try
        {
            _logger.LogInformation("Starting Phase 1+2 flow for node {NodeId} to remote {RemoteUrl}",
                request.NodeId, request.RemoteNodeUrl);

            // Phase 1: Open encrypted channel
            var channelResponse = await _nodeChannelClient.OpenChannelAsync(request.RemoteNodeUrl);
            var channelId = channelResponse.ChannelId;

            _logger.LogInformation("Phase 1 complete. Channel {ChannelId} established", channelId);

            // Load certificate for fingerprint calculation and identity creation
            var certificate = CertificateHelper.LoadCertificateFromBase64(request.Certificate);
            var certBytes = Convert.FromBase64String(request.Certificate);
            string certificateFingerprint;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(certBytes);
                certificateFingerprint = Convert.ToBase64String(hash);
            }

            // Phase 2: Identify node
            var timestamp = DateTime.UtcNow;
            var dataToSign = $"{channelId}{request.NodeId}{timestamp:O}";

            // Load private key if provided, otherwise skip signature
            string signature = string.Empty;
            if (!string.IsNullOrEmpty(request.CertificateWithPrivateKey))
            {
                var privateCert = CertificateHelper.LoadCertificateWithPrivateKeyFromBase64(
                    request.CertificateWithPrivateKey,
                    request.Password ?? "test123");
                signature = CertificateHelper.SignData(dataToSign, privateCert);
            }

            var identifyRequest = new NodeIdentifyRequest
            {
                ChannelId = channelId,
                NodeId = request.NodeId,
                NodeName = request.NodeName,
                Certificate = request.Certificate,
                Timestamp = timestamp,
                Signature = signature
            };

            var identifyResponse = await _nodeChannelClient.IdentifyNodeAsync(channelId, identifyRequest);

            _logger.LogInformation("Phase 2 complete. Node identification response: IsKnown={IsKnown}, Status={Status}",
                identifyResponse.IsKnown, identifyResponse.Status);

            // Get channel info to get expiration time
            var channel = await _channelStore.GetChannelAsync(channelId);

            return Ok(new
            {
                success = true,
                phase1 = new
                {
                    channelId = channelId,
                    cipher = channelResponse.SelectedCipher,
                    expiresAt = channel?.ExpiresAt
                },
                phase2 = new
                {
                    isKnown = identifyResponse.IsKnown,
                    status = identifyResponse.Status.ToString(),
                    nodeId = identifyResponse.NodeId, // String NodeId (protocol format)
                    registrationId = identifyResponse.RegistrationId, // Guid (internal ID) if known
                    certificateFingerprint = certificateFingerprint,
                    message = identifyResponse.Message,
                    registrationUrl = identifyResponse.RegistrationUrl
                },
                nextSteps = identifyResponse.IsKnown && identifyResponse.Status == AuthorizationStatus.Authorized
                    ? (object)new
                    {
                        phase = "Phase 3: Request Challenge",
                        endpoint = "POST /api/testing/request-challenge",
                        payload = new
                        {
                            channelId = channelId,
                            nodeId = request.NodeId // Use string NodeId for protocol
                        }
                    }
                    : identifyResponse.IsKnown
                        ? (object)new
                        {
                            phase = "Waiting for authorization",
                            action = $"Update node status to Authorized: PUT /api/node/{identifyResponse.RegistrationId}/status",
                            payload = new { status = "Authorized" }
                        }
                        : (object)new
                        {
                            phase = "Phase 2: Registration Required",
                            endpoint = "POST /api/node/register",
                            action = "Register the node first",
                            registrationUrl = identifyResponse.RegistrationUrl
                        }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Phase 1+2 flow");
            return BadRequest(new
            {
                success = false,
                error = "Failed to complete Phase 1+2 flow",
                message = ex.Message
            });
        }
    }
}

public class GenerateCertificateRequest
{
    public string SubjectName { get; set; } = string.Empty;
    public int ValidityYears { get; set; } = 2;
    public string? Password { get; set; } = "test123";
}

public class SignDataRequest
{
    public string Data { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;


    public string CertificateWithPrivateKey { get; set; } = string.Empty;
    public string Password { get; set; } = "test123";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

}

public class VerifySignatureRequest
{
    public string Data { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Certificate { get; set; } = string.Empty;
}

public class GenerateNodeIdentityRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public int ValidityYears { get; set; } = 2;
    public string? Password { get; set; } = "test123";
}

public class EncryptPayloadRequest
{
    /// <summary>
    /// Channel ID to use for encryption
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Payload to encrypt (can be any JSON object)
    /// </summary>
    public object Payload { get; set; }
}

public class DecryptPayloadRequest
{
    /// <summary>
    /// Channel ID to use for decryption
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted payload to decrypt
    /// </summary>
    public EncryptedPayload EncryptedPayload { get; set; } = new();
}

public class TestChallengeRequest
{
    /// <summary>
    /// Channel ID from Phase 1
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Node ID requesting authentication
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
}

public class SignChallengeRequest
{
    /// <summary>
    /// Challenge data received from request-challenge
    /// </summary>
    public string ChallengeData { get; set; } = string.Empty;

    /// <summary>
    /// Channel ID from Phase 1
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Node ID authenticating
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Certificate with private key (PFX format, Base64)
    /// </summary>
    public string CertificateWithPrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Password for the PFX certificate
    /// </summary>
    public string Password { get; set; } = "test123";

    /// <summary>
    /// Timestamp to use in signature
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TestAuthenticateRequest
{
    /// <summary>
    /// Channel ID from Phase 1
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Node ID authenticating
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Challenge data received from request-challenge
    /// </summary>
    public string ChallengeData { get; set; } = string.Empty;

    /// <summary>
    /// RSA signature of: {ChallengeData}{ChannelId}{NodeId}{Timestamp:O}
    /// Use /api/testing/sign-challenge to generate
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp used in signature (must match signed data)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompletePhase1Phase2Request
{
    /// <summary>
    /// Remote node URL to connect to (e.g., http://localhost:5001)
    /// </summary>
    public string RemoteNodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Node ID (string identifier used in protocol DTOs)
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Node display name
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Certificate (public key only, Base64)
    /// Generate using: POST /api/testing/generate-certificate
    /// </summary>
    public string Certificate { get; set; } = string.Empty;

    /// <summary>
    /// Certificate with private key (PFX format, Base64) - optional
    /// Required for signing the identification request
    /// </summary>
    public string? CertificateWithPrivateKey { get; set; }

    /// <summary>
    /// Password for the PFX certificate
    /// </summary>
    public string? Password { get; set; } = "test123";
}
