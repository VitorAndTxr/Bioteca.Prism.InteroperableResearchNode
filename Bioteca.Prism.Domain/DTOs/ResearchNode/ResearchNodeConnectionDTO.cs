using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Domain.DTOs.ResearchNode;

public class ResearchNodeConnectionDTO
{
    public Guid Id { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string NodeUrl { get; set; } = string.Empty;
    public AuthorizationStatus Status { get; set; }
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }
    public string ContactInfo { get; set; } = string.Empty;
    public string Certificate { get; set; } = string.Empty;
    public string CertificateFingerprint { get; set; } = string.Empty;
    public string InstitutionDetails { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


