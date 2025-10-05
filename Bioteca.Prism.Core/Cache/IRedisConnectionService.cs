using StackExchange.Redis;

namespace Bioteca.Prism.Core.Cache;

/// <summary>
/// Service for managing Redis connections
/// </summary>
public interface IRedisConnectionService
{
    /// <summary>
    /// Gets the Redis database instance
    /// </summary>
    IDatabase GetDatabase();

    /// <summary>
    /// Gets the Redis server instance for administrative operations
    /// </summary>
    IServer GetServer();

    /// <summary>
    /// Checks if Redis is connected and available
    /// </summary>
    bool IsConnected { get; }
}
