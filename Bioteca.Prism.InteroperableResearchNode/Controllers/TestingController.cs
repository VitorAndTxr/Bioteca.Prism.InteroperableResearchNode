using Bioteca.Prism.Service.Interfaces.Node;
using Bioteca.Prism.Service.Services.Node;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
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

    public TestingController(
        ILogger<TestingController> logger,
        IConfiguration configuration,
        IChannelStore channelStore,
        IChannelEncryptionService channelEncryptionService)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._channelStore = channelStore;
        this._channelEncryptionService = channelEncryptionService;
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
            var signature = CertificateHelper.SignData(request.Data, certificate);

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
    public IActionResult EncryptPayload([FromBody] EncryptPayloadRequest request)
    {
        try
        {
            _logger.LogInformation("Encrypting payload for channel {ChannelId}", request.ChannelId);

            // 1. Get channel from store
            var channel = _channelStore.GetChannel(request.ChannelId);
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
    public IActionResult DecryptPayload([FromBody] DecryptPayloadRequest request)
    {
        try
        {
            _logger.LogInformation("Decrypting payload for channel {ChannelId}", request.ChannelId);

            // 1. Get channel from store
            var channel = _channelStore.GetChannel(request.ChannelId);
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
    /// Get information about an active channel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Channel information (without sensitive keys)</returns>
    [HttpGet("channel-info/{channelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetChannelInfo(string channelId)
    {
        try
        {
            _logger.LogInformation("Getting channel info for {ChannelId}", channelId);

            var channel = _channelStore.GetChannel(channelId);
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
                symmetricKeyLength = channel.SymmetricKey.Length
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
    public string CertificateWithPrivateKey { get; set; } = string.Empty;
    public string Password { get; set; } = "test123";
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
