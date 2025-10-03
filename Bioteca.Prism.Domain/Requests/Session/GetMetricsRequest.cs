namespace Bioteca.Prism.Domain.Requests.Session;

/// <summary>
/// Phase 4: Request to get session metrics
/// This request is encrypted via the channel
/// Requires admin:node capability
/// </summary>
public class GetMetricsRequest
{
    /// <summary>
    /// Channel ID (for validation)
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Session token (for authentication)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Node ID to get metrics for (optional, defaults to current session's node)
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Request timestamp (for replay protection)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
