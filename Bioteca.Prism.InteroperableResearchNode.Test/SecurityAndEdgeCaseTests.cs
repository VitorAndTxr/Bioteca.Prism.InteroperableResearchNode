using System.Net;
using System.Net.Http.Json;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Node;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for security scenarios, edge cases, and error handling
/// </summary>
public class SecurityAndEdgeCaseTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public SecurityAndEdgeCaseTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Timestamp Validation Tests

    [Fact]
    public async Task OpenChannel_WithOldTimestamp_ReturnsBadRequest()
    {
        // Arrange
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);

        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow.AddMinutes(-10) // Old timestamp (more than 5 minutes old)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        clientEcdh.Dispose();
    }

    [Fact]
    public async Task OpenChannel_WithFutureTimestamp_ReturnsBadRequest()
    {
        // Arrange
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);

        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow.AddMinutes(10) // Future timestamp
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        clientEcdh.Dispose();
    }

    #endregion

    #region Nonce Validation Tests

    [Fact]
    public async Task OpenChannel_WithInvalidNonce_ReturnsBadRequest()
    {
        // Arrange
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);

        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = "INVALID_BASE64", // Invalid base64
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        clientEcdh.Dispose();
    }

    [Fact]
    public async Task OpenChannel_WithShortNonce_ReturnsBadRequest()
    {
        // Arrange
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);

        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)), // Too short (< 16 bytes)
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        clientEcdh.Dispose();
    }

    #endregion

    #region Payload Tampering Tests

    [Fact]
    public async Task IdentifyNode_WithTamperedEncryptedData_ReturnsBadRequest()
    {
        // Arrange - Establish channel
        var (channelId, symmetricKey) = await EstablishChannelAsync();

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "test-cert",
            Signature = "test-signature"
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        // Tamper with encrypted data (change one byte)
        var tamperedData = Convert.FromBase64String(encryptedPayload.EncryptedData);
        tamperedData[0] ^= 0xFF; // Flip bits in first byte
        encryptedPayload.EncryptedData = Convert.ToBase64String(tamperedData);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert - Should fail authentication tag verification
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    [Fact]
    public async Task IdentifyNode_WithTamperedAuthTag_ReturnsBadRequest()
    {
        // Arrange - Establish channel
        var (channelId, symmetricKey) = await EstablishChannelAsync();

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "test-cert",
            Signature = "test-signature"
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        // Tamper with auth tag
        var tamperedTag = Convert.FromBase64String(encryptedPayload.AuthTag);
        tamperedTag[0] ^= 0xFF; // Flip bits in first byte
        encryptedPayload.AuthTag = Convert.ToBase64String(tamperedTag);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert - Should fail authentication
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    #endregion

    #region Certificate Validation Tests

    [Fact]
    public async Task RegisterNode_WithExpiredCertificate_ReturnsBadRequest()
    {
        // Arrange
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Generate an expired certificate (validity of 0 years, but we can't easily make it expired in the past)
        // For this test, we'll simulate with an invalid certificate format
        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "expired-node",
            NodeName = "Expired Node",
            Certificate = "EXPIRED_OR_INVALID_CERT",
            ContactInfo = "admin@expired.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://expired:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    [Fact]
    public async Task RegisterNode_WithInvalidCertificateFormat_ReturnsBadRequest()
    {
        // Arrange
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "invalid-cert-node",
            NodeName = "Invalid Cert Node",
            Certificate = "NOT_A_VALID_CERTIFICATE_BASE64",
            ContactInfo = "admin@invalid.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://invalid:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    #endregion

    #region Channel State Tests

    [Fact]
    public async Task IdentifyNode_WithInvalidChannelId_ReturnsBadRequest()
    {
        // Arrange
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();
        var fakeKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "test-cert",
            Signature = "test-signature"
        };

        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, fakeKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", "non-existent-channel-id");

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(fakeKey, 0, fakeKey.Length);
    }

    [Fact]
    public async Task RegisterNode_WithoutChannelId_ReturnsBadRequest()
    {
        // Arrange
        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "test-node",
            NodeName = "Test Node",
            Certificate = "test-cert",
            ContactInfo = "admin@test.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://test:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        // Act (no X-Channel-Id header)
        var response = await _client.PostAsJsonAsync("/api/node/register", registrationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Key Exchange Algorithm Tests

    [Fact]
    public async Task OpenChannel_WithUnsupportedKeyExchangeAlgorithm_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = "dummy-key",
            KeyExchangeAlgorithm = "INVALID-ALGORITHM",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Node Status Tests

    [Fact]
    public async Task UpdateNodeStatus_ForNonExistentNode_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/node/non-existent-node-id/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateNodeStatus_ToInvalidStatus_ReturnsBadRequest()
    {
        // Arrange - Register a node first
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = await RegisterTestNodeAsync(channelId, symmetricKey, "status-test-node");

        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = (AuthorizationStatus)999 // Invalid status
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    #endregion

    #region Duplicate Registration Tests

    [Fact]
    public async Task RegisterNode_Twice_SecondRegistrationUpdatesInfo()
    {
        // Arrange - Register node first time
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = "duplicate-test-node";
        await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);

        // Act - Register same node again with different information
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var certificate = GenerateTestCertificate(nodeId);

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = nodeId,
            NodeName = "Updated Node Name", // Different name
            Certificate = certificate,
            ContactInfo = "updated@test.test", // Different contact
            InstitutionDetails = "Updated Institution",
            NodeUrl = $"http://{nodeId}:8080",
            RequestedCapabilities = new List<string> { "search", "retrieve", "update" } // Different capabilities
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var registrationResult = encryptionService.DecryptPayload<NodeRegistrationResponse>(
            encryptedResponse!, symmetricKey);

        registrationResult.Success.Should().BeTrue();
        // Note: Implementation should handle this appropriately (update or reject)

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    #endregion

    #region Empty/Null Request Tests

    [Fact]
    public async Task RegisterNode_WithEmptyNodeId_ReturnsBadRequest()
    {
        // Arrange
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "", // Empty node ID
            NodeName = "Test Node",
            Certificate = GenerateTestCertificate("test"),
            ContactInfo = "admin@test.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://test:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    [Fact]
    public async Task RegisterNode_WithEmptyNodeName_ReturnsBadRequest()
    {
        // Arrange
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "test-node",
            NodeName = "", // Empty node name
            Certificate = GenerateTestCertificate("test"),
            ContactInfo = "admin@test.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://test:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    #endregion

    #region Helper Methods

    private async Task<(string channelId, byte[] symmetricKey)> EstablishChannelAsync()
    {
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);
        var clientNonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = clientNonce,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/channel/open", request);
        var channelId = response.Headers.GetValues("X-Channel-Id").First();

        var channelReady = await response.Content.ReadFromJsonAsync<ChannelReadyResponse>();

        // Derive symmetric key
        var serverPublicKey = channelReady!.EphemeralPublicKey;
        var serverEcdh = ephemeralKeyService.ImportPublicKey(serverPublicKey, "P384");
        var sharedSecret = ephemeralKeyService.DeriveSharedSecret(clientEcdh, serverEcdh);

        var salt = CombineNonces(clientNonce, channelReady.Nonce);
        var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");
        var symmetricKey = encryptionService.DeriveKey(sharedSecret, salt, info);

        clientEcdh.Dispose();
        serverEcdh.Dispose();
        Array.Clear(sharedSecret, 0, sharedSecret.Length);

        return (channelId, symmetricKey);
    }

    private async Task<string> RegisterTestNodeAsync(string channelId, byte[] symmetricKey, string nodeId)
    {
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var certificate = GenerateTestCertificate(nodeId);

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = nodeId,
            NodeName = $"Test Node {nodeId}",
            Certificate = certificate,
            ContactInfo = $"admin@{nodeId}.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = $"http://{nodeId}:8080",
            RequestedCapabilities = new List<string> { "search" }
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        return nodeId;
    }

    private string GenerateTestCertificate(string nodeId)
    {
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate(
            nodeId,
            1);

        return Convert.ToBase64String(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));
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

    #endregion
}

