namespace Bioteca.Prism.Domain.Payloads.Record;

public class UpdateClinicalSessionPayload
{
    public DateTime? FinishedAt { get; set; }
    public TargetAreaPayload? TargetArea { get; set; }
}
