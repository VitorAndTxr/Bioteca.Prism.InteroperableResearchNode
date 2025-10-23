namespace Bioteca.Prism.Domain.Entities.Research;

/// <summary>
/// Represents a research project hosted by a node
/// </summary>
public class Research
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to research node
    /// </summary>
    public Guid ResearchNodeId { get; set; }

    /// <summary>
    /// Research title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Research description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Research start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Research end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Research status (e.g., Planning, Active, Completed, Suspended)
    /// </summary>
    public string Status { get; set; } = string.Empty;

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
    public ICollection<Record.RecordSession> RecordSessions { get; set; } = new List<Record.RecordSession>();

    // Many-to-many relationships
    public ICollection<Application.Application> Applications { get; set; } = new List<Application.Application>();
    public ICollection<Device.ResearchDevice> ResearchDevices { get; set; } = new List<Device.ResearchDevice>();
    public ICollection<ResearchVolunteer> ResearchVolunteers { get; set; } = new List<ResearchVolunteer>();
    public ICollection<ResearchResearcher> ResearchResearchers { get; set; } = new List<ResearchResearcher>();
}
