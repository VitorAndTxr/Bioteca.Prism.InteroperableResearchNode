using Bioteca.Prism.Domain.DTOs.ResearchNode;

namespace Bioteca.Prism.Domain.DTOs.Research;

public class ResearchDetailDTO
{
    public Guid Id { get; set; }
    public Guid ResearchNodeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ResearchNodeConnectionDTO ResearchNode { get; set; } = null!;
    public int ResearcherCount { get; set; }
    public int VolunteerCount { get; set; }
    public int ApplicationCount { get; set; }
    public int DeviceCount { get; set; }
}
