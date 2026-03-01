namespace Bioteca.Prism.Domain.Payloads.Record;

public class TargetAreaPayload
{
    public string BodyStructureCode { get; set; } = string.Empty;
    public string? LateralityCode { get; set; }
    public List<string> TopographicalModifierCodes { get; set; } = new();
}
