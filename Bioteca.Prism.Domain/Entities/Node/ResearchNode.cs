using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Domain.Entities.Node;

/// <summary>
/// Represents a research node in the network
/// </summary>
public class ResearchNode
{
    /// <summary>
    /// Unique identifier of the node
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name of the node/institution
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded X.509 certificate
    /// </summary>
    public string Certificate { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the certificate
    /// </summary>
    public string CertificateFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Public URL where this node can be reached
    /// </summary>
    public string NodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authorization status
    /// </summary>
    public AuthorizationStatus Status { get; set; }

    /// <summary>
    /// Capabilities granted to this node
    /// </summary>
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }

    /// <summary>
    /// Contact information
    /// </summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>
    /// Institution details
    /// </summary>
    public string InstitutionDetails { get; set; } = string.Empty;

    /// <summary>
    /// Date when the node was registered
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Date when the node was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Date of last successful authentication (if any)
    /// </summary>
    public DateTime? LastAuthenticatedAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
