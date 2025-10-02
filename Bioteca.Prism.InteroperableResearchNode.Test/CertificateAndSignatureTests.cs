using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bioteca.Prism.Service.Services.Node;
using FluentAssertions;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for certificate generation, signing, and verification
/// Tests the /api/testing/* endpoints
/// </summary>
public class CertificateAndSignatureTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CertificateAndSignatureTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Certificate Generation Tests

    [Fact]
    public async Task GenerateCertificate_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            subjectName = "test-node-001",
            validityYears = 2,
            password = "test123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testing/generate-certificate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        jsonDoc.Should().NotBeNull();
        jsonDoc!.RootElement.GetProperty("certificate").GetString().Should().NotBeNullOrEmpty();
        jsonDoc.RootElement.GetProperty("certificateWithPrivateKey").GetString().Should().NotBeNullOrEmpty();
        jsonDoc.RootElement.GetProperty("thumbprint").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateCertificate_WithCustomValidity_ReturnsValidCertificate()
    {
        // Arrange
        var request = new
        {
            subjectName = "long-lived-node",
            validityYears = 5,
            password = "secure-password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testing/generate-certificate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        jsonDoc.Should().NotBeNull();

        // Verify validity period
        var validFrom = DateTime.Parse(jsonDoc!.RootElement.GetProperty("validFrom").GetString()!);
        var validTo = DateTime.Parse(jsonDoc.RootElement.GetProperty("validTo").GetString()!);
        var validityPeriod = validTo - validFrom;

        validityPeriod.TotalDays.Should().BeApproximately(365 * 5, 1); // ~5 years
    }

    [Fact]
    public async Task GenerateCertificate_WithEmptySubjectName_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            subjectName = "",
            validityYears = 2,
            password = "test123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testing/generate-certificate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Data Signing Tests

    [Fact]
    public async Task SignData_WithValidCertificate_ReturnsSignature()
    {
        // Arrange - Generate certificate first
        var certRequest = new
        {
            subjectName = "signing-test-node",
            validityYears = 1,
            password = "test123"
        };

        var certResponse = await _client.PostAsJsonAsync("/api/testing/generate-certificate", certRequest);
        using var certDoc = await certResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var certificateWithPrivateKey = certDoc!.RootElement.GetProperty("certificateWithPrivateKey").GetString();

        // Sign data
        var signRequest = new
        {
            data = "test-data-to-sign",
            certificateWithPrivateKey = certificateWithPrivateKey,
            password = "test123"
        };

        // Act
        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);

        // Assert
        signResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var signDoc = await signResponse.Content.ReadFromJsonAsync<JsonDocument>();
        signDoc.Should().NotBeNull();
        signDoc!.RootElement.GetProperty("signature").GetString().Should().NotBeNullOrEmpty();
        signDoc.RootElement.GetProperty("algorithm").GetString().Should().Be("RSA-SHA256");
    }

    [Fact]
    public async Task SignData_WithWrongPassword_ReturnsError()
    {
        // Arrange - Generate certificate first
        var certRequest = new
        {
            subjectName = "wrong-password-node",
            validityYears = 1,
            password = "correct-password"
        };

        var certResponse = await _client.PostAsJsonAsync("/api/testing/generate-certificate", certRequest);
        using var certDoc = await certResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var certificateWithPrivateKey = certDoc!.RootElement.GetProperty("certificateWithPrivateKey").GetString();

        // Try to sign with wrong password
        var signRequest = new
        {
            data = "test-data-to-sign",
            certificateWithPrivateKey = certificateWithPrivateKey,
            password = "wrong-password"
        };

        // Act
        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);

        // Assert
        signResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SignData_WithInvalidCertificate_ReturnsError()
    {
        // Arrange
        var signRequest = new
        {
            data = "test-data-to-sign",
            certificateWithPrivateKey = "INVALID_CERTIFICATE_DATA",
            password = "test123"
        };

        // Act
        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);

        // Assert
        signResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Signature Verification Tests

    [Fact]
    public async Task VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange - Generate certificate and sign data
        var certRequest = new
        {
            subjectName = "verify-test-node",
            validityYears = 1,
            password = "test123"
        };

        var certResponse = await _client.PostAsJsonAsync("/api/testing/generate-certificate", certRequest);
        using var certDoc = await certResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var certificate = certDoc!.RootElement.GetProperty("certificate").GetString();
        var certificateWithPrivateKey = certDoc.RootElement.GetProperty("certificateWithPrivateKey").GetString();

        var dataToSign = "important-data-to-verify";

        var signRequest = new
        {
            data = dataToSign,
            certificateWithPrivateKey = certificateWithPrivateKey,
            password = "test123"
        };

        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);
        using var signDoc = await signResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var signature = signDoc!.RootElement.GetProperty("signature").GetString();

        // Verify signature
        var verifyRequest = new
        {
            data = dataToSign,
            signature = signature,
            certificate = certificate
        };

        // Act
        var verifyResponse = await _client.PostAsJsonAsync("/api/testing/verify-signature", verifyRequest);

        // Assert
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyDoc = await verifyResponse.Content.ReadFromJsonAsync<JsonDocument>();
        verifyDoc.Should().NotBeNull();
        verifyDoc!.RootElement.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignature_WithTamperedData_ReturnsFalse()
    {
        // Arrange - Generate certificate and sign data
        var certRequest = new
        {
            subjectName = "tamper-test-node",
            validityYears = 1,
            password = "test123"
        };

        var certResponse = await _client.PostAsJsonAsync("/api/testing/generate-certificate", certRequest);
        using var certDoc = await certResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var certificate = certDoc!.RootElement.GetProperty("certificate").GetString();
        var certificateWithPrivateKey = certDoc.RootElement.GetProperty("certificateWithPrivateKey").GetString();

        var originalData = "original-data";

        var signRequest = new
        {
            data = originalData,
            certificateWithPrivateKey = certificateWithPrivateKey,
            password = "test123"
        };

        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);
        using var signDoc = await signResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var signature = signDoc!.RootElement.GetProperty("signature").GetString();

        // Verify with tampered data
        var verifyRequest = new
        {
            data = "tampered-data", // Different from original
            signature = signature,
            certificate = certificate
        };

        // Act
        var verifyResponse = await _client.PostAsJsonAsync("/api/testing/verify-signature", verifyRequest);

        // Assert
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyDoc = await verifyResponse.Content.ReadFromJsonAsync<JsonDocument>();
        verifyDoc.Should().NotBeNull();
        verifyDoc!.RootElement.GetProperty("isValid").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task VerifySignature_WithWrongCertificate_ReturnsFalse()
    {
        // Arrange - Generate two certificates
        var cert1Request = new
        {
            subjectName = "node-1",
            validityYears = 1,
            password = "test123"
        };

        var cert1Response = await _client.PostAsJsonAsync("/api/testing/generate-certificate", cert1Request);
        using var cert1Doc = await cert1Response.Content.ReadFromJsonAsync<JsonDocument>();
        var certificateWithPrivateKey = cert1Doc!.RootElement.GetProperty("certificateWithPrivateKey").GetString();

        var cert2Request = new
        {
            subjectName = "node-2",
            validityYears = 1,
            password = "test123"
        };

        var cert2Response = await _client.PostAsJsonAsync("/api/testing/generate-certificate", cert2Request);
        using var cert2Doc = await cert2Response.Content.ReadFromJsonAsync<JsonDocument>();
        var differentCertificate = cert2Doc!.RootElement.GetProperty("certificate").GetString();

        // Sign with cert1
        var dataToSign = "test-data";
        var signRequest = new
        {
            data = dataToSign,
            certificateWithPrivateKey = certificateWithPrivateKey,
            password = "test123"
        };

        var signResponse = await _client.PostAsJsonAsync("/api/testing/sign-data", signRequest);
        using var signDoc = await signResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var signature = signDoc!.RootElement.GetProperty("signature").GetString();

        // Verify with cert2 (wrong certificate)
        var verifyRequest = new
        {
            data = dataToSign,
            signature = signature,
            certificate = differentCertificate
        };

        // Act
        var verifyResponse = await _client.PostAsJsonAsync("/api/testing/verify-signature", verifyRequest);

        // Assert
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyDoc = await verifyResponse.Content.ReadFromJsonAsync<JsonDocument>();
        verifyDoc.Should().NotBeNull();
        verifyDoc!.RootElement.GetProperty("isValid").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region Node Identity Generation Tests

    [Fact]
    public async Task GenerateNodeIdentity_WithValidRequest_ReturnsCompleteIdentity()
    {
        // Arrange
        var request = new
        {
            nodeId = "complete-identity-node",
            nodeName = "Complete Identity Test Node",
            channelId = "test-channel-123",
            validityYears = 2,
            password = "test123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testing/generate-node-identity", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        jsonDoc.Should().NotBeNull();
        jsonDoc!.RootElement.GetProperty("nodeId").GetString().Should().Be("complete-identity-node");
        jsonDoc.RootElement.GetProperty("certificate").GetString().Should().NotBeNullOrEmpty();
        jsonDoc.RootElement.GetProperty("certificateWithPrivateKey").GetString().Should().NotBeNullOrEmpty();

        // Verify identification request is ready to use
        var identRequest = jsonDoc.RootElement.GetProperty("identificationRequest");
        identRequest.GetProperty("channelId").GetString().Should().Be("test-channel-123");
        identRequest.GetProperty("nodeId").GetString().Should().Be("complete-identity-node");
        identRequest.GetProperty("signature").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateNodeIdentity_SignatureIsValid_CanBeVerified()
    {
        // Arrange - Generate node identity
        var identityRequest = new
        {
            nodeId = "signature-valid-node",
            nodeName = "Signature Valid Test Node",
            channelId = "test-channel-456",
            validityYears = 1,
            password = "test123"
        };

        var identityResponse = await _client.PostAsJsonAsync("/api/testing/generate-node-identity", identityRequest);
        using var identityDoc = await identityResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var certificate = identityDoc!.RootElement.GetProperty("certificate").GetString();
        var identRequest = identityDoc.RootElement.GetProperty("identificationRequest");
        var signature = identRequest.GetProperty("signature").GetString();
        var timestamp = identRequest.GetProperty("timestamp").GetString();

        // Reconstruct signed data
        var signedData = $"test-channel-456signature-valid-node{timestamp}";

        // Verify signature
        var verifyRequest = new
        {
            data = signedData,
            signature = signature,
            certificate = certificate
        };

        // Act
        var verifyResponse = await _client.PostAsJsonAsync("/api/testing/verify-signature", verifyRequest);

        // Assert
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyDoc = await verifyResponse.Content.ReadFromJsonAsync<JsonDocument>();
        verifyDoc!.RootElement.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Certificate Helper Direct Tests

    [Fact]
    public void CertificateHelper_GenerateCertificate_ProducesValidCertificate()
    {
        // Act
        var cert = CertificateHelper.GenerateSelfSignedCertificate(
            "direct-test-node",
            1);

        // Assert
        cert.Should().NotBeNull();
        cert.Subject.Should().Contain("direct-test-node");
        cert.HasPrivateKey.Should().BeTrue();
        // Note: X509Certificate2.NotBefore returns local time, not UTC
        cert.NotBefore.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(5));
        cert.NotAfter.Should().BeCloseTo(DateTime.Now.AddYears(1), TimeSpan.FromDays(1));
    }

    [Fact]
    public void CertificateHelper_ExportAndImport_PreservesData()
    {
        // Arrange
        var originalCert = CertificateHelper.GenerateSelfSignedCertificate(
            "export-test-node",
            1);

        var password = "test123";

        // Act - Export with private key
        var pfxBase64 = CertificateHelper.ExportCertificateWithPrivateKeyToBase64(originalCert, password);

        // Import back
        var importedCert = CertificateHelper.LoadCertificateWithPrivateKeyFromBase64(pfxBase64, password);

        // Assert
        importedCert.Should().NotBeNull();
        importedCert.Thumbprint.Should().Be(originalCert.Thumbprint);
        importedCert.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public void CertificateHelper_SignAndVerify_WorksCorrectly()
    {
        // Arrange
        var cert = CertificateHelper.GenerateSelfSignedCertificate(
            "sign-verify-node",
            1);

        var data = "test-data-for-signing";

        // Act
        var signature = CertificateHelper.SignData(data, cert);
        var isValid = CertificateHelper.VerifySignature(data, signature, cert);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }

    [Fact]
    public void CertificateHelper_VerifySignature_WithTamperedData_ReturnsFalse()
    {
        // Arrange
        var cert = CertificateHelper.GenerateSelfSignedCertificate(
            "tamper-test",
            1);

        var originalData = "original-data";
        var signature = CertificateHelper.SignData(originalData, cert);

        // Act - Verify with tampered data
        var isValid = CertificateHelper.VerifySignature("tampered-data", signature, cert);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion
}

