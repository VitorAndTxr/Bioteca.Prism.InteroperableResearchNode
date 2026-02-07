namespace Bioteca.Prism.Domain.Payloads.Sensor;

/// <summary>
/// Payload for creating a new sensor
/// </summary>
public class AddSensorPayload
{
    public Guid DeviceId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public float MaxSamplingRate { get; set; }
    public string Unit { get; set; } = string.Empty;
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public float Accuracy { get; set; }
    public string AdditionalInfo { get; set; } = string.Empty;
}
