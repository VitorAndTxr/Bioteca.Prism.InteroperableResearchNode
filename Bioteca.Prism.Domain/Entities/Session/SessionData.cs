namespace Bioteca.Prism.Domain.Entities.Session;

/// <summary>
/// Represents an authenticated session for a node
/// Stored in-memory by SessionService
/// </summary>
public class SessionData
{
    /// <summary>
    /// Unique session identifier (GUID)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Node that owns this session
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Channel ID used during authentication
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// When the session was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session expires (UTC)
    /// Default: 1 hour from creation
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Last time the session was used (UTC)
    /// Updated on each authenticated request
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Capabilities granted to this session
    /// Example: ["query:read", "query:aggregate", "data:write"]
    /// </summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// Number of requests made in this session
    /// Used for rate limiting and metrics
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// IP address of the authenticated node (optional)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Check if session is still valid
    /// </summary>
    public bool IsValid()
    {
        return DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Check if session has a specific capability
    /// </summary>
    public bool HasCapability(string capability)
    {
        return Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get remaining session time in seconds
    /// </summary>
    public int GetRemainingSeconds()
    {
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining.TotalSeconds > 0 ? (int)remaining.TotalSeconds : 0;
    }
}
