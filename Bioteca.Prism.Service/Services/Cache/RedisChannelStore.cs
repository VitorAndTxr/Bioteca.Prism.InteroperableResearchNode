using Bioteca.Prism.Core.Cache;
using Bioteca.Prism.Core.Middleware.Channel;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Bioteca.Prism.Service.Services.Cache;

/// <summary>
/// Redis-based channel store with automatic TTL expiration
/// </summary>
public class RedisChannelStore : IChannelStore
{
    private readonly IRedisConnectionService _redis;
    private readonly ILogger<RedisChannelStore> _logger;

    // Redis key patterns
    private const string ChannelKeyPrefix = "channel:";
    private const string ChannelKeyBinaryPrefix = "channel:key:";

    public RedisChannelStore(
        IRedisConnectionService redis,
        ILogger<RedisChannelStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> AddChannelAsync(string channelId, ChannelContext context)
    {
        try
        {
            var db = _redis.GetDatabase();
            var metadataKey = GetChannelMetadataKey(channelId);
            var keyBinaryKey = GetChannelKeyBinaryKey(channelId);

            // Calculate TTL from channel expiration
            var ttl = context.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                _logger.LogWarning("Attempted to create channel {ChannelId} with expired TTL", channelId);
                return false;
            }

            // Store metadata (everything except SymmetricKey)
            var metadata = new
            {
                context.ChannelId,
                context.SelectedCipher,
                context.ClientNonce,
                context.ServerNonce,
                context.CreatedAt,
                context.ExpiresAt,
                context.RemoteNodeUrl,
                context.Role,
                context.IdentifiedNodeId,
                context.CertificateFingerprint

            };

            var metadataJson = JsonSerializer.Serialize(context);

            // Use transaction to ensure atomicity
            var transaction = db.CreateTransaction();

            // Store metadata with TTL
            var setMetadataTask = transaction.StringSetAsync(metadataKey, metadataJson, ttl);

            // Store symmetric key separately (binary data) with TTL
            var setKeyTask = transaction.StringSetAsync(keyBinaryKey, context.SymmetricKey, ttl);

            // Execute transaction
            var committed = await transaction.ExecuteAsync();

            if (committed)
            {
                _logger.LogInformation(
                    "Channel {ChannelId} created in Redis with role {Role} and TTL {TTL}s",
                    channelId, context.Role, (int)ttl.TotalSeconds);
                return true;
            }

            _logger.LogWarning("Failed to create channel {ChannelId} - transaction not committed", channelId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating channel {ChannelId}", channelId);
            return false;
        }
    }

    public async Task<ChannelContext?> GetChannelAsync(string channelId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var metadataKey = GetChannelMetadataKey(channelId);
            var keyBinaryKey = GetChannelKeyBinaryKey(channelId);

            // Get metadata and symmetric key
            var metadataTask = db.StringGetAsync(metadataKey);
            var symmetricKeyTask = db.StringGetAsync(keyBinaryKey);

            await Task.WhenAll(metadataTask, symmetricKeyTask);

            var metadataJson = await metadataTask;
            var symmetricKeyBytes = await symmetricKeyTask;

            if (metadataJson.IsNullOrEmpty || symmetricKeyBytes.IsNullOrEmpty)
            {
                return null;
            }

            // Deserialize metadata
            var metadata = JsonSerializer.Deserialize<ChannelMetadata>(metadataJson!);
            if (metadata == null)
            {
                _logger.LogWarning("Failed to deserialize channel metadata for {ChannelId}", channelId);
                return null;
            }

            // Check expiration (defense in depth, Redis TTL should handle this)
            if (metadata.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Retrieved expired channel {ChannelId}, removing", channelId);
                await RemoveChannelAsync(channelId);
                return null;
            }

            // Reconstruct ChannelContext
            var context = new ChannelContext
            {
                ChannelId = metadata.ChannelId,
                SymmetricKey = (byte[])symmetricKeyBytes!,
                SelectedCipher = metadata.SelectedCipher,
                ClientNonce = metadata.ClientNonce,
                ServerNonce = metadata.ServerNonce,
                CreatedAt = metadata.CreatedAt,
                ExpiresAt = metadata.ExpiresAt,
                RemoteNodeUrl = metadata.RemoteNodeUrl,
                Role = metadata.Role,
                IdentifiedNodeId = metadata.IdentifiedNodeId,
                CertificateFingerprint = metadata.CertificateFingerprint
            };

            _logger.LogDebug("Retrieved valid channel {ChannelId} from Redis", channelId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving channel {ChannelId}", channelId);
            return null;
        }
    }

    public async Task<bool> RemoveChannelAsync(string channelId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var metadataKey = GetChannelMetadataKey(channelId);
            var keyBinaryKey = GetChannelKeyBinaryKey(channelId);

            // Use transaction to remove both keys
            var transaction = db.CreateTransaction();
            var delMetadataTask = transaction.KeyDeleteAsync(metadataKey);
            var delKeyTask = transaction.KeyDeleteAsync(keyBinaryKey);

            var committed = await transaction.ExecuteAsync();

            if (committed)
            {
                _logger.LogInformation("Channel {ChannelId} removed from Redis", channelId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing channel {ChannelId}", channelId);
            return false;
        }
    }

    public async Task<bool> IsChannelValidAsync(string channelId)
    {
        var channel = await GetChannelAsync(channelId);
        return channel != null;
    }

    public Task CleanupExpiredChannelsAsync()
    {
        // Redis automatically removes expired keys via TTL
        // No manual cleanup needed for Redis implementation
        _logger.LogDebug("CleanupExpiredChannelsAsync called - no action needed for Redis (automatic TTL)");
        return Task.CompletedTask;
    }

    public async Task<int> GetActiveChannelCountAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer();

            // Scan for all channel metadata keys (expensive operation, use with caution)
            var pattern = $"{ChannelKeyPrefix}*";
            var keys = server.Keys(pattern: pattern);

            var count = 0;
            foreach (var key in keys)
            {
                if (await db.KeyExistsAsync(key))
                {
                    count++;
                }
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channel count");
            return 0;
        }
    }

    // Helper methods for key generation
    private static string GetChannelMetadataKey(string channelId) => $"{ChannelKeyPrefix}{channelId}";
    private static string GetChannelKeyBinaryKey(string channelId) => $"{ChannelKeyBinaryPrefix}{channelId}";

    /// <summary>
    /// Metadata stored in Redis (everything except SymmetricKey)
    /// </summary>
    private class ChannelMetadata
    {
        public string ChannelId { get; set; } = string.Empty;
        public string SelectedCipher { get; set; } = string.Empty;
        public string ClientNonce { get; set; } = string.Empty;
        public string ServerNonce { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? RemoteNodeUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid? IdentifiedNodeId { get; set; }
        public string? CertificateFingerprint { get; set; }
    }
}
