namespace Bioteca.Prism.Domain.Entities.Clinical;

/// <summary>
/// SNOMED CT clinical condition catalog
/// </summary>
public class ClinicalCondition
{
    /// <summary>
    /// SNOMED CT code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the clinical condition
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the clinical condition
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this condition is currently active in SNOMED CT
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this condition was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this condition was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer clinical conditions using this SNOMED code
    /// </summary>
    public ICollection<Volunteer.VolunteerClinicalCondition> VolunteerClinicalConditions { get; set; } = new List<Volunteer.VolunteerClinicalCondition>();
}
