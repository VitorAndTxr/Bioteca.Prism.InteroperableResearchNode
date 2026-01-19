namespace Bioteca.Prism.Domain.DTOs.Researcher;

public class ResearcherDTO
{
    public Guid ResearcherId { get; set; }
    public Guid ResearchNodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
}

