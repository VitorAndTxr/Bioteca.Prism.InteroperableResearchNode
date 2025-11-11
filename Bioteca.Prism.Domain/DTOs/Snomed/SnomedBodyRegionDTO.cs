namespace Bioteca.Prism.Domain.DTOs.Snomed;

public class SnomedBodyRegionDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public SnomedBodyRegionDTO? ParentRegion { get; set; }
}

public class AddSnomedBodyRegionDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string? ParentRegionCode { get; set; }
}

public class SnomedBodyStructureDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string Type { get; set; }
    public SnomedBodyRegionDTO BodyRegion { get; set; }
    public SnomedBodyStructureDTO? ParentStructure { get; set; }
}

public class AddSnomedBodyStructureDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string BodyRegionCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
    public string Type { get; set; }
    public string? ParentStructureCode { get; set; }
}

public class SnomedTopographicalModifierDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; }
}

public class SnomedClinicalConditionDTO
{
    public string SnomedCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; }
}


