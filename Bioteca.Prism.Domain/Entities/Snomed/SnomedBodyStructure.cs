using System.Text.Json.Serialization;

namespace Bioteca.Prism.Domain.Entities.Snomed;

/// <summary>
/// Represents SNOMED CT body structure codes
/// </summary>
public class SnomedBodyStructure
{
    /// <summary>
    /// SNOMED code (primary key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Body region code
    /// </summary>
    public string BodyRegionCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Structure type
    /// </summary>
    public string StructureType { get; set; } = string.Empty;

    /// <summary>
    /// Parent structure code (for hierarchical structure)
    /// </summary>
    
    public string? ParentStructureCode { get; set; }

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
    public SnomedBodyRegion BodyRegion { get; set; } = null!;
    [JsonIgnore]
    public SnomedBodyStructure? ParentStructure { get; set; }
    [JsonIgnore]
    public ICollection<SnomedBodyStructure> SubStructures { get; set; } = new List<SnomedBodyStructure>();
}
