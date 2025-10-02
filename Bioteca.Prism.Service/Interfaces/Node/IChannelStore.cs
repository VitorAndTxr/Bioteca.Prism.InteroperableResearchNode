namespace Bioteca.Prism.Service.Interfaces.Node;

/// <summary>
/// Service for managing active encrypted channels
/// </summary>
public interface IChannelStore
{
    /// <summary>
    /// Add a channel to the store
    /// </summary>
    void AddChannel(string channelId, ChannelContext context);

    /// <summary>
    /// Get a channel by ID
    /// </summary>
    /// <returns>Channel context or null if not found/expired</returns>
    ChannelContext? GetChannel(string channelId);

    /// <summary>
    /// Remove a channel from the store
    /// </summary>
    bool RemoveChannel(string channelId);

    /// <summary>
    /// Check if a channel exists and is valid
    /// </summary>
    bool IsChannelValid(string channelId);
}

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
}
