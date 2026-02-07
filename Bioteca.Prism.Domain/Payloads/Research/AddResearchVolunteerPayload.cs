namespace Bioteca.Prism.Domain.Payloads.Research;

public class AddResearchVolunteerPayload
{
    public Guid VolunteerId { get; set; }
    public DateTime ConsentDate { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
}
