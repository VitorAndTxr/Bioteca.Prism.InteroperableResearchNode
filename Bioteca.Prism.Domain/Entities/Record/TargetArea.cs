namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents a target area (body structure) for a record session
/// </summary>
public class TargetArea
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to record session
    /// </summary>
    public Guid RecordSessionId { get; set; }

    /// <summary>
    /// SNOMED body structure code
    /// </summary>
    public string BodyStructureCode { get; set; } = string.Empty;

    /// <summary>
    /// SNOMED laterality code (optional)
    /// </summary>
    public string? LateralityCode { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public RecordSession RecordSession { get; set; } = null!;
    public Snomed.SnomedBodyStructure BodyStructure { get; set; } = null!;
    public Snomed.SnomedLaterality? Laterality { get; set; }
    public ICollection<TargetAreaTopographicalModifier> TopographicalModifiers { get; set; } = new List<TargetAreaTopographicalModifier>();
}
