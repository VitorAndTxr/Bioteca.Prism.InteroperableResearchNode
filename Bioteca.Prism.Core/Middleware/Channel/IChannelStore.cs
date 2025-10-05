namespace Bioteca.Prism.Core.Middleware.Channel;

/// <summary>
/// Service for managing active encrypted channels (in-memory or distributed cache)
/// </summary>
public interface IChannelStore
{
    /// <summary>
    /// Add a channel to the store with automatic TTL expiration
    /// </summary>
    Task<bool> AddChannelAsync(string channelId, ChannelContext context);

    /// <summary>
    /// Get a channel by ID
    /// </summary>
    /// <returns>Channel context or null if not found/expired</returns>
    Task<ChannelContext?> GetChannelAsync(string channelId);

    /// <summary>
    /// Remove a channel from the store
    /// </summary>
    Task<bool> RemoveChannelAsync(string channelId);

    /// <summary>
    /// Check if a channel exists and is valid
    /// </summary>
    Task<bool> IsChannelValidAsync(string channelId);

    /// <summary>
    /// Cleans up expired channels (for in-memory implementation)
    /// Redis handles this automatically via TTL
    /// </summary>
    Task CleanupExpiredChannelsAsync();

    /// <summary>
    /// Gets total number of active channels
    /// </summary>
    Task<int> GetActiveChannelCountAsync();
}
