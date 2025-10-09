namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Represents a volunteer participant in research
/// </summary>
public class Volunteer
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Foreign key to research node
    /// </summary>
    public Guid ResearchNodeId { get; set; }

    /// <summary>
    /// Anonymized volunteer code
    /// </summary>
    public string VolunteerCode { get; set; } = string.Empty;

    /// <summary>
    /// Birth date
    /// </summary>
    public DateTime BirthDate { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Blood type
    /// </summary>
    public string BloodType { get; set; } = string.Empty;

    /// <summary>
    /// Height in meters
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Weight in kilograms
    /// </summary>
    public float? Weight { get; set; }

    /// <summary>
    /// Medical history
    /// </summary>
    public string MedicalHistory { get; set; } = string.Empty;

    /// <summary>
    /// Consent status (e.g., Pending, Consented, Withdrawn)
    /// </summary>
    public string ConsentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Enrollment timestamp
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Node.ResearchNode ResearchNode { get; set; } = null!;
    public ICollection<Record.RecordSession> RecordSessions { get; set; } = new List<Record.RecordSession>();
    public ICollection<Research.ResearchVolunteer> ResearchVolunteers { get; set; } = new List<Research.ResearchVolunteer>();

    // Clinical data navigation properties
    public ICollection<VolunteerClinicalCondition> ClinicalConditions { get; set; } = new List<VolunteerClinicalCondition>();
    public ICollection<VolunteerClinicalEvent> ClinicalEvents { get; set; } = new List<VolunteerClinicalEvent>();
    public ICollection<VolunteerMedication> Medications { get; set; } = new List<VolunteerMedication>();
    public ICollection<VolunteerAllergyIntolerance> AllergyIntolerances { get; set; } = new List<VolunteerAllergyIntolerance>();
    public ICollection<VitalSigns> VitalSigns { get; set; } = new List<VitalSigns>();
}
