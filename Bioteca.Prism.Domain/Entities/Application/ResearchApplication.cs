namespace Bioteca.Prism.Domain.Entities.Application;

/// <summary>
/// Join entity for many-to-many relationship between Research and Application
/// </summary>
public class ResearchApplication
{
    /// <summary>
    /// Foreign key to research
    /// </summary>
    public Guid ResearchId { get; set; }

    /// <summary>
    /// Foreign key to application
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Application role in the research
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// When the application was added to the research
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// When the application was removed from the research (null if still active)
    /// </summary>
    public DateTime? RemovedAt { get; set; }

    /// <summary>
    /// Configuration or settings specific to this application-research pairing
    /// </summary>
    public string? Configuration { get; set; }

    // Navigation properties
    public Research.Research Research { get; set; } = null!;
    public Application Application { get; set; } = null!;
}
