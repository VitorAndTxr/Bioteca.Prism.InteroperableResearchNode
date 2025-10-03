using System.Net;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using FluentAssertions;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for Phase 1: Encrypted Channel Establishment
/// </summary>
public class Phase1ChannelEstablishmentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public Phase1ChannelEstablishmentTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/channel/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task OpenChannel_WithValidRequest_ReturnsChannelReady()
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
            SupportedCiphers = new List<string> { "AES-256-GCM", "ChaCha20-Poly1305" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Channel-Id");

        var channelReady = await response.Content.ReadFromJsonAsync<ChannelReadyResponse>();
        channelReady.Should().NotBeNull();
        channelReady!.ProtocolVersion.Should().Be("1.0");
        channelReady.SelectedCipher.Should().BeOneOf("AES-256-GCM", "ChaCha20-Poly1305");
        channelReady.EphemeralPublicKey.Should().NotBeNullOrEmpty();

        clientEcdh.Dispose();
    }

    [Fact]
    public async Task OpenChannel_WithInvalidProtocolVersion_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "2.0", // Unsupported version
            EphemeralPublicKey = "invalid",
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenChannel_WithInvalidPublicKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = "INVALID_BASE64_KEY",
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenChannel_WithNoCompatibleCipher_ReturnsBadRequest()
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
            SupportedCiphers = new List<string> { "UNSUPPORTED-CIPHER" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/channel/open", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        clientEcdh.Dispose();
    }

    [Fact]
    public async Task GetChannel_WithValidChannelId_ReturnsChannelInfo()
    {
        // Arrange - First open a channel
        var ephemeralKeyService = _factory.Services.GetRequiredService<IEphemeralKeyService>();
        var clientEcdh = ephemeralKeyService.GenerateEphemeralKeyPair("P384");
        var clientPublicKey = ephemeralKeyService.ExportPublicKey(clientEcdh);

        var openRequest = new ChannelOpenRequest
        {
            ProtocolVersion = "1.0",
            EphemeralPublicKey = clientPublicKey,
            KeyExchangeAlgorithm = "ECDH-P384",
            SupportedCiphers = new List<string> { "AES-256-GCM" },
            Nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)),
            Timestamp = DateTime.UtcNow
        };

        var openResponse = await _client.PostAsJsonAsync("/api/channel/open", openRequest);
        var channelId = openResponse.Headers.GetValues("X-Channel-Id").First();

        // Act
        var response = await _client.GetAsync($"/api/channel/{channelId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("channelId");

        clientEcdh.Dispose();
    }

    [Fact]
    public async Task GetChannel_WithInvalidChannelId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/channel/invalid-channel-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
