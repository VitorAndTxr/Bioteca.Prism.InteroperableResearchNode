namespace Bioteca.Prism.Domain.Responses.Node;

/// <summary>
/// Response indicating the encrypted channel is ready
/// </summary>
public class ChannelReadyResponse
{
    /// <summary>
    /// Protocol version
    /// </summary>
    public string ProtocolVersion { get; set; } = "1.0";

    /// <summary>
    /// Base64-encoded ephemeral public key for ECDH key exchange
    /// </summary>
    public string EphemeralPublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Key exchange algorithm used (e.g., "ECDH-P384")
    /// </summary>
    public string KeyExchangeAlgorithm { get; set; } = string.Empty;

    /// <summary>
    /// Selected symmetric cipher from the list provided by requester
    /// </summary>
    public string SelectedCipher { get; set; } = string.Empty;

    /// <summary>
    /// Response timestamp (ISO 8601)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique nonce for replay attack prevention
    /// </summary>
    public string Nonce { get; set; } = string.Empty;
}
