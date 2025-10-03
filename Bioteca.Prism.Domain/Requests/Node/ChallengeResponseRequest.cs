namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Phase 3: Response to server's challenge with signed challenge data
/// </summary>
public class ChallengeResponseRequest
{
    /// <summary>
    /// Channel ID from Phase 1 handshake
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Node ID that is responding to the challenge
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Original challenge data received from server
    /// </summary>
    public string ChallengeData { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature of the challenge data using node's private key
    /// Signature of: ChallengeData + ChannelId + NodeId + Timestamp
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
