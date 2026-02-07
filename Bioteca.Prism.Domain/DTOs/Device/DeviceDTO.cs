namespace Bioteca.Prism.Domain.DTOs.Device;

public class DeviceDTO
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SensorCount { get; set; }
}
