namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Join entity for the N:M relationship between TargetArea and SnomedTopographicalModifier
/// </summary>
public class TargetAreaTopographicalModifier
{
    public Guid TargetAreaId { get; set; }
    public string TopographicalModifierCode { get; set; } = string.Empty;

    // Navigation properties
    public TargetArea TargetArea { get; set; } = null!;
    public Snomed.SnomedTopographicalModifier TopographicalModifier { get; set; } = null!;
}
