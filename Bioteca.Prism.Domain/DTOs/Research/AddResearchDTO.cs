namespace Bioteca.Prism.Domain.DTOs.Research;

public class AddResearchDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ResearchNodeId { get; set; }
}


