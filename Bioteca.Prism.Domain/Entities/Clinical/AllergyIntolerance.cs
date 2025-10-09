namespace Bioteca.Prism.Domain.Entities.Clinical;

/// <summary>
/// Allergy and intolerance catalog with SNOMED CT codes
/// </summary>
public class AllergyIntolerance
{
    /// <summary>
    /// SNOMED CT code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Category of the allergy/intolerance (e.g., food, medication, environment)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Name of the substance causing the allergy/intolerance
    /// </summary>
    public string SubstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Type (allergy or intolerance)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this allergy/intolerance is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this allergy/intolerance was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this allergy/intolerance was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer allergies/intolerances using this catalog entry
    /// </summary>
    public ICollection<Volunteer.VolunteerAllergyIntolerance> VolunteerAllergyIntolerances { get; set; } = new List<Volunteer.VolunteerAllergyIntolerance>();
}
