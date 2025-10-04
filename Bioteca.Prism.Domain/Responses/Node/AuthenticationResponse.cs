using Bioteca.Prism.Domain.Enumerators.Node;

namespace Bioteca.Prism.Domain.Responses.Node;

/// <summary>
/// Phase 3: Final authentication result after challenge-response verification
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// Whether authentication was successful
    /// </summary>
    public bool Authenticated { get; set; }

    /// <summary>
    /// Node ID that was authenticated
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Session token for subsequent authenticated requests (Phase 4)
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Session expiration time
    /// </summary>
    public DateTime? SessionExpiresAt { get; set; }

    /// <summary>
    /// List of capabilities granted to this node
    /// </summary>
    public NodeAccessTypeEnum GrantedNodeAccessLevel { get; set; } 

    /// <summary>
    /// Message describing authentication result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Next phase in the handshake (e.g., "phase4_session" or null if complete)
    /// </summary>
    public string? NextPhase { get; set; }

    /// <summary>
    /// Timestamp of authentication
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
