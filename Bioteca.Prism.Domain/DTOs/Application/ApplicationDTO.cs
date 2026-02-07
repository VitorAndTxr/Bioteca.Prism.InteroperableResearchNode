namespace Bioteca.Prism.Domain.DTOs.Application;

public class ApplicationDTO
{
    public Guid ApplicationId { get; set; }
    public Guid ResearchId { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
