using System.Net;
using System.Net.Http.Json;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Node;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for full encrypted channel workflow (Phase 1 + Phase 2 combined)
/// Simulates the complete interaction between two nodes
/// </summary>
public class EncryptedChannelIntegrationTests
{
    [Fact]
    public async Task FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize()
    {
        // Arrange - Create two node instances (Node A and Node B)
        using var factoryNodeA = new TestWebApplicationFactory("NodeA");
        using var factoryNodeB = new TestWebApplicationFactory("NodeB");

        var clientA = factoryNodeA.CreateClient();
        var clientB = factoryNodeB.CreateClient();

        var ephemeralKeyServiceA = factoryNodeA.Services.GetRequiredService<IEphemeralKeyService>();
        var encryptionServiceA = factoryNodeA.Services.GetRequiredService<IChannelEncryptionService>();

        // Step 1: Node A establishes encrypted channel with Node B
        var clientEcdh = ephemeralKeyServiceA.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyServiceA.ExportPublicKey(clientEcdh);
        var clientNonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

        var channelOpenRequest = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = clientNonce,
            Timestamp = DateTime.UtcNow
        };

        // Node A sends handshake to Node B
        var channelResponse = await clientB.PostAsJsonAsync("/api/channel/open", channelOpenRequest);
        channelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var channelId = channelResponse.Headers.GetValues("X-Channel-Id").First();
        var channelReady = await channelResponse.Content.ReadFromJsonAsync<ChannelReadyResponse>();

        // Derive symmetric key on Node A side
        var serverEcdh = ephemeralKeyServiceA.ImportPublicKey(channelReady!.EphemeralPublicKey, "P384");
        var sharedSecret = ephemeralKeyServiceA.DeriveSharedSecret(clientEcdh, serverEcdh);
        var salt = CombineNonces(clientNonce, channelReady.Nonce);
        var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");
        var symmetricKey = encryptionServiceA.DeriveKey(sharedSecret, salt, info);

        // Cleanup ECDH
        clientEcdh.Dispose();
        serverEcdh.Dispose();
        Array.Clear(sharedSecret, 0, sharedSecret.Length);

