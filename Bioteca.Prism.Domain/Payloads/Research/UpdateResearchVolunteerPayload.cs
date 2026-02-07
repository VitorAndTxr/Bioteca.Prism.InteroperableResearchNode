namespace Bioteca.Prism.Domain.Payloads.Research;

public class UpdateResearchVolunteerPayload
{
    public string? EnrollmentStatus { get; set; }
    public DateTime? ConsentDate { get; set; }
    public string? ConsentVersion { get; set; }
    public string? ExclusionReason { get; set; }
}
