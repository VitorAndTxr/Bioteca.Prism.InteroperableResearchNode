namespace Bioteca.Prism.Domain.Entities.Snomed;

/// <summary>
/// Represents SNOMED CT topographical modifier codes
/// </summary>
public class SnomedTopographicalModifier
{
    /// <summary>
    /// SNOMED code (primary key)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this code is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation timestamp (required for incremental sync)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (used as watermark for incremental sync)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Record.TargetAreaTopographicalModifier> TargetAreaTopographicalModifiers { get; set; } = new List<Record.TargetAreaTopographicalModifier>();
}
