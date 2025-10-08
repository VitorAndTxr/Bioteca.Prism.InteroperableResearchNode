namespace Bioteca.Prism.Domain.Entities.Application;

/// <summary>
/// Represents an application used in research
/// </summary>
public class Application
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Application name
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Application URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional information
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Research projects using this application (many-to-many)
    /// </summary>
    public ICollection<ResearchApplication> ResearchApplications { get; set; } = new List<ResearchApplication>();
}
