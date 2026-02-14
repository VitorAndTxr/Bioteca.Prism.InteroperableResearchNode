namespace Bioteca.Prism.Domain.Payloads.Record;

public class CreateClinicalSessionPayload
{
    public Guid Id { get; set; }
    public Guid? ResearchId { get; set; }
    public Guid VolunteerId { get; set; }
    public string ClinicalContext { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
