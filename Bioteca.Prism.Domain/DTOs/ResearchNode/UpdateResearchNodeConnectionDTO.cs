using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Domain.DTOs.ResearchNode;

/// <summary>
/// DTO for updating a research node connection
/// </summary>
public class UpdateResearchNodeConnectionDTO
{
    /// <summary>
    /// Human-readable name of the node/institution
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Public URL where this node can be reached
    /// </summary>
    public string NodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authorization status (0=Unknown, 1=Authorized, 2=Pending, 3=Revoked)
    /// </summary>
    public AuthorizationStatus Status { get; set; }

    /// <summary>
    /// Access level granted to this node
    /// </summary>
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }

    /// <summary>
    /// Contact information for this node
    /// </summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded X.509 certificate
    /// </summary>
    public string Certificate { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the certificate
    /// </summary>
    public string CertificateFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Institution details
    /// </summary>
    public string InstitutionDetails { get; set; } = string.Empty;
}
