using Bioteca.Prism.Domain.DTOs.ResearchNode;

namespace Bioteca.Prism.Domain.DTOs.Research;

public class ResearchDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public ResearchNodeConnectionDTO ResearchNode { get; set; } 
}


