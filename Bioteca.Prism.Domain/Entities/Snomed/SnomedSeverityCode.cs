namespace Bioteca.Prism.Domain.Entities.Snomed;

/// <summary>
/// SNOMED CT severity classification codes
/// </summary>
public class SnomedSeverityCode
{
    /// <summary>
    /// SNOMED CT severity code (primary key)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the severity level
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the severity level
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this severity code is currently active in SNOMED CT
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this severity code was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this severity code was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Clinical conditions using this severity code
    /// </summary>
    public ICollection<Volunteer.VolunteerClinicalCondition> ClinicalConditions { get; set; } = new List<Volunteer.VolunteerClinicalCondition>();

    /// <summary>
    /// Clinical events rated with this severity code
    /// </summary>
    public ICollection<Volunteer.VolunteerClinicalEvent> ClinicalEvents { get; set; } = new List<Volunteer.VolunteerClinicalEvent>();
}
