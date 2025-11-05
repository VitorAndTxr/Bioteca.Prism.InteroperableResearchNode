namespace Bioteca.Prism.Domain.Payloads.User;

public class AddResearcherPayload
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Institution { get; set; }
    public string Role { get; set; }

    public Guid ResearchNodeId { get; set; }
    public string Orcid { get; set; }
}


