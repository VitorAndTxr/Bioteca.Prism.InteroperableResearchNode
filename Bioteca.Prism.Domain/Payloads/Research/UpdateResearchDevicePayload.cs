namespace Bioteca.Prism.Domain.Payloads.Research;

public class UpdateResearchDevicePayload
{
    public string? Role { get; set; }
    public string? CalibrationStatus { get; set; }
    public DateTime? LastCalibrationDate { get; set; }
}
