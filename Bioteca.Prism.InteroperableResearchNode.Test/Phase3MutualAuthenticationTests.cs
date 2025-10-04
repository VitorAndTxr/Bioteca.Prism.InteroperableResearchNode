using Bioteca.Prism.Core.Security.Certificate;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using FluentAssertions;
using System.Net;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for Phase 3: Mutual Authentication (WITH ENCRYPTION)
/// </summary>
public class Phase3MutualAuthenticationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public Phase3MutualAuthenticationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RequestChallenge_AuthorizedNode_ReturnsChallenge()
    {
        // Arrange - Establish channel, register, and authorize node
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var cert = await RegisterAndAuthorizeNodeAsync(channelId, symmetricKey, "challenge-node-001");
        var nodeId = "challenge-node-001";

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/challenge", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        encryptedResponse.Should().NotBeNull();

        var challengeResponse = encryptionService.DecryptPayload<ChallengeResponse>(
            encryptedResponse!, symmetricKey);

        challengeResponse.Should().NotBeNull();
        challengeResponse.ChallengeData.Should().NotBeNullOrEmpty();
        challengeResponse.ChallengeTtlSeconds.Should().BeGreaterThan(0);
        challengeResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestChallenge_UnauthorizedNode_ReturnsBadRequest()
    {
        // Arrange - Establish channel and register node (but don't authorize)
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = await RegisterTestNodeAsync(channelId, symmetricKey, "unauthorized-challenge-node");

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        // Encrypt payload
        var encryptedPayload = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        // Add channel header
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/challenge", encryptedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authenticate_WithValidChallengeResponse_ReturnsSessionToken()
    {
        // Arrange - Establish channel, register, authorize, and get challenge
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = "auth-node-001";
        var cert = await RegisterAndAuthorizeNodeAsync(channelId, symmetricKey, nodeId);

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Request challenge
        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        var encryptedChallengeRequest = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var challengeResponse = await _client.PostAsJsonAsync("/api/node/challenge", encryptedChallengeRequest);
        var encryptedChallengeResponse = await challengeResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var challenge = encryptionService.DecryptPayload<ChallengeResponse>(encryptedChallengeResponse!, symmetricKey);

        // Sign challenge
        var timestamp = DateTime.UtcNow;
        var signedData = $"{challenge.ChallengeData}{channelId}{nodeId}{timestamp:O}";
        var signature = CertificateHelper.SignData(signedData, cert);

        var authRequest = new ChallengeResponseRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            ChallengeData = challenge.ChallengeData,
            Signature = signature,
            Timestamp = timestamp
        };

        // Encrypt auth request
        var encryptedAuthRequest = encryptionService.EncryptPayload(authRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/authenticate", encryptedAuthRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var encryptedAuthResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        var authResponse = encryptionService.DecryptPayload<AuthenticationResponse>(
            encryptedAuthResponse!, symmetricKey);

        authResponse.Should().NotBeNull();
        authResponse.Authenticated.Should().BeTrue();
        authResponse.NodeId.Should().Be(nodeId);
        authResponse.SessionToken.Should().NotBeNullOrEmpty();
        authResponse.SessionExpiresAt.Should().NotBeNull();
        authResponse.SessionExpiresAt.Should().BeAfter(DateTime.UtcNow);
        authResponse.GrantedNodeAccessLevel.Should().NotBe(default(NodeAccessTypeEnum));
        authResponse.NextPhase.Should().Be("phase4_session");
    }

    [Fact]
    public async Task Authenticate_WithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange - Establish channel, register, authorize, and get challenge
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = "invalid-sig-auth-node";
        await RegisterAndAuthorizeNodeAsync(channelId, symmetricKey, nodeId);

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Request challenge
        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        var encryptedChallengeRequest = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var challengeResponse = await _client.PostAsJsonAsync("/api/node/challenge", encryptedChallengeRequest);
        var encryptedChallengeResponse = await challengeResponse.Content.ReadFromJsonAsync<EncryptedPayload>();
        var challenge = encryptionService.DecryptPayload<ChallengeResponse>(encryptedChallengeResponse!, symmetricKey);

        // Create auth request with invalid signature
        var authRequest = new ChallengeResponseRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            ChallengeData = challenge.ChallengeData,
            Signature = "invalid-signature-data",
            Timestamp = DateTime.UtcNow
        };

        // Encrypt auth request
        var encryptedAuthRequest = encryptionService.EncryptPayload(authRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/authenticate", encryptedAuthRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authenticate_WithWrongChallengeData_ReturnsBadRequest()
    {
        // Arrange - Establish channel, register, authorize
        var (channelId, symmetricKey) = await EstablishChannelAsync();
        var nodeId = "wrong-challenge-node";
        var cert = await RegisterAndAuthorizeNodeAsync(channelId, symmetricKey, nodeId);

        var encryptionService = _factory.Services.GetRequiredService<IChannelEncryptionService>();

        // Request challenge (but we'll use different challenge data)
        var challengeRequest = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        var encryptedChallengeRequest = encryptionService.EncryptPayload(challengeRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        await _client.PostAsJsonAsync("/api/node/challenge", encryptedChallengeRequest);

        // Use wrong challenge data
        var wrongChallengeData = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var timestamp = DateTime.UtcNow;
        var signedData = $"{wrongChallengeData}{channelId}{nodeId}{timestamp:O}";
        var signature = CertificateHelper.SignData(signedData, cert);

        var authRequest = new ChallengeResponseRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            ChallengeData = wrongChallengeData,
            Signature = signature,
            Timestamp = timestamp
        };

        // Encrypt auth request
        var encryptedAuthRequest = encryptionService.EncryptPayload(authRequest, symmetricKey);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/node/authenticate", encryptedAuthRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        return nodeId;
    }

    private async Task<System.Security.Cryptography.X509Certificates.X509Certificate2> RegisterAndAuthorizeNodeAsync(
        string channelId, byte[] symmetricKey, string nodeId)
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

        await _client.PostAsJsonAsync("/api/node/register", encryptedPayload);

        // Authorize node
        var updateRequest = new UpdateNodeStatusRequest
        {
            Status = AuthorizationStatus.Authorized
        };

        await _client.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);

        return cert;
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
