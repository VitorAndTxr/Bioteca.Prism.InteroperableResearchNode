namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents a target area (body structure) for a record channel
/// </summary>
public class TargetArea
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to record channel
    /// </summary>
    public Guid RecordChannelId { get; set; }

    /// <summary>
    /// SNOMED body structure code
    /// </summary>
    public string BodyStructureCode { get; set; } = string.Empty;

    /// <summary>
    /// SNOMED laterality code (optional)
    /// </summary>
    public string? LateralityCode { get; set; }

    /// <summary>
    /// SNOMED topographical modifier code (optional)
    /// </summary>
    public string? TopographicalModifierCode { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public RecordChannel RecordChannel { get; set; } = null!;
    public Snomed.SnomedBodyStructure BodyStructure { get; set; } = null!;
    public Snomed.SnomedLaterality? Laterality { get; set; }
    public Snomed.SnomedTopographicalModifier? TopographicalModifier { get; set; }
}
