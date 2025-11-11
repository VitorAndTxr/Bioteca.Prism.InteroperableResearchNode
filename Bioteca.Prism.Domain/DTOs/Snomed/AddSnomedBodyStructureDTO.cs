namespace Bioteca.Prism.Domain.DTOs.Snomed;

public class AddSnomedBodyStructureDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string BodyRegionCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string Type { get; set; }
    public string? ParentStructureCode { get; set; }
}


