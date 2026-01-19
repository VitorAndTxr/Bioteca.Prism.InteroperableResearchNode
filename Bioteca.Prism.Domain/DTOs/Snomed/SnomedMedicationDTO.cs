namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for creating a SNOMED CT medication
/// </summary>
public class SnomedMedicationDTO
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
}
