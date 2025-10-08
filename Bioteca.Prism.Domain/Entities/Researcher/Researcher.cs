namespace Bioteca.Prism.Domain.Entities.Researcher;

/// <summary>
/// Represents a researcher working on research projects
/// </summary>
public class Researcher
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid ResearcherId { get; set; }

    /// <summary>
    /// Foreign key to research node
    /// </summary>
    public Guid ResearchNodeId { get; set; }

    /// <summary>
    /// Researcher name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Institution affiliation
    /// </summary>
    public string Institution { get; set; } = string.Empty;

    /// <summary>
    /// Role/position
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Node.ResearchNode ResearchNode { get; set; } = null!;
    public ICollection<Research.ResearchResearcher> ResearchResearchers { get; set; } = new List<Research.ResearchResearcher>();
}
