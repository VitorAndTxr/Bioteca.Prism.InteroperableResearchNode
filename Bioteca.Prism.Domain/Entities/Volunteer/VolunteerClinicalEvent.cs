namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Clinical event experienced by a volunteer
/// </summary>
public class VolunteerClinicalEvent
{
    /// <summary>
    /// Unique identifier (primary key)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Volunteer ID (foreign key)
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Type of event (symptom, sign, finding)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Clinical event SNOMED code (foreign key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime EventDatetime { get; set; }

    /// <summary>
    /// Duration of the event in minutes
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Severity code (foreign key, optional)
    /// </summary>
    public string? SeverityCode { get; set; }

    /// <summary>
    /// Numeric value for quantifiable events (e.g., pain scale 1-10)
    /// </summary>
    public float? NumericValue { get; set; }

    /// <summary>
    /// Unit of the numeric value
    /// </summary>
    public string ValueUnit { get; set; } = string.Empty;

    /// <summary>
    /// JSON object with additional event characteristics
    /// </summary>
    public string Characteristics { get; set; } = string.Empty;

    /// <summary>
    /// Target area ID where event occurred (foreign key, optional)
    /// </summary>
    public Guid? TargetAreaId { get; set; }

    /// <summary>
    /// Record session ID when event was captured (foreign key, optional)
    /// </summary>
    public Guid? RecordSessionId { get; set; }

    /// <summary>
    /// Researcher who observed/recorded this event
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When this event record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this event record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer who experienced this event
    /// </summary>
    public Volunteer Volunteer { get; set; } = null!;

    /// <summary>
    /// Clinical event catalog entry
    /// </summary>
    public Clinical.ClinicalEvent ClinicalEvent { get; set; } = null!;

    /// <summary>
    /// Severity classification (optional)
    /// </summary>
    public Snomed.SnomedSeverityCode? Severity { get; set; }

    /// <summary>
    /// Body area where event occurred (optional)
    /// </summary>
    public Record.TargetArea? TargetArea { get; set; }

    /// <summary>
    /// Record session during which event was captured (optional)
    /// </summary>
    public Record.RecordSession? RecordSession { get; set; }
}
