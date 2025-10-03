namespace Bioteca.Prism.Domain.Responses.Node;

/// <summary>
/// Phase 3: Server's response with challenge data for client to sign
/// </summary>
public class ChallengeResponse
{
    /// <summary>
    /// Random challenge data that must be signed by the client
    /// Base64-encoded 32-byte random value
    /// </summary>
    public string ChallengeData { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when challenge was generated
    /// </summary>
    public DateTime ChallengeTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time-to-live for this challenge in seconds
    /// </summary>
    public int ChallengeTtlSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Expiration time for this challenge
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddSeconds(300);
}
