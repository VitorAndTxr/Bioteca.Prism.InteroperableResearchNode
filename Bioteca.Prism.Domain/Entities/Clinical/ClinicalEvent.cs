namespace Bioteca.Prism.Domain.Entities.Clinical;

/// <summary>
/// SNOMED CT clinical event catalog
/// </summary>
public class ClinicalEvent
{
    /// <summary>
    /// SNOMED CT code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the clinical event
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the clinical event
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this event is currently active in SNOMED CT
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this event was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this event was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer clinical events using this SNOMED code
    /// </summary>
    public ICollection<Volunteer.VolunteerClinicalEvent> VolunteerClinicalEvents { get; set; } = new List<Volunteer.VolunteerClinicalEvent>();
}
