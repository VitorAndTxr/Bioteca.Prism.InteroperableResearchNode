namespace Bioteca.Prism.Domain.Requests.Session;

/// <summary>
/// Phase 4: Request to revoke/logout a session
/// This request is encrypted via the channel
/// </summary>
public class RevokeSessionRequest
{
    /// <summary>
    /// Channel ID (for validation)
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Session token to revoke
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Request timestamp (for replay protection)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
