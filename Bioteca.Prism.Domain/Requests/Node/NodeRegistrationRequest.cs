namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Phase 2: Request to register a new unknown node
/// </summary>
public class NodeRegistrationRequest
{
    /// <summary>
    /// Unique identifier of the node requesting registration
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the node/institution
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded X.509 certificate for node authentication
    /// </summary>
    public string Certificate { get; set; } = string.Empty;

    /// <summary>
    /// Contact information (email, phone, etc.)
    /// </summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>
    /// Institution details
    /// </summary>
    public string InstitutionDetails { get; set; } = string.Empty;

    /// <summary>
    /// Public URL where this node can be reached
    /// </summary>
    public string NodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Requested capabilities/permissions
    /// </summary>
    public List<string> RequestedCapabilities { get; set; } = new();

    /// <summary>
    /// Timestamp of registration request
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
