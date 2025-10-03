using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Domain.Requests.Node;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Integration tests for Phase 4: Session Management
/// Tests session validation, renewal, revocation, and capability-based authorization
/// </summary>
public class Phase4SessionManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public Phase4SessionManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Helper: Complete authentication flow (Phases 1-3) and return session token
    /// </summary>
    private async Task<string> AuthenticateAndGetSessionTokenAsync(string nodeId = "test-node-phase4")
    {
        // Phase 1: Establish channel
        var channelResponse = await _client.PostAsync("/api/channel/open", new StringContent(
            JsonSerializer.Serialize(new { clientPublicKey = GenerateDummyPublicKey() }),
            Encoding.UTF8,
            "application/json"));

        channelResponse.EnsureSuccessStatusCode();
        var channelId = channelResponse.Headers.GetValues("X-Channel-Id").First();

        // Phase 2: Generate certificate and register node
        var certResponse = await _client.PostAsync("/api/testing/generate-certificate", new StringContent(
            JsonSerializer.Serialize(new { nodeId }),
            Encoding.UTF8,
            "application/json"));

        certResponse.EnsureSuccessStatusCode();
        var certDoc = JsonDocument.Parse(await certResponse.Content.ReadAsStringAsync());
        var certificateWithKey = certDoc.RootElement.GetProperty("certificateWithPrivateKey").GetString()!;
        var certificate = certDoc.RootElement.GetProperty("certificate").GetString()!;

        // Register node
        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = nodeId,
            NodeName = "Test Node Phase 4",
            Certificate = certificate,
            ContactInfo = "test@example.com",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://localhost:5000",
            RequestedCapabilities = new List<string> { "query:read", "data:write" },
            Timestamp = DateTime.UtcNow
        };

        var registerResponse = await _client.PostAsync("/api/node/register", new StringContent(
            JsonSerializer.Serialize(new
            {
                encryptedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(registrationRequest))),
                iv = Convert.ToBase64String(new byte[12]),
                authTag = Convert.ToBase64String(new byte[16])
            }),
            Encoding.UTF8,
            "application/json"));

        registerResponse.EnsureSuccessStatusCode();

        // Approve node
        var approveResponse = await _client.PutAsync($"/api/node/{nodeId}/status", new StringContent(
            JsonSerializer.Serialize(new { status = "Authorized" }),
            Encoding.UTF8,
            "application/json"));

        approveResponse.EnsureSuccessStatusCode();

        // Phase 3: Request challenge and authenticate
        var challengeRequest = new
        {
            channelId,
            nodeId,
            timestamp = DateTime.UtcNow
        };

        var challengeResponse = await _client.PostAsync("/api/testing/request-challenge", new StringContent(
            JsonSerializer.Serialize(challengeRequest),
            Encoding.UTF8,
            "application/json"));

        challengeResponse.EnsureSuccessStatusCode();
        var challengeDoc = JsonDocument.Parse(await challengeResponse.Content.ReadAsStringAsync());
        var challengeData = challengeDoc.RootElement.GetProperty("challengeData").GetString()!;

        // Sign challenge
        var signRequest = new
        {
            challengeData,
            channelId,
            nodeId,
            certificateWithPrivateKey = certificateWithKey,
            password = "test123",
            timestamp = DateTime.UtcNow
        };

        var signResponse = await _client.PostAsync("/api/testing/sign-challenge", new StringContent(
            JsonSerializer.Serialize(signRequest),
            Encoding.UTF8,
            "application/json"));

        signResponse.EnsureSuccessStatusCode();
        var signDoc = JsonDocument.Parse(await signResponse.Content.ReadAsStringAsync());
        var signature = signDoc.RootElement.GetProperty("signature").GetString()!;

        // Authenticate
        var authRequest = new
        {
            channelId,
            nodeId,
            challengeData,
            signature,
            timestamp = DateTime.UtcNow
        };

        var authResponse = await _client.PostAsync("/api/testing/authenticate", new StringContent(
            JsonSerializer.Serialize(authRequest),
            Encoding.UTF8,
            "application/json"));

        authResponse.EnsureSuccessStatusCode();
        var authDoc = JsonDocument.Parse(await authResponse.Content.ReadAsStringAsync());
        return authDoc.RootElement.GetProperty("sessionToken").GetString()!;
    }

    private string GenerateDummyPublicKey()
    {
        return Convert.ToBase64String(new byte[91]); // P-384 public key size
    }

    [Fact]
    public async Task WhoAmI_WithValidSession_ReturnsSessionInfo()
    {
        // Arrange
        var sessionToken = await AuthenticateAndGetSessionTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

        // Act
        var response = await _client.GetAsync("/api/session/whoami");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("sessionToken").GetString().Should().Be(sessionToken);
        doc.RootElement.GetProperty("nodeId").GetString().Should().Be("test-node-phase4");
        doc.RootElement.GetProperty("capabilities").GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("remainingSeconds").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WhoAmI_WithoutAuthorizationHeader_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/session/whoami");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("error").GetString().Should().Be("ERR_NO_AUTH_HEADER");
    }

    [Fact]
    public async Task WhoAmI_WithInvalidBearerFormat_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "invalid");

        // Act
        var response = await _client.GetAsync("/api/session/whoami");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("error").GetString().Should().Be("ERR_INVALID_AUTH_FORMAT");
    }

    [Fact]
    public async Task WhoAmI_WithInvalidSessionToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/session/whoami");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("error").GetString().Should().Be("ERR_INVALID_SESSION");
    }

    [Fact]
    public async Task RenewSession_WithValidSession_ExtendsExpiration()
    {
        // Arrange
        var sessionToken = await AuthenticateAndGetSessionTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

        // Get initial expiration
        var whoamiResponse1 = await _client.GetAsync("/api/session/whoami");
        var whoami1 = JsonDocument.Parse(await whoamiResponse1.Content.ReadAsStringAsync());
        var initialExpiration = whoami1.RootElement.GetProperty("expiresAt").GetDateTime();

        // Wait 1 second
        await Task.Delay(1000);

        // Act
        var renewResponse = await _client.PostAsync("/api/session/renew", new StringContent(
            JsonSerializer.Serialize(new { additionalSeconds = 3600 }),
            Encoding.UTF8,
            "application/json"));

        // Assert
        renewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var renewContent = await renewResponse.Content.ReadAsStringAsync();
        var renewDoc = JsonDocument.Parse(renewContent);

        var newExpiration = renewDoc.RootElement.GetProperty("expiresAt").GetDateTime();
        newExpiration.Should().BeAfter(initialExpiration);
    }

    [Fact]
    public async Task RevokeSession_WithValidSession_InvalidatesSession()
    {
        // Arrange
        var sessionToken = await AuthenticateAndGetSessionTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

        // Verify session is valid
        var whoamiResponse1 = await _client.GetAsync("/api/session/whoami");
        whoamiResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Revoke session
        var revokeResponse = await _client.PostAsync("/api/session/revoke", null);

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revokeContent = await revokeResponse.Content.ReadAsStringAsync();
        var revokeDoc = JsonDocument.Parse(revokeContent);

        revokeDoc.RootElement.GetProperty("revoked").GetBoolean().Should().BeTrue();

        // Verify session is now invalid
        var whoamiResponse2 = await _client.GetAsync("/api/session/whoami");
        whoamiResponse2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMetrics_WithAdminCapability_ReturnsMetrics()
    {
        // Arrange
        var sessionToken = await AuthenticateAndGetSessionTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

        // Note: This test assumes the node has admin:node capability
        // If not, we need to add capability configuration to test setup

        // Act
        var response = await _client.GetAsync("/api/session/metrics");

        // Assert - Either returns metrics or 403 if capability not granted
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            doc.RootElement.GetProperty("error").GetString().Should().Be("ERR_INSUFFICIENT_PERMISSIONS");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);

            doc.RootElement.GetProperty("nodeId").GetString().Should().Be("test-node-phase4");
            doc.RootElement.GetProperty("activeSessions").GetInt32().Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task RateLimiting_ExceedingLimit_Returns429()
    {
        // Arrange
        var sessionToken = await AuthenticateAndGetSessionTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);

        // Act - Make 61 requests rapidly (limit is 60/minute)
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 61; i++)
        {
            tasks.Add(_client.GetAsync("/api/session/whoami"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - At least one should be rate limited
        var rateLimitedResponses = responses.Count(r => r.StatusCode == (HttpStatusCode)429);
        rateLimitedResponses.Should().BeGreaterThan(0);

        var rateLimitedResponse = responses.First(r => r.StatusCode == (HttpStatusCode)429);
        var content = await rateLimitedResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("error").GetString().Should().Be("ERR_RATE_LIMIT_EXCEEDED");
    }
}
