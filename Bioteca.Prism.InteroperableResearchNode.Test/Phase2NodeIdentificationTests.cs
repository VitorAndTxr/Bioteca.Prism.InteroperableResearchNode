using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Node;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for Phase 2: Node Identification and Registration (WITH ENCRYPTION)
/// </summary>
public class Phase2NodeIdentificationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public Phase2NodeIdentificationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterNode_WithEncryptedPayload_ReturnsSuccess()
    {
        // Arrange - Establish channel first
        var (channelId, symmetricKey) = await EstablishChannelAsync();

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Generate certificate for test node
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate("test-node-001", 1);
        var certificate = Service.Services.Node.CertificateHelper.ExportCertificateToBase64(cert);

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "test-node-001",
            NodeName = "Test Research Node",
            Certificate = certificate,
            ContactInfo = "admin@testnode.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://testnode:8080",
            RequestedCapabilities = new List<string> { "search", "retrieve" }
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        encryptedResponse.Should().NotBeNull();

        var registrationResponse = encryptionService.DecryptPayload<NodeRegistrationResponse>(
            encryptedResponse!, symmetricKey);

        registrationResponse.Should().NotBeNull();
        registrationResponse.Success.Should().BeTrue();
        registrationResponse.Status.Should().Be(AuthorizationStatus.Pending);
    }

    [Fact]
    public async Task IdentifyNode_UnknownNode_ReturnsNotKnown()
    {
        // Arrange - Establish channel
        var (channelId, symmetricKey) = await EstablishChannelAsync();

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Generate certificate and signature
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate("unknown-node-999", 1);
        var certificate = Service.Services.Node.CertificateHelper.ExportCertificateToBase64(cert);
        var (signature, timestamp, signedData) = SignData(channelId, "unknown-node-999", cert);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "unknown-node-999",
            Certificate = certificate,
            Signature = signature,
            Timestamp = timestamp
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var statusResponse = encryptionService.DecryptPayload<NodeStatusResponse>(
            encryptedResponse!, symmetricKey);

        statusResponse.IsKnown.Should().BeFalse();
        statusResponse.Status.Should().Be(AuthorizationStatus.Unknown);
        statusResponse.RegistrationUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IdentifyNode_PendingNode_ReturnsPending()
    {
        // Arrange - Establish channel and register node
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = await RegisterTestNodeAsync(channelId, symmetricKey, "pending-node-001");

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Generate signature
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate(nodeId, 1);
        var certificate = Service.Services.Node.CertificateHelper.ExportCertificateToBase64(cert);
        var (signature, timestamp, signedData) = SignData(channelId, nodeId, cert);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = nodeId,
            Certificate = certificate,
            Signature = signature,
            Timestamp = timestamp
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var statusResponse = encryptionService.DecryptPayload<NodeStatusResponse>(
            encryptedResponse!, symmetricKey);

        statusResponse.IsKnown.Should().BeTrue();
        statusResponse.Status.Should().Be(AuthorizationStatus.Pending);
        statusResponse.NextPhase.Should().BeNull();
    }

    [Fact]
    public async Task IdentifyNode_AuthorizedNode_ReturnsAuthorizedWithPhase3()
    {
        // Arrange - Establish channel, register, and authorize node
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = await RegisterTestNodeAsync(channelId, symmetricKey, "authorized-node-001");
        await AuthorizeNodeAsync(nodeId);

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Generate signature
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate(nodeId, 1);
        var certificate = Service.Services.Node.CertificateHelper.ExportCertificateToBase64(cert);
        var (signature, timestamp, signedData) = SignData(channelId, nodeId, cert);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = nodeId,
            Certificate = certificate,
            Signature = signature,
            Timestamp = timestamp
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var statusResponse = encryptionService.DecryptPayload<NodeStatusResponse>(
            encryptedResponse!, symmetricKey);

        statusResponse.IsKnown.Should().BeTrue();
        statusResponse.Status.Should().Be(AuthorizationStatus.Authorized);
        statusResponse.NextPhase.Should().Be("phase3_authenticate");
    }

    [Fact]
    public async Task IdentifyNode_WithoutChannelHeader_ReturnsBadRequest()
    {
        // Arrange
        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "dummy-cert",
            Signature = "dummy-signature"
        };

        // Act (no X-Channel-Id header)
        var response = await _client.PostAsJsonAsync("/api/node/identify", identifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllNodes_ReturnsRegisteredNodes()
    {
        // Arrange - Register a few nodes
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        await RegisterTestNodeAsync(channelId, symmetricKey, "list-test-node-1");
        await RegisterTestNodeAsync(channelId, symmetricKey, "list-test-node-2");

        // Act
        var response = await _client.GetAsync("/api/node/nodes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("list-test-node-1");
        content.Should().Contain("list-test-node-2");
    }

    [Fact]
    public async Task UpdateNodeStatus_AuthorizesNode_ReturnsSuccess()
    {
        // Arrange - Register node
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = await RegisterTestNodeAsync(channelId, symmetricKey, "status-test-node");

        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

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

        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate(nodeId, 1);
        var certificate = Service.Services.Node.CertificateHelper.ExportCertificateToBase64(cert);

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

    private async Task AuthorizeNodeAsync(string nodeId)
    {
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);
    }


    private (string signature, DateTime timestamp, string signedData) SignData(string channelId, string nodeId, X509Certificate2 certificate)
    {
        // Generate real signature using the provided certificate
        var timestamp = DateTime.UtcNow;
        var signedData = $"{channelId}{nodeId}{timestamp:O}";
        var signature = Service.Services.Node.CertificateHelper.SignData(signedData, certificate);

        return (signature, timestamp, signedData);
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
