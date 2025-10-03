namespace Bioteca.Prism.Core.Middleware.Channel;

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
