namespace Bioteca.Prism.Domain.Responses.Node;

/// <summary>
/// Phase 2: Response indicating the status of a node (known/unknown)
/// </summary>
public class NodeStatusResponse
{
    /// <summary>
    /// Indicates if the node is recognized and authorized
    /// </summary>
    public bool IsKnown { get; set; }

    /// <summary>
    /// Authorization status (if known)
    /// </summary>
    public AuthorizationStatus Status { get; set; }

    /// <summary>
    /// Node ID that was identified (string identifier used in protocol)
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Registration ID (Guid - internal database identifier, only present if node is known)
    /// </summary>
    public Guid? RegistrationId { get; set; }

    /// <summary>
    /// Node name (if known)
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Registration endpoint URL (if unknown node)
    /// </summary>
    public string? RegistrationUrl { get; set; }

    /// <summary>
    /// Additional information or instructions
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Next phase to proceed to (if known and authorized)
    /// </summary>
    public string? NextPhase { get; set; }
}

/// <summary>
/// Authorization status of a node
/// </summary>
public enum AuthorizationStatus
{
    /// <summary>
    /// Node is unknown - needs registration
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Node is authorized and can proceed to authentication
    /// </summary>
    Authorized = 1,

    /// <summary>
    /// Node is pending approval
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Node is revoked/blocked
    /// </summary>
    Revoked = 3
}
