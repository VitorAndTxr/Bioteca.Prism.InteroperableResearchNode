namespace Bioteca.Prism.Domain.DTOs.Snomed;

public class SnomedBodyStructureDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string Type { get; set; }
    public SnomedBodyRegionDTO BodyRegion { get; set; }
    public SnomedBodyStructureDTO? ParentStructure { get; set; }
}


