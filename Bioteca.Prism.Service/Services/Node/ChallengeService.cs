using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bioteca.Prism.Service.Services.Node;

/// <summary>
/// In-memory implementation of challenge service for Phase 3 authentication
/// Integrated with SessionService (Phase 4)
/// </summary>
public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly ISessionService _sessionService;
    private readonly ConcurrentDictionary<string, ChallengeData> _activeChallenges = new();
    private const int ChallengeTtlSeconds = 300; // 5 minutes

    public ChallengeService(
        ILogger<ChallengeService> logger,
        ISessionService sessionService)
    {
        _logger = logger;
        _sessionService = sessionService;
    }

    public Task<ChallengeResponse> GenerateChallengeAsync(string channelId, string nodeId)
    {
        try
        {
            // Generate 32-byte random challenge
            var challengeBytes = RandomNumberGenerator.GetBytes(32);
            var challengeData = Convert.ToBase64String(challengeBytes);

            var expiresAt = DateTime.UtcNow.AddSeconds(ChallengeTtlSeconds);

            // Store challenge
            var key = $"{channelId}:{nodeId}";
            _activeChallenges[key] = new ChallengeData
            {
                ChallengeValue = challengeData,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("Generated challenge for node {NodeId} on channel {ChannelId}", nodeId, channelId);

            return Task.FromResult(new ChallengeResponse
            {
                ChallengeData = challengeData,
                ChallengeTimestamp = DateTime.UtcNow,
                ChallengeTtlSeconds = ChallengeTtlSeconds,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating challenge for node {NodeId}", nodeId);
            throw;
        }
    }

    public Task<bool> VerifyChallengeResponseAsync(ChallengeResponseRequest request, string certificate)
    {
        try
        {
            var key = $"{request.ChannelId}:{request.NodeId}";

            // Check if challenge exists
            if (!_activeChallenges.TryGetValue(key, out var challengeData))
            {
                _logger.LogWarning("No active challenge found for node {NodeId} on channel {ChannelId}",
                    request.NodeId, request.ChannelId);
                return Task.FromResult(false);
            }

            // Check if challenge has expired
            if (challengeData.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Challenge expired for node {NodeId}", request.NodeId);
                _activeChallenges.TryRemove(key, out _);
                return Task.FromResult(false);
            }

            // Verify challenge data matches
            if (request.ChallengeData != challengeData.ChallengeValue)
            {
                _logger.LogWarning("Challenge data mismatch for node {NodeId}", request.NodeId);
                return Task.FromResult(false);
            }

            // Verify signature
            var certBytes = Convert.FromBase64String(certificate);
            using var cert = new X509Certificate2(certBytes);
            using var rsa = cert.GetRSAPublicKey();

            if (rsa == null)
            {
                _logger.LogWarning("Certificate does not contain RSA public key for node {NodeId}", request.NodeId);
                return Task.FromResult(false);
            }

            // Build signed data: ChallengeData + ChannelId + NodeId + Timestamp
            var signedData = $"{request.ChallengeData}{request.ChannelId}{request.NodeId}{request.Timestamp:O}";
            var dataBytes = Encoding.UTF8.GetBytes(signedData);
            var signatureBytes = Convert.FromBase64String(request.Signature);

            var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (isValid)
            {
                _logger.LogInformation("Challenge response verified successfully for node {NodeId}", request.NodeId);
            }
            else
            {
                _logger.LogWarning("Invalid challenge response signature from node {NodeId}", request.NodeId);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying challenge response for node {NodeId}", request.NodeId);
            return Task.FromResult(false);
        }
    }

    public async Task<AuthenticationResponse> GenerateAuthenticationResultAsync(Guid nodeId, string channelId, NodeAccessTypeEnum nodeAccessLevel)
    {
        try
        {
            // Create session using SessionService (Phase 4)
            var sessionData = await _sessionService.CreateSessionAsync(
                nodeId,
                channelId,
                nodeAccessLevel,
                ttlSeconds: 3600);

            _logger.LogInformation(
                "Generated session for node {NodeId} on channel {ChannelId}: {SessionToken}",
                nodeId,
                channelId,
                sessionData.SessionToken);

            return new AuthenticationResponse
            {
                Authenticated = true,
                NodeId = nodeId.ToString(),
                SessionToken = sessionData.SessionToken,
                SessionExpiresAt = sessionData.ExpiresAt,
                GrantedNodeAccessLevel = nodeAccessLevel,
                Message = "Authentication successful",
                NextPhase = "phase4_session",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authentication result for node {NodeId}", nodeId);
            throw;
        }
    }

    public Task InvalidateChallengeAsync(string channelId, string nodeId)
    {
        var key = $"{channelId}:{nodeId}";
        _activeChallenges.TryRemove(key, out _);
        _logger.LogInformation("Invalidated challenge for node {NodeId} on channel {ChannelId}", nodeId, channelId);
        return Task.CompletedTask;
    }

    private class ChallengeData
    {
        public string ChallengeValue { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
