namespace Bioteca.Prism.Domain.DTOs.Sensor;

public class SensorDTO
{
    public Guid SensorId { get; set; }
    public Guid DeviceId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public float MaxSamplingRate { get; set; }
    public string Unit { get; set; } = string.Empty;
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public float Accuracy { get; set; }
    public string AdditionalInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
