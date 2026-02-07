namespace Bioteca.Prism.Domain.Payloads.Research;

public class AddResearchDevicePayload
{
    public Guid DeviceId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string CalibrationStatus { get; set; } = string.Empty;
    public DateTime? LastCalibrationDate { get; set; }
}
