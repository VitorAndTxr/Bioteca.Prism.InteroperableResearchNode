namespace Bioteca.Prism.Domain.Payloads.Record;

public class UpdateClinicalSessionPayload
{
    public DateTime? FinishedAt { get; set; }
    public string? ClinicalContext { get; set; }
}
