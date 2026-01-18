namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED CT clinical condition
/// </summary>
public class UpdateSnomedClinicalConditionDTO
{
    /// <summary>
    /// Display name for the clinical condition
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the clinical condition
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
