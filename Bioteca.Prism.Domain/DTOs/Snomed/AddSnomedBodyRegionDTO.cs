namespace Bioteca.Prism.Domain.DTOs.Snomed;

public class AddSnomedBodyRegionDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string? ParentRegionCode { get; set; }
}


