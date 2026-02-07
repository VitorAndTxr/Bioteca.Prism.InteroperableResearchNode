namespace Bioteca.Prism.Domain.Payloads.Research;

public class AddResearchResearcherPayload
{
    public Guid ResearcherId { get; set; }
    public bool IsPrincipal { get; set; }
}
