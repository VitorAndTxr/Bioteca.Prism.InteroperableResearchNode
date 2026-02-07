namespace Bioteca.Prism.Domain.Payloads.Device;

public class AddDevicePayload
{
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
}
