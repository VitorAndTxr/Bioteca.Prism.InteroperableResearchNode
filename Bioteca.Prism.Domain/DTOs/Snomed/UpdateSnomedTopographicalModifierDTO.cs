namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED CT topographical modifier
/// </summary>
public class UpdateSnomedTopographicalModifierDTO
{
    /// <summary>
    /// Display name for the topographical modifier
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the topographical modifier
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the topographical modifier
    /// </summary>
    public string Category { get; set; } = string.Empty;
}
