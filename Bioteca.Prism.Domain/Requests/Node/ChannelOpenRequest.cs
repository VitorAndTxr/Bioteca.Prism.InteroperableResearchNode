namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Request to open an encrypted channel using ephemeral keys (ECDH)
/// </summary>
public class ChannelOpenRequest
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
    /// Key exchange algorithm (e.g., "ECDH-P384")
    /// </summary>
    public string KeyExchangeAlgorithm { get; set; } = string.Empty;

    /// <summary>
    /// List of supported symmetric ciphers for the channel
    /// </summary>
    public List<string> SupportedCiphers { get; set; } = new();

    /// <summary>
    /// Request timestamp (ISO 8601)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique nonce for replay attack prevention
    /// </summary>
    public string Nonce { get; set; } = string.Empty;
}
