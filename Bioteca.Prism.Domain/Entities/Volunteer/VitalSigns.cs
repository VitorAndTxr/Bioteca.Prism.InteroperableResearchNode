namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Vital signs measurements for volunteers
/// </summary>
public class VitalSigns
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
    /// Record session ID (foreign key)
    /// </summary>
    public Guid RecordSessionId { get; set; }

    /// <summary>
    /// Systolic blood pressure (mmHg)
    /// </summary>
    public float? SystolicBp { get; set; }

    /// <summary>
    /// Diastolic blood pressure (mmHg)
    /// </summary>
    public float? DiastolicBp { get; set; }

    /// <summary>
    /// Heart rate (beats per minute)
    /// </summary>
    public float? HeartRate { get; set; }

    /// <summary>
    /// Respiratory rate (breaths per minute)
    /// </summary>
    public float? RespiratoryRate { get; set; }

    /// <summary>
    /// Body temperature (Celsius)
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Oxygen saturation (SpO2 %)
    /// </summary>
    public float? OxygenSaturation { get; set; }

    /// <summary>
    /// Weight (kg)
    /// </summary>
    public float? Weight { get; set; }

    /// <summary>
    /// Height (cm)
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Body Mass Index (calculated)
    /// </summary>
    public float? Bmi { get; set; }

    /// <summary>
    /// When the measurement was taken
    /// </summary>
    public DateTime MeasurementDatetime { get; set; }

    /// <summary>
    /// Context of the measurement (e.g., pre-procedure, post-procedure, routine)
    /// </summary>
    public string MeasurementContext { get; set; } = string.Empty;

    /// <summary>
    /// Researcher who recorded the vital signs
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When this vital signs record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this vital signs record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer associated with these vital signs
    /// </summary>
    public Volunteer Volunteer { get; set; } = null!;

    /// <summary>
    /// Record session during which these vital signs were collected
    /// </summary>
    public Record.RecordSession RecordSession { get; set; } = null!;

    /// <summary>
    /// Researcher who measured these vital signs
    /// </summary>
    public Researcher.Researcher Recorder { get; set; } = null!;
}
