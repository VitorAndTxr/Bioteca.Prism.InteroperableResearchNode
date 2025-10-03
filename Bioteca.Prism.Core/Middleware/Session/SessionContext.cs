namespace Bioteca.Prism.Core.Middleware.Session;

/// <summary>
/// Context object passed to controllers after session validation
/// Stored in HttpContext.Items["SessionContext"]
/// </summary>
public class SessionContext
{
    /// <summary>
    /// Session token (GUID)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated node ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Channel ID used during authentication
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Session expiration time (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Capabilities granted to this session
    /// </summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// Request count for this session
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Check if session has a specific capability
    /// </summary>
    public bool HasCapability(string capability)
    {
        return Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if session has ANY of the specified capabilities
    /// </summary>
    public bool HasAnyCapability(params string[] capabilities)
    {
        return capabilities.Any(c => HasCapability(c));
    }

    /// <summary>
    /// Check if session has ALL of the specified capabilities
    /// </summary>
    public bool HasAllCapabilities(params string[] capabilities)
    {
        return capabilities.All(c => HasCapability(c));
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
