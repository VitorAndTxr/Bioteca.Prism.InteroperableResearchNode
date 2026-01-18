using Bioteca.Prism.Domain.Enumerators.Node;

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
    /// Access level granted to this node
    /// </summary>
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }
}
