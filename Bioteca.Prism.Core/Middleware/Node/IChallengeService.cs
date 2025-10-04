using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Core.Middleware.Node;

/// <summary>
/// Service for managing challenge-response authentication (Phase 3)
/// </summary>
public interface IChallengeService
{
    /// <summary>
    /// Generate a new challenge for node authentication
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="nodeId">Node ID requesting authentication</param>
    /// <returns>Challenge data to be signed by client</returns>
    Task<ChallengeResponse> GenerateChallengeAsync(string channelId, string nodeId);

    /// <summary>
    /// Verify challenge response signature
    /// </summary>
    /// <param name="request">Challenge response with signature</param>
    /// <param name="certificate">Node's X.509 certificate for verification</param>
    /// <returns>True if signature is valid and challenge has not expired</returns>
    Task<bool> VerifyChallengeResponseAsync(ChallengeResponseRequest request, string certificate);

    /// <summary>
    /// Generate authentication result and session token
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="nodeAccessLevel">List of capabilities to grant</param>
    /// <returns>Authentication response with session token</returns>
    Task<AuthenticationResponse> GenerateAuthenticationResultAsync(string nodeId, string channelId, NodeAccessTypeEnum nodeAccessLevel);

    /// <summary>
    /// Invalidate a challenge (cleanup after successful authentication or timeout)
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="nodeId">Node ID</param>
    Task InvalidateChallengeAsync(string channelId, string nodeId);
}
