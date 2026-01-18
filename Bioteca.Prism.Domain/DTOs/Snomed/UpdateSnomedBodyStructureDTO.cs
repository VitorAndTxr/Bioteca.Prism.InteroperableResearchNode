namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED body structure
/// </summary>
public class UpdateSnomedBodyStructureDTO
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? BodyRegionCode { get; set; }
}
