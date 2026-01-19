namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED CT medication
/// </summary>
public class UpdateSnomedMedicationDTO
{
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
