namespace Bioteca.Prism.Domain.Entities.Research;

/// <summary>
/// Join table for many-to-many relationship between Research and Researcher
/// </summary>
public class ResearchResearcher
{
    /// <summary>
    /// Foreign key to research
    /// </summary>
    public Guid ResearchId { get; set; }

    /// <summary>
    /// Foreign key to researcher
    /// </summary>
    public Guid ResearcherId { get; set; }

    /// <summary>
    /// Whether this researcher is the principal investigator
    /// </summary>
    public bool IsPrincipal { get; set; }

    /// <summary>
    /// Assignment timestamp
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// Removal timestamp
    /// </summary>
    public DateTime? RemovedAt { get; set; }

    // Navigation properties
    public Research Research { get; set; } = null!;
    public Researcher.Researcher Researcher { get; set; } = null!;
}
