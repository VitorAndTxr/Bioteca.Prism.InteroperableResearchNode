namespace Bioteca.Prism.Domain.Payloads.Sensor;

public class UpdateSensorPayload
{
    public string? SensorName { get; set; }
    public float? MaxSamplingRate { get; set; }
    public string? Unit { get; set; }
    public float? MinRange { get; set; }
    public float? MaxRange { get; set; }
    public float? Accuracy { get; set; }
    public string? AdditionalInfo { get; set; }
}
