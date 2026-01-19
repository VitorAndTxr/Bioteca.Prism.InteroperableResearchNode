namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED CT clinical event
/// </summary>
public class UpdateSnomedClinicalEventDTO
{
    /// <summary>
    /// Display name for the clinical event
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the clinical event
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
