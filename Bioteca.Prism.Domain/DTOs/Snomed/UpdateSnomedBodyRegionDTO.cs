namespace Bioteca.Prism.Domain.DTOs.Snomed;

/// <summary>
/// DTO for updating a SNOMED body region
/// </summary>
public class UpdateSnomedBodyRegionDTO
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentRegionCode { get; set; }
}
