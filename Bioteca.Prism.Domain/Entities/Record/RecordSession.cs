namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents a recording session
/// </summary>
public class RecordSession
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to research
    /// </summary>
    public Guid? ResearchId { get; set; }

    /// <summary>
    /// Foreign key to volunteer
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Optional FK to the TargetArea that captures clinical context for this session
    /// </summary>
    public Guid? TargetAreaId { get; set; }

    /// <summary>
    /// Session start timestamp
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Session end timestamp
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Research.Research? Research { get; set; }
    public Volunteer.Volunteer Volunteer { get; set; } = null!;
    public TargetArea? TargetArea { get; set; }
    public ICollection<Record> Records { get; set; } = new List<Record>();
    public ICollection<SessionAnnotation> SessionAnnotations { get; set; } = new List<SessionAnnotation>();

    // Clinical data captured during session
    public ICollection<Volunteer.VolunteerClinicalEvent> ClinicalEvents { get; set; } = new List<Volunteer.VolunteerClinicalEvent>();
    public ICollection<Volunteer.VitalSigns> VitalSigns { get; set; } = new List<Volunteer.VitalSigns>();
}
