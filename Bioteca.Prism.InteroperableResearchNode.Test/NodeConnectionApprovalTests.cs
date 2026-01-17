using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Security.Certificate;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using FluentAssertions;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for Node Connection Approval/Rejection endpoints
/// </summary>
public class NodeConnectionApprovalTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public NodeConnectionApprovalTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region ApproveConnection Tests

    [Fact]
    public async Task ApproveConnection_PendingConnection_ReturnsOkAndSetsStatusToAuthorized()
    {
        // Arrange - Establish channel, register a pending node, then create an admin session
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = $"approve-test-node-{Guid.NewGuid():N}";
        var (registrationId, cert) = await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);

        // Create an admin session to perform the approval
        var (adminChannelId, adminSymmetricKey, adminSessionToken) = await SetupAuthenticatedSessionAsync();

        // Act - Approve the pending connection
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", adminChannelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", adminSessionToken);

        var response = await _client.PostAsync($"/api/node/{registrationId}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the status was updated to Authorized
        var nodeRegistry = _factory.Services.GetRequiredService<IResearchNodeService>();
        var node = await nodeRegistry.GetNodeAsync(Guid.Parse(registrationId));
        node.Should().NotBeNull();
        node!.Status.Should().Be(AuthorizationStatus.Authorized);
    }

    [Fact]
    public async Task ApproveConnection_NonExistentConnection_ReturnsNotFound()
    {
        // Arrange - Establish channel and authenticate with any valid node
        var (channelId, symmetricKey, sessionToken) = await SetupAuthenticatedSessionAsync();

        var nonExistentId = Guid.NewGuid();

        // Act
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        var response = await _client.PostAsync($"/api/node/{nonExistentId}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Connection not found");
    }

    [Fact]
    public async Task ApproveConnection_AlreadyAuthorizedConnection_ReturnsBadRequest()
    {
        // Arrange - Create an admin session
        var (channelId, symmetricKey, sessionToken) = await SetupAuthenticatedSessionAsync();

        // Create and authorize a node
        var nodeId = $"already-authorized-{Guid.NewGuid():N}";
        var (registrationId, cert) = await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);
        await AuthorizeNodeAsync(registrationId);

        // Act - Try to approve again
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        var response = await _client.PostAsync($"/api/node/{registrationId}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Connection is not in pending status");
    }

    #endregion

    #region RejectConnection Tests

    [Fact]
    public async Task RejectConnection_PendingConnection_ReturnsOkAndSetsStatusToRevoked()
    {
        // Arrange - Establish channel, register node (which creates a pending connection)
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = $"reject-test-node-{Guid.NewGuid():N}";
        var (registrationId, cert) = await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);

        // Create an admin session to perform the rejection
        var (adminChannelId, adminSymmetricKey, adminSessionToken) = await SetupAuthenticatedSessionAsync();

        // Act - Reject the connection
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", adminChannelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", adminSessionToken);

        var response = await _client.PostAsync($"/api/node/{registrationId}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the status was updated to Revoked
        var nodeRegistry = _factory.Services.GetRequiredService<IResearchNodeService>();
        var node = await nodeRegistry.GetNodeAsync(Guid.Parse(registrationId));
        node.Should().NotBeNull();
        node!.Status.Should().Be(AuthorizationStatus.Revoked);
    }

    [Fact]
    public async Task RejectConnection_NonExistentConnection_ReturnsNotFound()
    {
        // Arrange
        var (channelId, symmetricKey, sessionToken) = await SetupAuthenticatedSessionAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        var response = await _client.PostAsync($"/api/node/{nonExistentId}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Connection not found");
    }

    [Fact]
    public async Task RejectConnection_AlreadyRevokedConnection_ReturnsBadRequest()
    {
        // Arrange - Create an admin session
        var (channelId, symmetricKey, sessionToken) = await SetupAuthenticatedSessionAsync();

        // Create and revoke a connection
        var nodeId = $"already-revoked-{Guid.NewGuid():N}";
        var (registrationId, cert) = await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);
        await RevokeNodeAsync(registrationId);

        // Act - Try to reject again
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        _client.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        var response = await _client.PostAsync($"/api/node/{registrationId}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Connection is not in pending status");
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

        _client.DefaultRequestHeaders.Clear();
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

    private async Task<(string registrationId, X509Certificate2 certificate)> RegisterTestNodeAsync(string channelId, byte[] symmetricKey, string nodeId)
    {
        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var cert = CertificateHelper.GenerateSelfSignedCertificate(nodeId, 1);
        var certificate = CertificateHelper.ExportCertificateToBase64(cert);

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = nodeId,
            NodeName = $"Test Node {nodeId}",
            Certificate = certificate,
            ContactInfo = $"admin@{nodeId}.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = $"http://{nodeId}:8080",
            RequestedNodeAccessLevel = NodeAccessTypeEnum.ReadWrite
        };

        var encryptedPayload = encryptionService.EncryptPayload(registrationRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var registrationResponse = encryptionService.DecryptPayload<NodeRegistrationResponse>(
            encryptedResponse!, symmetricKey);

        return (registrationResponse.RegistrationId, cert);
    }

    private async Task AuthorizeNodeAsync(string nodeId)
    {
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        _client.DefaultRequestHeaders.Clear();
        await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);
    }

    private async Task RevokeNodeAsync(string nodeId)
    {
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Revoked
        };

        _client.DefaultRequestHeaders.Clear();
        await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);
    }

    private async Task<(string channelId, byte[] symmetricKey, string sessionToken)> SetupAuthenticatedSessionAsync()
    {
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = $"admin-node-{Guid.NewGuid():N}";
        var (registrationId, cert) = await RegisterTestNodeAsync(channelId, symmetricKey, nodeId);

        // Authorize the node first
        await AuthorizeNodeAsync(registrationId);

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Identify the node
        var certificate = CertificateHelper.ExportCertificateToBase64(cert);
        var (signature, timestamp, _) = SignData(channelId, nodeId, cert);

        var identifyRequest = new NodeIdentifyRequest
        {
            NodeId = nodeId,
            Certificate = certificate,
            Signature = signature,
            Timestamp = timestamp
        };

        var encryptedPayload = encryptionService.EncryptPayload(identifyRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        await _client.PostAsJsonAsync("/api/node/identify", encryptedPayload);

        // Request challenge
        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };
        encryptedPayload = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var challengeResponse = await _client.PostAsJsonAsync("/api/node/challenge", encryptedPayload);
        var encryptedChallengeResponse = await challengeResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var challenge = encryptionService.DecryptPayload<ChallengeResponse>(encryptedChallengeResponse!, symmetricKey);

        // Sign the challenge and respond
        var authTimestamp = DateTime.UtcNow;
        var signedData = $"{challenge.ChallengeData}{channelId}{nodeId}{authTimestamp:O}";
        var challengeSignature = CertificateHelper.SignData(signedData, cert);

        var authRequest = new ChallengeResponseRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            ChallengeData = challenge.ChallengeData,
            Signature = challengeSignature,
            Timestamp = authTimestamp
        };

        encryptedPayload = encryptionService.EncryptPayload(authRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var authResponse = await _client.PostAsJsonAsync("/api/node/authenticate", encryptedPayload);
        var encryptedAuthResponse = await authResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var authResult = encryptionService.DecryptPayload<AuthenticationResponse>(encryptedAuthResponse!, symmetricKey);

        return (channelId, symmetricKey, authResult.SessionToken!);
    }

    private (string signature, DateTime timestamp, string signedData) SignData(string channelId, string nodeId, X509Certificate2 certificate)
    {
        var timestamp = DateTime.UtcNow;
        var signedData = $"{channelId}{nodeId}{timestamp:O}";
        var signature = CertificateHelper.SignData(signedData, certificate);

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
