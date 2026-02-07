namespace Bioteca.Prism.Domain.DTOs.Research;

public class ResearchResearcherDTO
{
    public Guid ResearchId { get; set; }
    public Guid ResearcherId { get; set; }
    public string ResearcherName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
    public bool IsPrincipal { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}
