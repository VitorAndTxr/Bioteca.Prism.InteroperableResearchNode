namespace Bioteca.Prism.Domain.Requests.Session;

/// <summary>
/// Phase 4: Request to renew/extend a session
/// This request is encrypted via the channel
/// </summary>
public class RenewSessionRequest
{
    /// <summary>
    /// Channel ID (for validation)
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Session token to renew
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Additional seconds to extend the session (default: 3600 = 1 hour)
    /// </summary>
    public int AdditionalSeconds { get; set; } = 3600;

    /// <summary>
    /// Request timestamp (for replay protection)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
