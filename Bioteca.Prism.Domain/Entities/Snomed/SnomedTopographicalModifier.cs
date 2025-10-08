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

    // Navigation properties
    public ICollection<Record.TargetArea> TargetAreas { get; set; } = new List<Record.TargetArea>();
}
