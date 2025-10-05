using Bioteca.Prism.Core.Middleware.Channel;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Bioteca.Prism.Data.Cache.Channel;

/// <summary>
/// In-memory implementation of channel store (used when Redis is disabled)
/// For multi-instance deployments, use RedisChannelStore instead
/// </summary>
public class ChannelStore : IChannelStore
{
    private static readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
    private readonly ILogger<ChannelStore> _logger;

    public ChannelStore(ILogger<ChannelStore> logger)
    {
        _logger = logger;
    }

    public Task<bool> AddChannelAsync(string channelId, ChannelContext context)
    {
        var success = _channels.TryAdd(channelId, context);

        if (success)
        {
            _logger.LogInformation("Channel {ChannelId} added to in-memory store (role: {Role}, expires: {ExpiresAt})",
                channelId, context.Role, context.ExpiresAt);
        }
        else
        {
            _logger.LogWarning("Failed to add channel {ChannelId} - already exists", channelId);
        }

        return Task.FromResult(success);
    }

    public Task<ChannelContext?> GetChannelAsync(string channelId)
    {
        if (_channels.TryGetValue(channelId, out var context))
        {
            if (context.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Retrieved valid channel {ChannelId} from in-memory store", channelId);
                return Task.FromResult<ChannelContext?>(context);
            }

            // Channel expired - remove it
            _channels.TryRemove(channelId, out _);
            _logger.LogWarning("Channel {ChannelId} expired and removed from in-memory store", channelId);
        }

        return Task.FromResult<ChannelContext?>(null);
    }

    public Task<bool> RemoveChannelAsync(string channelId)
    {
        var removed = _channels.TryRemove(channelId, out var context);

        if (removed && context != null)
        {
            // Clear sensitive data
            Array.Clear(context.SymmetricKey, 0, context.SymmetricKey.Length);
            _logger.LogInformation("Channel {ChannelId} removed from in-memory store", channelId);
        }

        return Task.FromResult(removed);
    }

    public async Task<bool> IsChannelValidAsync(string channelId)
    {
        var channel = await GetChannelAsync(channelId);
        return channel != null;
    }

    public Task CleanupExpiredChannelsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredChannels = _channels
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        var count = 0;
        foreach (var channelId in expiredChannels)
        {
            if (_channels.TryRemove(channelId, out var context))
            {
                // Clear sensitive data
                Array.Clear(context.SymmetricKey, 0, context.SymmetricKey.Length);
                count++;
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired channels from in-memory store", count);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetActiveChannelCountAsync()
    {
        var count = _channels.Values.Count(c => c.ExpiresAt > DateTime.UtcNow);
        return Task.FromResult(count);
    }
}
