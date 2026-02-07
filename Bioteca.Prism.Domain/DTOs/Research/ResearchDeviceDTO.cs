namespace Bioteca.Prism.Domain.DTOs.Research;

public class ResearchDeviceDTO
{
    public Guid ResearchId { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CalibrationStatus { get; set; } = string.Empty;
    public DateTime? LastCalibrationDate { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public int SensorCount { get; set; }
}
