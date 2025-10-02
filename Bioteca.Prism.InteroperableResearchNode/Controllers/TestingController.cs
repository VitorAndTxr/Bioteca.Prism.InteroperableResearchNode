using Bioteca.Prism.Service.Services.Node;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

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

    public TestingController(
        ILogger<TestingController> logger,
        IConfiguration _configuration)
    {
        this._logger = logger;
        this._configuration = _configuration;
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