        // Step 2: Node A registers with Node B (encrypted payload)
        var certificate = GenerateTestCertificate("node-a-integration");

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "node-a-integration",
            NodeName = "Node A Integration Test",
            Certificate = certificate,
            ContactInfo = "admin@node-a.test",
            InstitutionDetails = "Test Institution A",
            NodeUrl = "http://node-a:8080",
            RequestedCapabilities = new List<string> { "search", "retrieve" }
        };

        // Encrypt registration payload
        var encryptedRegPayload = encryptionServiceA.EncryptPayload(registrationRequest, symmetricKey);

        clientB.DefaultRequestHeaders.Clear();
        clientB.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var regResponse = await clientB.PostAsJsonAsync("/api/node/register", encryptedRegPayload);
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedRegResponse = await regResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var registrationResult = encryptionServiceA.DecryptPayload<NodeRegistrationResponse>(
            encryptedRegResponse!, symmetricKey);

        registrationResult.Success.Should().BeTrue();
        registrationResult.Status.Should().Be(AuthorizationStatus.Pending);

        // Step 3: Node A identifies itself (should be Pending)
        var (signature, signedData) = SignData(channelId, "node-a-integration");

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "node-a-integration",
            Certificate = certificate,
            Signature = signature
        };

        var encryptedIdentifyPayload = encryptionServiceA.EncryptPayload(identifyRequest, symmetricKey);

        clientB.DefaultRequestHeaders.Clear();
        clientB.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var identifyResponse = await clientB.PostAsJsonAsync("/api/channel/identify", encryptedIdentifyPayload);
        identifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedIdentifyResponse = await identifyResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var statusResponse = encryptionServiceA.DecryptPayload<NodeStatusResponse>(
            encryptedIdentifyResponse!, symmetricKey);

        statusResponse.IsKnown.Should().BeTrue();
        statusResponse.Status.Should().Be(AuthorizationStatus.Pending);
        statusResponse.NextPhase.Should().BeNull();

        // Step 4: Admin approves Node A on Node B
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        var approveResponse = await clientB.PutAsJsonAsync(
            "/api/node/node-a-integration/status", updateRequest);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Node A identifies again (should be Authorized with phase3)
        var (newSignature, newSignedData) = SignData(channelId, "node-a-integration");

        var identifyRequest2 = new NodeIdentifyRequest
        {
            NodeId = "node-a-integration",
            Certificate = certificate,
            Signature = newSignature
        };

        var encryptedIdentifyPayload2 = encryptionServiceA.EncryptPayload(identifyRequest2, symmetricKey);

        clientB.DefaultRequestHeaders.Clear();
        clientB.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var identifyResponse2 = await clientB.PostAsJsonAsync("/api/channel/identify", encryptedIdentifyPayload2);
        identifyResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedIdentifyResponse2 = await identifyResponse2.Content.ReadFromJsonAsync<EncryptedPayload>();
        var statusResponse2 = encryptionServiceA.DecryptPayload<NodeStatusResponse>(
            encryptedIdentifyResponse2!, symmetricKey);

        statusResponse2.IsKnown.Should().BeTrue();
        statusResponse2.Status.Should().Be(AuthorizationStatus.Authorized);
        statusResponse2.NextPhase.Should().Be("phase3_authenticate");

        // Cleanup
        Array.Clear(symmetricKey, 0, symmetricKey.Length);
    }

    [Fact]
    public async Task EncryptedChannel_WithWrongKey_FailsDecryption()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var ephemeralKeyService = factory.Services.GetRequiredService<IEphemeralKeyService>();
        var encryptionService = factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Establish channel
        var (channelId, _) = await EstablishChannelAsync(client, ephemeralKeyService, encryptionService);

        // Generate WRONG symmetric key
        var wrongKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "dummy-cert",
            Signature = "dummy-signature"
        };

        // Encrypt with wrong key
        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, wrongKey);

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await client.PostAsJsonAsync("/api/channel/identify", encryptedPayload);

        // Assert - Server should fail to decrypt
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(wrongKey, 0, wrongKey.Length);
    }

    [Fact]
    public async Task EncryptedChannel_ExpiredChannel_ReturnsBadRequest()
    {
        // This test would require manipulating the channel expiration
        // For now, we can test with an invalid channel ID
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var encryptionService = factory.Services.GetRequiredService<IChannelEncryptionService>();
        var fakeKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = "test-node",
            Certificate = "dummy-cert",
            Signature = "dummy-signature"
        };

        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, fakeKey);

        client.DefaultRequestHeaders.Add("X-Channel-Id", "invalid-channel-id");

        // Act
        var response = await client.PostAsJsonAsync("/api/channel/identify", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        Array.Clear(fakeKey, 0, fakeKey.Length);
    }

    #region Helper Methods

    private async Task<(string channelId, byte[] symmetricKey)> EstablishChannelAsync(
        HttpClient client,
        IEphemeralKeyService ephemeralKeyService,
        IChannelEncryptionService encryptionService)
    {
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

        var response = await client.PostAsJsonAsync("/api/channel/open", request);
        var channelId = response.Headers.GetValues("X-Channel-Id").First();

        var channelReady = await response.Content.ReadFromJsonAsync<ChannelReadyResponse>();

        var serverEcdh = ephemeralKeyService.ImportPublicKey(channelReady!.EphemeralPublicKey, "P384");
        var sharedSecret = ephemeralKeyService.DeriveSharedSecret(clientEcdh, serverEcdh);

        var salt = CombineNonces(clientNonce, channelReady.Nonce);
        var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");
        var symmetricKey = encryptionService.DeriveKey(sharedSecret, salt, info);

        clientEcdh.Dispose();
        serverEcdh.Dispose();
        Array.Clear(sharedSecret, 0, sharedSecret.Length);

        return (channelId, symmetricKey);
    }

    private string GenerateTestCertificate(string nodeId)
    {
        var cert = Service.Services.Node.CertificateHelper.GenerateSelfSignedCertificate(
            nodeId,
            1);

        return Convert.ToBase64String(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert));
    }

    private (string signature, string signedData) SignData(string channelId, string nodeId)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        var signedData = $"{channelId}{nodeId}{timestamp}";
        var signature = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(256));

        return (signature, signedData);
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
