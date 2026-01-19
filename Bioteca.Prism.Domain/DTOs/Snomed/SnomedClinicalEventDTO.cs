namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for creating a SNOMED CT clinical event
/// </summary>
public class SnomedClinicalEventDTO
{
    /// <summary>
    /// SNOMED CT code for the clinical event
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the clinical event
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the clinical event
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
