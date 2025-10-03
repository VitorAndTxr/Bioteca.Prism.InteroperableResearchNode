namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Phase 3: Request to initiate challenge-response authentication
/// </summary>
public class ChallengeRequest
{
    /// <summary>
    /// Channel ID from Phase 1 handshake
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Node ID that is requesting authentication
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the challenge request
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
