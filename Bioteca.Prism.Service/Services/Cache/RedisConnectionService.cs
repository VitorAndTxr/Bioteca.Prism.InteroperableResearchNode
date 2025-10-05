using Bioteca.Prism.Core.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Bioteca.Prism.Service.Services.Cache;

/// <summary>
/// Redis connection management service
/// </summary>
public class RedisConnectionService : IRedisConnectionService, IDisposable
{
    private readonly ILogger<RedisConnectionService> _logger;
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private bool _disposed;

    public RedisConnectionService(
        IConfiguration configuration,
        ILogger<RedisConnectionService> logger)
    {
        _logger = logger;

        var connectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis connection string not configured");

        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            try
            {
                _logger.LogInformation("Connecting to Redis: {ConnectionString}",
                    MaskPassword(connectionString));

                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 5000;
                options.SyncTimeout = 5000;

                var multiplexer = ConnectionMultiplexer.Connect(options);

                multiplexer.ConnectionFailed += (sender, args) =>
                {
                    _logger.LogError("Redis connection failed: {FailureType} - {Exception}",
                        args.FailureType, args.Exception?.Message);
                };

                multiplexer.ConnectionRestored += (sender, args) =>
                {
                    _logger.LogInformation("Redis connection restored: {FailureType}",
                        args.FailureType);
                };

                _logger.LogInformation("Redis connection established successfully");
                return multiplexer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        });
    }

    public IDatabase GetDatabase()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisConnectionService));

        return _connection.Value.GetDatabase();
    }

    public IServer GetServer()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisConnectionService));

        var endpoints = _connection.Value.GetEndPoints();
        if (endpoints.Length == 0)
            throw new InvalidOperationException("No Redis endpoints available");

        return _connection.Value.GetServer(endpoints[0]);
    }

    public bool IsConnected => _connection.IsValueCreated && _connection.Value.IsConnected;

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_connection.IsValueCreated)
        {
            _logger.LogInformation("Closing Redis connection");
            _connection.Value.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static string MaskPassword(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var parts = connectionString.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim().StartsWith("password=", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "password=***";
            }
        }
        return string.Join(',', parts);
    }
}
