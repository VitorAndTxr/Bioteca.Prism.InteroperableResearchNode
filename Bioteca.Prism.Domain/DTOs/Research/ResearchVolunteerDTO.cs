namespace Bioteca.Prism.Domain.DTOs.Research;

public class ResearchVolunteerDTO
{
    public Guid ResearchId { get; set; }
    public Guid VolunteerId { get; set; }
    public string VolunteerName { get; set; } = string.Empty;
    public string VolunteerCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EnrollmentStatus { get; set; } = string.Empty;
    public DateTime ConsentDate { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string? ExclusionReason { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}
