namespace Bioteca.Prism.Core.Middleware.Channel;

/// <summary>
/// Context for an active encrypted channel
/// </summary>
public class ChannelContext
{
    public string ChannelId { get; set; } = string.Empty;
    public byte[] SymmetricKey { get; set; } = Array.Empty<byte>();
    public string SelectedCipher { get; set; } = string.Empty;
    public string ClientNonce { get; set; } = string.Empty;
    public string ServerNonce { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? RemoteNodeUrl { get; set; }
    public string Role { get; set; } = string.Empty; // "client" or "server"

    /// <summary>
    /// Identified node's Guid ID (set after Phase 2 identification)
    /// </summary>
    public Guid? IdentifiedNodeId { get; set; }

    /// <summary>
    /// Certificate fingerprint of the identified node (set after Phase 2 identification)
    /// </summary>
    public string? CertificateFingerprint { get; set; }
}
