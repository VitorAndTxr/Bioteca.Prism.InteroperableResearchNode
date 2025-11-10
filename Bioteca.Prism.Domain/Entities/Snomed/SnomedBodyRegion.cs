using System.Text.Json.Serialization;

namespace Bioteca.Prism.Domain.Entities.Snomed;

/// <summary>
/// Represents SNOMED CT body region codes
/// </summary>
public class SnomedBodyRegion
{
    /// <summary>
    /// SNOMED code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Parent region code (for hierarchical structure)
    /// </summary>
    public string? ParentRegionCode { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this code is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public SnomedBodyRegion? ParentRegion { get; set; }

    /// <summary>
    /// Sub-regions (child regions in hierarchy)
    /// JsonIgnore prevents circular reference during serialization
    /// </summary>
    [JsonIgnore]
    public ICollection<SnomedBodyRegion> SubRegions { get; set; } = new List<SnomedBodyRegion>();

    /// <summary>
    /// Sub-regions (child regions in hierarchy)
    /// JsonIgnore prevents circular reference during serialization
    /// </summary>
    [JsonIgnore]
    public ICollection<SnomedBodyStructure> BodyStructures { get; set; } = new List<SnomedBodyStructure>();
}
