namespace Bioteca.Prism.Domain.Responses.Node;

/// <summary>
/// Response to a node registration request
/// </summary>
public class NodeRegistrationResponse
{
    /// <summary>
    /// Indicates if the registration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Registration ID for tracking
    /// </summary>
    public string RegistrationId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the registration
    /// </summary>
    public RegistrationStatus Status { get; set; }

    /// <summary>
    /// Message to the requester
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estimated time for approval (if pending)
    /// </summary>
    public TimeSpan? EstimatedApprovalTime { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of a registration request
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Registration received and pending approval
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Registration approved
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Registration rejected
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Registration requires additional information
    /// </summary>
    RequiresMoreInfo = 3
}
