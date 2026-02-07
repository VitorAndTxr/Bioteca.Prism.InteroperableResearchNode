namespace Bioteca.Prism.Domain.Payloads.Research;

public class UpdateResearchPayload
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}
