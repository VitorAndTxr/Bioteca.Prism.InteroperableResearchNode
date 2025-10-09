namespace Bioteca.Prism.Domain.Entities.Clinical;

/// <summary>
/// Medication catalog with SNOMED CT and ANVISA codes
/// </summary>
public class Medication
{
    /// <summary>
    /// SNOMED CT code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Medication name
    /// </summary>
    public string MedicationName { get; set; } = string.Empty;

    /// <summary>
    /// Active pharmaceutical ingredient
    /// </summary>
    public string ActiveIngredient { get; set; } = string.Empty;

    /// <summary>
    /// ANVISA (Brazilian Health Regulatory Agency) code
    /// </summary>
    public string AnvisaCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this medication is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this medication was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this medication was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer medications prescribed using this medication
    /// </summary>
    public ICollection<Volunteer.VolunteerMedication> VolunteerMedications { get; set; } = new List<Volunteer.VolunteerMedication>();
}
