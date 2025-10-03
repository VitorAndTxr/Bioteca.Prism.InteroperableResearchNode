namespace Bioteca.Prism.Domain.Requests.Session;

/// <summary>
/// Phase 4: Request to get current session information
/// This request is encrypted via the channel
/// </summary>
public class WhoAmIRequest
{
    /// <summary>
    /// Channel ID (for validation)
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Session token (will also be in Authorization header)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Request timestamp (for replay protection)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
