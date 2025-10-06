using Bioteca.Prism.Domain.Enumerators.Node;

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
    /// Authenticated node Guid
    /// </summary>
    public Guid NodeId { get; set; }

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
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }
    /// <summary>
    /// Request count for this session
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Check if session has a specific capability
    /// </summary>
    public bool HasCapability(NodeAccessTypeEnum capability)
    {
        return NodeAccessLevel == capability;
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
