using Bioteca.Prism.Core.Middleware.Channel;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Bioteca.Prism.Data.Cache.Channel;

/// <summary>
/// In-memory implementation of channel store
/// In production, use distributed cache (Redis) for multi-instance deployments
/// </summary>
public class ChannelStore : IChannelStore
{
    private static readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
    private readonly ILogger<ChannelStore> _logger;

    public ChannelStore(ILogger<ChannelStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void AddChannel(string channelId, ChannelContext context)
    {
        if (_channels.TryAdd(channelId, context))
        {
            _logger.LogInformation("Channel {ChannelId} added to store (role: {Role}, expires: {ExpiresAt})",
                channelId, context.Role, context.ExpiresAt);
        }
        else
        {
            _logger.LogWarning("Failed to add channel {ChannelId} - already exists", channelId);
        }
    }

    /// <inheritdoc/>
    public ChannelContext? GetChannel(string channelId)
    {
        if (_channels.TryGetValue(channelId, out var context))
        {
            if (context.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Retrieved valid channel {ChannelId}", channelId);
                return context;
            }

            // Channel expired - remove it
            _channels.TryRemove(channelId, out _);
            _logger.LogWarning("Channel {ChannelId} expired and removed from store", channelId);
        }

        return null;
    }

    /// <inheritdoc/>
    public bool RemoveChannel(string channelId)
    {
        var removed = _channels.TryRemove(channelId, out var context);

        if (removed && context != null)
        {
            // Clear sensitive data
            Array.Clear(context.SymmetricKey, 0, context.SymmetricKey.Length);
            _logger.LogInformation("Channel {ChannelId} removed from store", channelId);
        }

        return removed;
    }

    /// <inheritdoc/>
    public bool IsChannelValid(string channelId)
    {
        return GetChannel(channelId) != null;
    }
}
