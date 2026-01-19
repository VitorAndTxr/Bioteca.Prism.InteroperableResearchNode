namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED CT allergy/intolerance entry
/// </summary>
public class UpdateSnomedAllergyIntoleranceDTO
{
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
}
