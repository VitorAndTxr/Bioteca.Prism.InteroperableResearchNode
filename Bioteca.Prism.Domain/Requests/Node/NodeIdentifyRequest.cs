namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Phase 2: Request to identify a node after encrypted channel is established
/// </summary>
public class NodeIdentifyRequest
{
    /// <summary>
    /// Channel ID from Phase 1 handshake
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the node (e.g., institution ID, UUID)
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the node/institution
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded X.509 certificate for node authentication
    /// </summary>
    public string Certificate { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of identification request
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Encrypted proof that node controls the certificate's private key
    /// Signature of: ChannelId + NodeId + Timestamp (signed with certificate's private key)
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}
